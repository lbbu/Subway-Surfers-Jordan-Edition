using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
internal sealed class JordanMaterialRoadBend : MonoBehaviour
{
    private const float MaximumStrength = 0.1f;
    private const float MinimumBendEntryDistance = 4f;

    private static readonly int CurveId = Shader.PropertyToID("_JordanRoadCurve");
    private static readonly int PowerId = Shader.PropertyToID("_JordanRoadBendPower");
    private static readonly int VerticalCurveId = Shader.PropertyToID("_JordanRoadVerticalCurve");

    [SerializeField, Range(-MaximumStrength, MaximumStrength), InspectorName("Bend")]
    [Tooltip("Negative bends left, zero is straight, and positive bends right.")]
    private float strength = 0.085f;
    [SerializeField, FormerlySerializedAs("straightDistance")]
    [Tooltip("Start offset in metres relative to the player. Negative starts behind the player.")]
    private float bendStart = 12f;
    [SerializeField, Min(MinimumBendEntryDistance), FormerlySerializedAs("buildupDistance")]
    [Tooltip("Metres from Bend Start to half of the final turn angle. Lower values make the turn visible sooner.")]
    private float bendEntryDistance = 30f;
    [SerializeField, Range(0f, 1f)]
    [Tooltip("Higher values spread the change in heading, so the road eases more gently into the turn.")]
    private float entrySoftness = 0.6f;
    [SerializeField, Range(2f, 5f), FormerlySerializedAs("buildupPower")]
    [Tooltip("Higher values make the first part of the turn subtler, then build the heading faster. Entry Distance still measures the halfway point.")]
    private float bendExponent = 4f;
    [SerializeField, Range(0f, 15f)]
    [Tooltip("Target angle in degrees at full bend strength. Tight entries can limit the angle to protect the inside curb.")]
    private float maximumAngle = 8f;
    [Header("Vertical Bend (Y)")]
    [SerializeField, Range(0f, 1f)]
    [Tooltip("Downward bend strength. Zero keeps the road level.")]
    private float verticalBend;
    [SerializeField]
    [Tooltip("Start offset in metres relative to the player. Negative starts behind the player.")]
    private float verticalBendStart = 16f;
    [SerializeField, Min(MinimumBendEntryDistance)]
    [Tooltip("Metres from Vertical Bend Start to half of the final downward angle.")]
    private float verticalBendEntryDistance = 32f;
    [SerializeField, Range(2f, 5f)]
    [Tooltip("Higher values keep the first part subtler and steepen the transition farther ahead.")]
    private float verticalBendExponent = 3f;
    [SerializeField, Range(0f, 15f)]
    [Tooltip("Downward angle in degrees the road gradually approaches at full vertical bend strength.")]
    private float maximumDownwardAngle = 8f;

    [Header("Coin Materials")]
    [SerializeField] private Material sourceCoinMaterial;
    [SerializeField] private Material bentCoinMaterial;

    private readonly HashSet<GameObject> boundCoins = new();
    private readonly List<Material> coinSlots = new();
    private Camera gameplayCamera;
    private ChunkManager coinOwner;
    private Vector4 previousCurve;
    private float previousPower;
    private Vector4 previousVerticalCurve;
    private Matrix4x4 previousCullingMatrix;
    private bool expandedCulling;
    private bool rendering;

    public float Strength
    {
        get => strength;
        set => strength = Mathf.Clamp(value, -MaximumStrength, MaximumStrength);
    }

    private void OnEnable()
    {
        gameplayCamera = GetComponent<Camera>();
        RenderPipelineManager.beginCameraRendering += BeginCamera;
        RenderPipelineManager.endCameraRendering += EndCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeginCamera;
        RenderPipelineManager.endCameraRendering -= EndCamera;
        RestoreCamera();
        coinOwner = null;
        boundCoins.Clear();
    }

    private void BeginCamera(ScriptableRenderContext context, Camera cameraToRender)
    {
        if (cameraToRender != gameplayCamera || cameraToRender.cameraType != CameraType.Game)
            return;

        var manager = ChunkManager.instance;
        if (!manager || !manager.player)
            return;

        if (coinOwner != manager)
        {
            coinOwner = manager;
            boundCoins.Clear();
            BindCoins(manager.coinPool);
        }

        BindCoins(manager.activeCoins);

        float signedStrength = Mathf.Clamp(strength / MaximumStrength, -1f, 1f);
        float angle = Mathf.Clamp(maximumAngle, 0f, 15f) * signedStrength * Mathf.Deg2Rad;
        float entry = Mathf.Max(MinimumBendEntryDistance, bendEntryDistance);
        float power = Mathf.Clamp(bendExponent, 2f, 5f) * Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(entrySoftness));
        float slope = GetSafeSlope(angle, entry, power);
        float playerZ = manager.player.transform.position.z;
        float downwardAngle = Mathf.Clamp(maximumDownwardAngle, 0f, 15f) * Mathf.Clamp01(verticalBend) * Mathf.Deg2Rad;
        float downwardSlope = Mathf.Tan(downwardAngle);
        float verticalEntry = Mathf.Max(MinimumBendEntryDistance, verticalBendEntryDistance);
        float verticalExponent = Mathf.Clamp(verticalBendExponent, 2f, 5f);

        previousCurve = Shader.GetGlobalVector(CurveId);
        previousPower = Shader.GetGlobalFloat(PowerId);
        previousVerticalCurve = Shader.GetGlobalVector(VerticalCurveId);
        Shader.SetGlobalVector(CurveId, new Vector4(playerZ, bendStart, entry, slope));
        Shader.SetGlobalFloat(PowerId, power);
        Shader.SetGlobalVector(VerticalCurveId, new Vector4(verticalBendStart, verticalEntry, verticalExponent, downwardSlope));
        rendering = true;

        if ((slope == 0f && downwardSlope == 0f) || cameraToRender.orthographic || cameraToRender.stereoEnabled)
            return;

        // Visibility tests use the original mesh bounds. Expand them horizontally
        // to include objects that the vertex shader rotates into the camera view.
        previousCullingMatrix = cameraToRender.cullingMatrix;
        cameraToRender.ResetCullingMatrix();
        if (cameraToRender.cullingMatrix != previousCullingMatrix)
        {
            cameraToRender.cullingMatrix = previousCullingMatrix;
            return;
        }

        var projection = cameraToRender.projectionMatrix;
        if (downwardSlope > 0f)
        {
            cameraToRender.cullingMatrix = GetVerticalCullingMatrix(cameraToRender, projection, playerZ, slope, downwardSlope);
        }
        else
        {
            float forward = Mathf.Max(0.01f, cameraToRender.transform.forward.z);
            projection.m00 /= 1f + Mathf.Abs(projection.m00 * slope) / forward;
            cameraToRender.cullingMatrix = projection * cameraToRender.worldToCameraMatrix;
        }
        expandedCulling = true;
    }

    private Matrix4x4 GetVerticalCullingMatrix(Camera cameraToRender, Matrix4x4 projection, float playerZ,
        float horizontalSlope, float downwardSlope)
    {
        var transform = cameraToRender.transform;
        var eye = transform.position;
        var forward = AbsComponents(transform.forward);
        var right = AbsComponents(transform.right);
        var up = AbsComponents(transform.up);
        float horizontalExtent = (1f + Mathf.Abs(projection.m02)) / Mathf.Abs(projection.m00);
        float verticalExtent = (1f + Mathf.Abs(projection.m12)) / Mathf.Abs(projection.m11);
        var positionPerDepth = forward + right * horizontalExtent + up * verticalExtent;

        float yaw = Mathf.Abs(horizontalSlope);
        float yawCosine = 1f / Mathf.Sqrt(1f + yaw * yaw);
        var displacementPerDepth = GetHorizontalDisplacementBound(positionPerDepth.x, positionPerDepth.z, yaw, yawCosine);
        var displacementAtEye = GetHorizontalDisplacementBound(Mathf.Abs(eye.x),
            Mathf.Max(0f, eye.z - (playerZ + bendStart)), yaw, yawCosine);

        // The integrated blend is bounded by distance past the start. The Y
        // shader uses original world Z, so include the horizontal Z displacement.
        displacementPerDepth.y = downwardSlope * (positionPerDepth.z + displacementPerDepth.z);
        displacementAtEye.y = downwardSlope * (Mathf.Max(0f, eye.z - (playerZ + verticalBendStart)) + displacementAtEye.z);

        float depthPerDepth = Vector3.Dot(forward, displacementPerDepth);
        float depthAtEye = Vector3.Dot(forward, displacementAtEye);
        float depthRatio = Mathf.Max(0.01f, 1f - depthPerDepth);
        horizontalExtent = (horizontalExtent + Vector3.Dot(right, displacementPerDepth)) / depthRatio;
        verticalExtent = (verticalExtent + Vector3.Dot(up, displacementPerDepth)) / depthRatio;

        // A signed start behind the camera adds a distance-independent margin.
        // Back up the culling frustum and keep the rendering projection intact.
        float near = cameraToRender.nearClipPlane;
        float eyeBack = near * depthPerDepth + depthAtEye + Mathf.Max(
            Vector3.Dot(right, displacementAtEye) / horizontalExtent,
            Vector3.Dot(up, displacementAtEye) / verticalExtent);
        float far = cameraToRender.farClipPlane * (1f + depthPerDepth) + eyeBack + depthAtEye;

        var cullingView = cameraToRender.worldToCameraMatrix;
        cullingView.m23 -= eyeBack;
        var cullingProjection = Matrix4x4.Frustum(-near * horizontalExtent, near * horizontalExtent,
            -near * verticalExtent, near * verticalExtent, near, far);
        return cullingProjection * cullingView;
    }

    private static Vector3 GetHorizontalDisplacementBound(float x, float z, float yaw, float yawCosine)
    {
        // Bound the inverse horizontal bend, including forward shortening and
        // the world Z shift of vertices away from the middle lane.
        float integral = (z + yaw * x) / yawCosine;
        float compression = (1f - yawCosine) / yawCosine;
        return new Vector3(compression * x + yaw * integral, 0f, compression * integral + yaw * x);
    }

    private static Vector3 AbsComponents(Vector3 vector)
    {
        return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
    }

    private static float GetSafeSlope(float angle, float entry, float power)
    {
        // Match RoadCurve.hlsl. The inner edge loses longitudinal length
        // as curvature increases. Bound that loss across the road and curbs.
        const float halfRamp = 0.70710678118f;
        const float roadHalfWidth = 12f;
        const float minimumCenterlineSpeed = 0.99f;

        float shape = Mathf.Exp(power);
        float rate = Mathf.Log((1f + halfRamp * shape) / (1f - halfRamp)) / entry;
        float skew = shape - 1f;
        float peakRamp = (skew + Mathf.Sqrt(skew * skew + 3f * shape)) / (3f * shape);
        float peakBlendRate = 2f * rate * peakRamp * (1f - peakRamp) * (1f + shape * peakRamp) / (1f + shape);
        float safeSin = 0.88f * minimumCenterlineSpeed * minimumCenterlineSpeed / (roadHalfWidth * peakBlendRate);
        float signedSin = Mathf.Clamp(Mathf.Sin(angle), -safeSin, safeSin);
        return signedSin / Mathf.Sqrt(1f - signedSin * signedSin);
    }

    private void EndCamera(ScriptableRenderContext context, Camera cameraToRender)
    {
        if (cameraToRender == gameplayCamera)
            RestoreCamera();
    }

    private void RestoreCamera()
    {
        if (!rendering)
            return;

        Shader.SetGlobalVector(CurveId, previousCurve);
        Shader.SetGlobalFloat(PowerId, previousPower);
        Shader.SetGlobalVector(VerticalCurveId, previousVerticalCurve);
        if (expandedCulling && gameplayCamera)
            gameplayCamera.ResetCullingMatrix();
        expandedCulling = false;
        rendering = false;
    }

    private void BindCoins(List<GameObject> coins)
    {
        if (!sourceCoinMaterial || !bentCoinMaterial)
            return;

        foreach (var coin in coins)
        {
            if (!coin || !boundCoins.Add(coin))
                continue;

            // The coin pool instantiates the shared prefab at runtime, so its
            // renderers have no scene-instance material override to inherit.
            foreach (var renderer in coin.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.GetSharedMaterials(coinSlots);
                bool changed = false;
                for (int i = 0; i < coinSlots.Count; i++)
                {
                    if (coinSlots[i] != sourceCoinMaterial)
                        continue;

                    coinSlots[i] = bentCoinMaterial;
                    changed = true;
                }

                if (changed)
                    renderer.SetSharedMaterials(coinSlots);
            }
        }
    }
}
