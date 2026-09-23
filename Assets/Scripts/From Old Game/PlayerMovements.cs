using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class PlayerMovements : MonoBehaviour
{
    [SerializeField] float forceValue = 10;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheckPos;
    [SerializeField] float playerHight;
    [SerializeField] LayerMask whatIsGround;

    [Header("Slide Settings")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private float fallDuration = 0.15f;
    [SerializeField] private float slideDuration = 0.8f;

    [Header("Collider Settings")]
    [SerializeField] private CapsuleCollider playerCollider;
    [SerializeField] private float slideColliderHeight = 1f; // ارتفاع المصادم أثناء الانزلاق
    [SerializeField] private Vector3 slideColliderCenter = new Vector3(0, 0.5f, 0); // مركز المصادم أثناء الانزلاق

    Rigidbody rb;
    public static bool isAllowToMove;
    private bool isSliding = false;

    float currentXPos, middleXPos, rightXPos, leftXPos;

    // متغيرات لحفظ الأبعاد الأصلية للمصادم
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // إذا لم تقم بربط المصادم من الـ Inspector، سيبحث عنه الكود
        if (playerCollider == null)
            playerCollider = GetComponent<CapsuleCollider>();

        // حفظ الأبعاد الأصلية لاسترجاعها بعد الانزلاق
        if (playerCollider != null)
        {
            originalColliderHeight = playerCollider.height;
            originalColliderCenter = playerCollider.center;
        }
    }

    void Start()
    {
        isAllowToMove = true;

        currentXPos = transform.position.x;
        middleXPos = 0f;
        rightXPos = 4f;
        leftXPos = -4f;

        if (Swipe.Instance != null)
        {
            Swipe.Instance.OnSwipeDown += HandleSwipeDown;
            Swipe.Instance.OnSwipeLeft += HandleSwipeLeft;
            Swipe.Instance.OnSwipeRight += HandleSwipeRight;
        }
    }

    private void OnDestroy()
    {
        if (Swipe.Instance != null)
        {
            Swipe.Instance.OnSwipeDown -= HandleSwipeDown;
            Swipe.Instance.OnSwipeLeft -= HandleSwipeLeft;
            Swipe.Instance.OnSwipeRight -= HandleSwipeRight;
        }
    }

    private void HandleSwipeRight(object sender, EventArgs e)
    {
        if (!isAllowToMove) return;

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

    private void HandleSwipeLeft(object sender, EventArgs e)
    {
        if (!isAllowToMove) return;

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

    private void HandleSwipeDown(object sender, EventArgs e)
    {
        if (!isAllowToMove || isSliding) return;
        StartCoroutine(SlideDown());
    }

    void Update()
    {
        if (!isAllowToMove) { return; }
    }

    IEnumerator SlideDown()
    {
        isSliding = true;

        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger("Run");
            playerAnimator.ResetTrigger("Jump");
            playerAnimator.SetTrigger("Slide");
        }

        // تغيير أبعاد المصادم ليتماشى مع حركة الانزلاق
        if (playerCollider != null)
        {
            playerCollider.height = slideColliderHeight;
            playerCollider.center = slideColliderCenter;
        }

        if (Physics.Raycast(groundCheckPos.position, Vector3.down, out RaycastHit hitInfo, 10f, whatIsGround))
        {
            float groundYPos = hitInfo.point.y;
            float targetYPosition = groundYPos + (playerHight * 0.5f);

            if (transform.position.y > targetYPosition + 0.1f)
            {
                transform.DOMoveY(targetYPosition, fallDuration).SetEase(Ease.OutCubic);
            }
        }

        yield return new WaitForSeconds(slideDuration);

        // إرجاع المصادم لشكله الأصلي بعد انتهاء الانزلاق
        if (playerCollider != null)
        {
            playerCollider.height = originalColliderHeight;
            playerCollider.center = originalColliderCenter;
        }

        isSliding = false;
    }

    public void GoBackToRunning()
    {
        playerAnimator.SetTrigger("Run");
        playerAnimator.ResetTrigger("Slide");
        playerAnimator.ResetTrigger("Jump");

        // تأمين إضافي لإرجاع المصادم في حال تم قطع حركة الانزلاق قسرياً
        if (playerCollider != null)
        {
            playerCollider.height = originalColliderHeight;
            playerCollider.center = originalColliderCenter;
        }
    }
}