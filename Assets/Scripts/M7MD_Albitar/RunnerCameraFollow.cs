using UnityEngine;

/// <summary>Anchored-Z runner camera with lateral framing and brief directional impact feedback.</summary>
[DisallowMultipleComponent]
public class RunnerCameraFollow : MonoBehaviour
{
    [SerializeField] private PlayerMovements target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5.2f, -10f);
    [SerializeField, Range(0, 1)] private float lateralFollow = 1f;
    [SerializeField, Range(0, 1)] private float airborneVerticalFollow = 0.22f;
    [SerializeField, Min(0.01f)] private float lateralSmoothTime = 0.08f;
    [SerializeField, Min(0.01f)] private float verticalSmoothTime = 0.22f;
    [SerializeField, Range(0, 45)] private float pitch = 17f;
    [SerializeField, Range(35, 80)] private float fieldOfView = 55f;

    [Header("Keep the player visible, including portrait screens")]
    [SerializeField, Range(0.05f, 0.35f)] private float viewportMargin = 0.18f;
    [SerializeField, Min(0)] private float framingPadding = 0.4f;

    [Header("Side bump feedback (set amplitude and roll to zero to disable)")]
    [SerializeField, Range(0, 0.3f)] private float impactAmplitude = 0.1f;
    [SerializeField, Range(0, 3f)] private float impactRoll = 0.8f;
    [SerializeField, Range(0.05f, 0.5f)] private float impactDuration = 0.22f;
    [SerializeField, Range(1, 30)] private float impactFrequency = 12f;

    private float originX, fixedCameraZ, baseY, xVelocity, yVelocity;
    private Vector3 followPosition;
    private bool initialized, sessionPaused;
    private PlayerMovements subscribedTarget;
    private Camera cameraComponent;
    private float impactAge = 100f, impactDirection = 1f;
    public bool IsImpactActive => impactAge < impactDuration;

    private void OnEnable() { BindTarget(); }
    private void OnDisable() { UnbindTarget(); impactAge = 100f; initialized = false; }
    private void Start() => SnapToTarget();

    private void BindTarget()
    {
        if (subscribedTarget == target) return;
        UnbindTarget();
        subscribedTarget = target;
        if (subscribedTarget) subscribedTarget.SideBumped += OnSideBumped;
    }

    private void UnbindTarget()
    {
        if (subscribedTarget) subscribedTarget.SideBumped -= OnSideBumped;
        subscribedTarget = null;
    }

    /// <summary>الجولة تحدد هدف الكاميرا صراحة، مع فك الاشتراك عن الهدف السابق.</summary>
    public void SetTarget(PlayerMovements player)
    {
        target = player;
        BindTarget();
        initialized = false;
    }

    // تجميد حركة الكاميرا ومؤقت الهزة دون تغيير الزمن العام للتطبيق.
    public void SetSessionPaused(bool value) => sessionPaused = value;

    private void OnSideBumped()
    {
        impactDirection = target.LastSideBumpDirection == 0 ? 1 : target.LastSideBumpDirection;
        impactAge = 0f; // Replace a previous impulse, never accumulate unlimited shake.
    }

    public void SnapToTarget()
    {
        if (!target) return;
        BindTarget();
        originX = target.transform.position.x;
        fixedCameraZ = target.transform.position.z + offset.z;
        baseY = target.SupportHeight;
        xVelocity = yVelocity = 0;
        impactAge = 100f;
        followPosition = new Vector3(originX + offset.x, baseY + offset.y, fixedCameraZ);
        transform.SetPositionAndRotation(followPosition, Quaternion.Euler(pitch, 0, 0));
        cameraComponent = GetComponent<Camera>();
        if (cameraComponent) cameraComponent.fieldOfView = fieldOfView;
        initialized = true;
    }

    private void LateUpdate() => SimulateFollow(Time.deltaTime);

    /// <summary>Manual equivalent of LateUpdate for deterministic framing checks.</summary>
    public void SimulateFollow(float deltaTime)
    {
        if (sessionPaused || !target || deltaTime <= 0) return;
        BindTarget();
        if (!initialized) SnapToTarget();
        Vector3 player = target.transform.position;
        baseY = Mathf.SmoothDamp(baseY, target.SupportHeight, ref yVelocity,
            verticalSmoothTime, Mathf.Infinity, deltaTime);
        float desiredX = originX + (player.x - originX) * lateralFollow + offset.x;
        float x = Mathf.SmoothDamp(followPosition.x, desiredX, ref xVelocity,
            lateralSmoothTime, Mathf.Infinity, deltaTime);
        float desiredY = baseY + offset.y +
            Mathf.Max(0, player.y - target.SupportHeight) * airborneVerticalFollow;
        float y = Mathf.Lerp(followPosition.y, desiredY, 1f - Mathf.Exp(-deltaTime / verticalSmoothTime));
        if (cameraComponent)
        {
            Vector3 forward = Quaternion.Euler(pitch, 0, 0) * Vector3.forward;
            float depth = Mathf.Max(0.1f, Vector3.Dot(player + Vector3.up -
                new Vector3(x, y, fixedCameraZ), forward));
            float halfWidth = depth * Mathf.Tan(cameraComponent.fieldOfView * Mathf.Deg2Rad * 0.5f) *
                cameraComponent.aspect;
            float visibleHalfWidth = Mathf.Max(0, halfWidth * (1f - 2f * viewportMargin) -
                framingPadding - impactAmplitude);
            // Clamp only the lag that would push the player outside the safe frame.
            float framedX = Mathf.Clamp(x, player.x - visibleHalfWidth, player.x + visibleHalfWidth);
            if (!Mathf.Approximately(x, framedX)) xVelocity = 0;
            x = framedX;
        }
        followPosition = new Vector3(x, y, fixedCameraZ);
        float wave = 0f;
        if (impactAge < impactDuration)
        {
            impactAge = Mathf.Min(impactDuration, impactAge + deltaTime);
            float envelope = 1f - impactAge / impactDuration;
            wave = Mathf.Sin(impactAge * impactFrequency * Mathf.PI * 2f) *
                envelope * envelope * impactDirection;
        }
        transform.SetPositionAndRotation(followPosition + Vector3.right * (wave * impactAmplitude),
            Quaternion.Euler(pitch, 0, -wave * impactRoll));
    }
}

