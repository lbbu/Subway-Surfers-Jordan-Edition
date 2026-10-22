using System;
using UnityEngine;

public class JumpModified : MonoBehaviour
{
    public float jumpHeight = 2f;
    public float jumpForce = 10f;
    public float timeToJumpApex = 0.4f;

    private float gravity;
    private float jumpVelocity;
    private float velocityY;
    private Rigidbody rb;
    private bool jumpRequested; // متغير لالتقاط الإدخال
    private bool jumping = false;
    private bool leftGround = false;
    [SerializeField] private Animator playerAnimator;


    [Header("Ground Check")]
    [SerializeField] Transform groundCheckPos;
    [SerializeField] float playerHeight;
    [SerializeField] LayerMask whatIsGround;
    bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (!playerAnimator)
            playerAnimator = GetComponent<Animator>();
        rb.useGravity = false; // إيقاف جاذبية يونيتي لمنع التعارض
    }

    private void Start()
    {
        CalculateGravityAndJumpVelocity();

        Swipe.Instance.OnSwipeUp += HandleSwipeUp;

    }

    private void HandleSwipeUp(object sender, EventArgs e)
    {
        //Debug.Log($"[HandleSwipeUp] Event Swipe UP Recived!");
        jumpRequested = true;
    }

    private void Update()
    {

    }

    private void FixedUpdate()
    {
        
        if (!PlayerMovementModified.isAllowToMove) { return; }

        float rayLength = playerHeight * 0.5f + 0.01f;
        isGrounded = Physics.Raycast(groundCheckPos.position, Vector3.down, rayLength, whatIsGround);

        Debug.DrawRay(groundCheckPos.position, Vector3.down * rayLength, isGrounded ? Color.green : Color.red);
        // تصفير السرعة العمودية عند ملامسة الأرض
        if (isGrounded && velocityY < 0)
        {
            velocityY = 0;
        }
        
        ApplyGravity();
        //Debug.Log($"Method stops here{isGrounded}");
        // تنفيذ القفز
        if (isGrounded && jumpRequested)
        {
            HandleJumpInput();
            jumpRequested = false; // إلغاء الطلب إذا مر الوقت وهو على الأرض
        }

        if (jumping)
        {
            if (!isGrounded)
            {
                leftGround = true;
            }
            else if (leftGround && velocityY <= 0)
            {
                //Debug.Log($"[jump] landed");
                jumping = false;
                leftGround = false;
                isGrounded = true;
                GoBackToRunning();
            }
        }
    }

    private void CalculateGravityAndJumpVelocity()
    {
        gravity = -9.8f;
        jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
    }

    private void HandleJumpInput()
    {
        //Debug.Log($"[HandleJumpInput] JUMP!");
        playerAnimator.ResetTrigger("Run");
        playerAnimator.ResetTrigger("Slide");
        playerAnimator.SetTrigger("Jump");
        velocityY = jumpVelocity;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, velocityY, rb.linearVelocity.z);
        jumping = true;
    }

    private void ApplyGravity()
    {
        if (isGrounded) { return; } // الاعتماد على Raycast بدلاً من الارتفاع الثابت
        velocityY += gravity * Time.deltaTime;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, velocityY, rb.linearVelocity.z);
    }

    public void GoBackToRunning()
    {
        playerAnimator.SetTrigger("Run");
        playerAnimator.ResetTrigger("Slide");
        playerAnimator.ResetTrigger("Jump");

    }
}
