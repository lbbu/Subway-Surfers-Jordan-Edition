using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("عناصر التتبع")]
    // الهدف الذي ستتبعه الكاميرا (لاعبك)
    public Transform target;

    [Header("إعدادات الحركة")]
    // سرعة نعومة حركة الكاميرا (قيمة بين 0 و 1)
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.125f;

    // المسافة الثابتة بين الكاميرا واللاعب (Offset)
    public Vector3 offset = new Vector3(0f, 5f, -10f);

    // نستخدم LateUpdate لأنها تُنفذ بعد تحديث حركة اللاعب في Update
    void LateUpdate()
    {
        // حماية من الأخطاء: التأكد من تحديد الهدف أولاً
        if (target == null)
        {
            Debug.LogWarning("لم يتم تحديد الهدف (Target) في سكربت الكاميرا!");
            return;
        }

        // 1. حساب الموقع المطلوب الوصول إليه
        Vector3 desiredPosition = target.position + offset;

        // 2. الانتقال السلس من الموقع الحالي إلى الموقع المطلوب
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // 3. تطبيق الموقع الجديد على الكاميرا
        transform.position = smoothedPosition;
    }
}