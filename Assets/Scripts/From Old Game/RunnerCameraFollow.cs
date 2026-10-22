using UnityEngine;

/// <summary>A fixed runner composition with gentle lane and support-height tracking.</summary>
[DisallowMultipleComponent]
public class RunnerCameraFollow : MonoBehaviour
{
    [SerializeField] private PlayerMovements target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5.2f, -10f);
    [SerializeField, Range(0, 1)] private float lateralFollow = 0.35f;
    [SerializeField, Range(0, 1)] private float airborneVerticalFollow = 0.22f;
    [SerializeField, Min(0.01f)] private float lateralSmoothTime = 0.18f;
    [SerializeField, Min(0.01f)] private float verticalSmoothTime = 0.22f;
    [SerializeField, Range(0, 45)] private float pitch = 17f;
    [SerializeField, Range(35, 80)] private float fieldOfView = 55f;
    private float originX, baseY, xVelocity, yVelocity;
    private bool initialized;

    private void Start() => SnapToTarget();

    public void SnapToTarget()
    {
        if (!target) return;
        originX = target.transform.position.x;
        baseY = target.SupportHeight;
        xVelocity = yVelocity = 0;
        transform.position = new Vector3(originX + offset.x, baseY + offset.y,
            target.transform.position.z + offset.z);
        transform.rotation = Quaternion.Euler(pitch, 0, 0);
        var cameraComponent = GetComponent<Camera>();
        if (cameraComponent) cameraComponent.fieldOfView = fieldOfView;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!target || Time.deltaTime <= 0) return;
        if (!initialized) SnapToTarget();
        Vector3 player = target.transform.position;
        baseY = Mathf.SmoothDamp(baseY, target.SupportHeight, ref yVelocity,
            verticalSmoothTime, Mathf.Infinity, Time.deltaTime);
        float desiredX = originX + (player.x - originX) * lateralFollow + offset.x;
        float x = Mathf.SmoothDamp(transform.position.x, desiredX, ref xVelocity,
            lateralSmoothTime, Mathf.Infinity, Time.deltaTime);
        // A separate exponential filter restrains jumps/dives without following collider resize.
        float desiredY = baseY + offset.y +
            Mathf.Max(0, player.y - target.SupportHeight) * airborneVerticalFollow;
        float y = Mathf.Lerp(transform.position.y, desiredY, 1f - Mathf.Exp(-Time.deltaTime / verticalSmoothTime));
        // Exact forward offset avoids speed-dependent camera lag and catches acceleration cleanly.
        transform.position = new Vector3(x, y, player.z + offset.z);
        transform.rotation = Quaternion.Euler(pitch, 0, 0);
    }
}
