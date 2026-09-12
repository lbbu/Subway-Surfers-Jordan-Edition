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
    }

    private void Start()
    {
        CalculateGravityAndJumpVelocity();
    }



    private void FixedUpdate()
    {

        if (!PlayerMovements.isAllowToMove) { return; }

        isGrounded = Physics.Raycast(groundCheckPos.position, Vector3.down, playerHight * 0.5f + 0.01f, whatIsGround);

        ApplyGravity();
        if (!isGrounded) { return; }
        //rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ
        // | RigidbodyConstraints.FreezeRotation;
        if (swipeControls.SwipeUp)
        {
            //rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
            HandleJumpInput();

        }




    }

    private void CalculateGravityAndJumpVelocity()
    {


        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
    }

    private void HandleJumpInput()
    {
        /*
        if (Input.GetKeyDown(KeyCode.Space) && rb.velocity.y == 0)
        {
            velocityY = jumpVelocity;
            rb.velocity = new Vector3(rb.velocity.x, velocityY, rb.velocity.z);
        }
        */

        velocityY = jumpVelocity;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, velocityY, rb.linearVelocity.z);
    }

    private void ApplyGravity()
    {
        if (gameObject.transform.position.y <= 0.51f) { return; }
        velocityY += gravity * Time.deltaTime;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, velocityY, rb.linearVelocity.z);
    }


}
