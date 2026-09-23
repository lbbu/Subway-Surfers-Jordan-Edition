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

    Rigidbody rb;
    public static bool isAllowToMove;
    private bool isSliding = false;

    float currentXPos, middleXPos, rightXPos, leftXPos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        isAllowToMove = true;

        currentXPos = transform.position.x;
        middleXPos = 0f;
        rightXPos = 4f;
        leftXPos = -4f;

        // الاشتراك بأحداث السحب
        if (Swipe.Instance != null)
        {
            Swipe.Instance.OnSwipeDown += HandleSwipeDown;
            Swipe.Instance.OnSwipeLeft += HandleSwipeLeft;
            Swipe.Instance.OnSwipeRight += HandleSwipeRight;
        }
    }

    private void OnDestroy()
    {
        // ممارسة برمجية أساسية: فك الارتباط بالأحداث لتجنب تسريب الذاكرة (Memory Leaks)
        if (Swipe.Instance != null)
        {
            Swipe.Instance.OnSwipeDown -= HandleSwipeDown;
            Swipe.Instance.OnSwipeLeft -= HandleSwipeLeft;
            Swipe.Instance.OnSwipeRight -= HandleSwipeRight;
        }
    }

    private void HandleSwipeRight(object sender, EventArgs e)
    {
        // إزالة شرط isSliding للسماح بالحركة أثناء الانزلاق
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
        // إزالة شرط isSliding للسماح بالحركة أثناء الانزلاق
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
        // إبقاء الشرط هنا لمنع تكرار الانزلاق أثناء الانزلاق الحالي
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

        // 1. تشغيل الأنيميشن بدلاً من تدوير المجسم
        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger("Run");
            playerAnimator.ResetTrigger("Jump");
            playerAnimator.SetTrigger("Slide");
        }

        // 2. الهبوط السريع للأرض في حال كان اللاعب يقفز
        if (Physics.Raycast(groundCheckPos.position, Vector3.down, out RaycastHit hitInfo, 10f, whatIsGround))
        {
            float groundYPos = hitInfo.point.y;
            float targetYPosition = groundYPos + (playerHight * 0.5f);

            // إذا كان اللاعب أعلى من الأرض بمسافة ملحوظة، ننزله بسرعة
            if (transform.position.y > targetYPosition + 0.1f)
            {
                transform.DOMoveY(targetYPosition, fallDuration).SetEase(Ease.OutCubic);
            }
        }

        // الانتظار حتى ينتهي الأنيميشن
        yield return new WaitForSeconds(slideDuration);

        isSliding = false;
    }

    public void GoBackToRunning()
    {
        playerAnimator.SetTrigger("Run");
        playerAnimator.ResetTrigger("Slide");
        playerAnimator.ResetTrigger("Jump");
    }
}