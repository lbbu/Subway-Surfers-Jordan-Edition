using UnityEngine;

/// <summary>حدود قطعة الطريق؛ يمكن استخدام نقطتي ربط أو طول محلي قابل للتعديل.</summary>
[DisallowMultipleComponent]
public sealed class RunnerTrackSegment : MonoBehaviour
{
    [SerializeField, Min(1f)] private float length = 30f;
    [SerializeField] private float localStartZ = -15f;
    [SerializeField] private Transform entry;
    [SerializeField] private Transform exit;
    [SerializeField] private BoxCollider roadSurface;
    public float StartZ => entry ? entry.position.z : roadSurface ? roadSurface.transform.TransformPoint(roadSurface.center - Vector3.forward * roadSurface.size.z * 0.5f).z : transform.TransformPoint(new Vector3(0, 0, localStartZ)).z;
    public float EndZ => exit ? exit.position.z : roadSurface ? roadSurface.transform.TransformPoint(roadSurface.center + Vector3.forward * roadSurface.size.z * 0.5f).z : transform.TransformPoint(new Vector3(0, 0, localStartZ + length)).z;
    public float Length => EndZ - StartZ;

    // نقاط الربط تتبع تغيير حجم المجسم؛ لا نعتمد على حدود المباني والزينة لتحديد طول الشارع.
    public void PlaceStartAt(float z) => transform.position += Vector3.forward * (z - StartZ);
}
