using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jump : MonoBehaviour
{
    public float jumpHeight = 2f;
    public float timeToJumpApex = 0.4f;

    private float gravity;
    private float jumpVelocity;
    private float velocityY;
    private Rigidbody rb;
    private bool jumpRequested; // متغير لالتقاط الإدخال

    Swipe swipeControls;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheckPos;
    [SerializeField] float playerHight;
    [SerializeField] LayerMask whatIsGround;
    bool isGrounded;

    private void Awake()
    {
        swipeControls = GetComponent<Swipe>();
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // إيقاف جاذبية يونيتي لمنع التعارض
    }

    private void Start()
    {
        CalculateGravityAndJumpVelocity();
    }

    private void Update()
    {
        if (!PlayerMovements.isAllowToMove) { return; }

        // التقاط الإدخال في Update لمنع ضياع الفريم
        if (swipeControls.SwipeUp)
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (!PlayerMovements.isAllowToMove) { return; }

        isGrounded = Physics.Raycast(groundCheckPos.position, Vector3.down, playerHight * 0.5f + 0.01f, whatIsGround);

        // تصفير السرعة العمودية عند ملامسة الأرض
        if (isGrounded && velocityY < 0)
        {
            velocityY = 0;
        }

        ApplyGravity();

        // تنفيذ القفز
        if (isGrounded && jumpRequested)
        {
            HandleJumpInput();
            jumpRequested = false;
        }
        else if (isGrounded)
        {
            jumpRequested = false; // إلغاء الطلب إذا مر الوقت وهو على الأرض
        }
    }

    private void CalculateGravityAndJumpVelocity()
    {
        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
    }

    private void HandleJumpInput()
    {
        velocityY = jumpVelocity;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, velocityY, rb.linearVelocity.z);
    }

    private void ApplyGravity()
    {
        if (isGrounded) { return; } // الاعتماد على Raycast بدلاً من الارتفاع الثابت
        velocityY += gravity * Time.deltaTime;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, velocityY, rb.linearVelocity.z);
    }
}