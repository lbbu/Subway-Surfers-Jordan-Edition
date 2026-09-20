using UnityEngine;

public class AutoMove : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    void Update()
    {
        // التحرك للأمام تلقائياً باستمرار بناءً على اتجاه الكبسولة
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}