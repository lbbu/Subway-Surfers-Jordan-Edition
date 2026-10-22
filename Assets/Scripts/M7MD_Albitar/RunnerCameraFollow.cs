using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;

/// <summary>يوجه هدف Cinemachine قرب منتصف الطريق؛ الـ Brain وحده يحرك الكاميرا الفعلية.</summary>
[DisallowMultipleComponent]
public class RunnerCameraFollow : MonoBehaviour
{
    [SerializeField] private PlayerMovements target;
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private Transform trackingAnchor;
    [Tooltip("مكان اللاعب على الشاشة في المسارين الخارجيين: 0.23 يعني 27% و73% من عرض الشاشة.")]
    [SerializeField, Range(0.1f, 0.35f)] private float sideViewportOffset = 0.23f;
    [SerializeField, Range(0, 1)] private float airborneVerticalFollow = 0.12f;
    [SerializeField, Min(0)] private float lateralDamping = 0.08f;
    [SerializeField, Min(0)] private float verticalDamping = 0.65f;
    [SerializeField, Min(1)] private float followDistance = 7.8f;
    [SerializeField, Min(1)] private float cameraHeight = 4.4f;
    [SerializeField, Range(0, 45)] private float pitch = 8f;
    [SerializeField, Range(35, 80)] private float fieldOfView = 55f;
    [SerializeField, Min(1)] private float trackHalfWidth = 5f;
    [SerializeField, Range(0, 0.3f)] private float impactAmplitude = 0.1f;
    [SerializeField, Range(0, 3)] private float impactRoll = 0.8f;
    [SerializeField, Range(0.05f, 0.5f)] private float impactDuration = 0.22f;
    [SerializeField, Range(1, 30)] private float impactFrequency = 12f;
    private PlayerMovements subscribedTarget;
    private CinemachineFollow follow;
    private float originX, fixedZ, impactAge = 100f, impactDirection = 1f;
    private bool initialized, sessionPaused;
    public bool IsImpactActive => impactAge < impactDuration;
    public Vector3 ImpactOffset { get; private set; }
    public float ImpactRoll { get; private set; }

    private void OnEnable() => BindTarget();
    private void OnDisable() { UnbindTarget(); initialized = false; }
    private void Start() { if (!initialized) SnapToTarget(); }
    private void BindTarget()
    {
        if (subscribedTarget == target) return;
        UnbindTarget(); subscribedTarget = target;
        if (subscribedTarget) subscribedTarget.SideBumped += OnSideBumped;
    }
    private void UnbindTarget()
    {
        if (subscribedTarget) subscribedTarget.SideBumped -= OnSideBumped;
        subscribedTarget = null;
    }
    public void SetTarget(PlayerMovements player) { target = player; BindTarget(); initialized = false; }
    public void SetSessionPaused(bool value) => sessionPaused = value;
    private void OnSideBumped()
    {
        impactAge = 0;
        impactDirection = target.LastSideBumpDirection == 0 ? 1 : target.LastSideBumpDirection;
    }

    public void SnapToTarget()
    {
        if (!target || !virtualCamera || !brain || !trackingAnchor) return;
        BindTarget();
        originX = target.transform.position.x;
        fixedZ = target.transform.position.z;
        impactAge = 100f; ImpactOffset = Vector3.zero; ImpactRoll = 0;
        follow = virtualCamera.GetComponent<CinemachineFollow>();
        virtualCamera.Follow = trackingAnchor;
        brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
        // بعض نسخ المشهد القديمة كانت تعطل Brain؛ نفعّله عند تجهيز الجولة.
        brain.enabled = true;
        UpdateAnchor();
        ConfigureFraming();
        // إلغاء تاريخ التخميد عند إعادة الجولة يمنع تحليق الكاميرا من مكان الجولة السابقة.
        virtualCamera.PreviousStateIsValid = false;
        brain.ManualUpdate();
        initialized = true;
    }

    private void UpdateAnchor()
    {
        // نحافظ على حجم اللاعب بقرب ثابت، ونضعه جانب الكادر بدل توسيع المسافة لإظهار عرض الطريق كله.
        float aspect = brain.OutputCamera ? Mathf.Max(0.25f, brain.OutputCamera.aspect) : 9f / 16f;
        float radians = pitch * Mathf.Deg2Rad;
        float chestDepth = followDistance * Mathf.Cos(radians) + (cameraHeight - 0.9f) * Mathf.Sin(radians);
        float halfWidth = chestDepth * Mathf.Tan(fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect;
        float laneFraction = Mathf.Clamp((target.transform.position.x - originX) / trackHalfWidth, -1, 1);
        float x = target.transform.position.x - laneFraction * (2 * sideViewportOffset * halfWidth);
        float y = target.SupportHeight + Mathf.Max(0, target.transform.position.y - target.SupportHeight) * airborneVerticalFollow;
        trackingAnchor.SetPositionAndRotation(new Vector3(x, y, fixedZ), Quaternion.identity);
    }

    private void ConfigureFraming()
    {
        // لا يتغير البعد على Z عند تبديل المسار أو تغيير أبعاد نافذة اللعب.
        virtualCamera.Lens.FieldOfView = fieldOfView;
        virtualCamera.transform.rotation = Quaternion.Euler(pitch, 0, 0);
        if (follow)
        {
            follow.FollowOffset = new Vector3(0, cameraHeight, -followDistance);
            follow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            follow.TrackerSettings.PositionDamping = new Vector3(lateralDamping, verticalDamping, 0);
        }
    }

    private void LateUpdate() => SimulateFollow(Time.deltaTime);
    public void SimulateFollow(float deltaTime)
    {
        if (sessionPaused || !target || deltaTime <= 0) return;
        if (!initialized) SnapToTarget();
        if (!initialized) return;
        UpdateAnchor(); ConfigureFraming();
        float wave = 0;
        if (IsImpactActive)
        {
            impactAge = Mathf.Min(impactDuration, impactAge + deltaTime);
            float envelope = 1 - impactAge / impactDuration;
            wave = Mathf.Sin(impactAge * impactFrequency * Mathf.PI * 2) * envelope * envelope * impactDirection;
        }
        ImpactOffset = Vector3.right * (wave * impactAmplitude);
        ImpactRoll = -wave * impactRoll;
        // تحديث يدوي واحد بعد تحريك الهدف، ويتوقف بالكامل عند إيقاف الجولة مؤقتًا.
        brain.ManualUpdate();
    }
}
