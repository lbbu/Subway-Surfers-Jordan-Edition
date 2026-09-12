using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerMovements : MonoBehaviour
{
    
    [SerializeField] float forceValue = 10;
    //[SerializeField] GameTutorial gameTutorial;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheckPos;
    [SerializeField] float playerHight;
    [SerializeField] LayerMask whatIsGround;
    bool isGrounded;



    Rigidbody rb;
    Swipe swipeControls;

    
    public static bool isAllowToMove;

    

    float currentXPos, middleXPos, rightXPos, leftXPos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        swipeControls = GetComponent<Swipe>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // reset enemy and ground speed when u start the game
        //GroundMovements.groundSpeed = 10f;

        isAllowToMove = true;

        currentXPos = transform.position.x;
        middleXPos = 0f;
        rightXPos = 4f;
        leftXPos = -4f;

    }

    // Update is called once per frame
    void Update()
    {

        if (!isAllowToMove) { return; }

        //InputMoveForTest();


        MoveRight();
        MoveLeft();
        MoveDown();



    }


    void MoveRight()
    {
        if (!swipeControls.SwipeRight) { return; }

        if (currentXPos < middleXPos)
        {
            transform.DOMoveX(middleXPos, 0.5f, false);
            currentXPos = middleXPos;
        }
        else if (currentXPos < rightXPos)
        {
            transform.DOMoveX(rightXPos, 0.5f, false);
            currentXPos = rightXPos;
        }

    }


    void MoveLeft()
    {

        if (!swipeControls.SwipeLeft) { return; }

        if (currentXPos > middleXPos)
        {
            transform.DOMoveX(middleXPos, 0.5f, false);
            currentXPos = middleXPos;
        }
        else if (currentXPos > leftXPos)
        {
            transform.DOMoveX(leftXPos, 0.5f, false);
            currentXPos = leftXPos;
        }

    }

    void MoveDown()
    {
        if (swipeControls.SwipeDown)
        {
            StartCoroutine(SlideDown());
        }
    }

    IEnumerator SlideDown()
    {
        rb.freezeRotation = false;
        transform.DORotate(new Vector3(-90, 0, 0), 0.2f, RotateMode.Fast);

        Physics.Raycast(groundCheckPos.position, Vector3.down, out RaycastHit hitInfo,
            playerHight * 4f, whatIsGround);
        float groundYPos = hitInfo.transform.position.y;
        transform.DOMoveY(groundYPos + 0.5f, transform.position.y * 0.1f, false);

        yield return new WaitForSecondsRealtime(0.8f);

        transform.DORotate(new Vector3(0, 0, 0), 0.2f, RotateMode.Fast);
        rb.freezeRotation = true;

    }

    

}
