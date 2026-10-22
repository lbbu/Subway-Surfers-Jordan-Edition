using UnityEngine;

public class AutoMove : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    Rigidbody rb;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    void FixedUpdate()
    {
        speed += 0.001f;
        Vector3 forward = transform.forward * speed;
        rb.linearVelocity = new Vector3(forward.x, rb.linearVelocity.y, forward.z);
    }
    void Update()
    {
        // التحرك للأمام تلقائياً باستمرار بناءً على اتجاه الكبسولة
        //transform.Translate(Vector3.forward * speed * Time.deltaTime);
        
    }
}