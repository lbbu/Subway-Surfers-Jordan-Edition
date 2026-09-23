using System;
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

    [Header("Slide Settings")]
    [SerializeField] float slideDistance = 5f;   // مسافة الانزلاق للأمام
    [SerializeField] float slideDuration = 0.8f;  // مدة الانزلاق بالثواني

    Rigidbody rb;

    public static bool isAllowToMove;

    float currentXPos, middleXPos, rightXPos, leftXPos;
    bool isSliding = false;

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
        rb.freezeRotation = false;

        // حفظ الدوران الأصلي بالكامل لاسترجاعه بدقة بعد الانتهاء
        Quaternion originalRotation = transform.rotation;

        // 1. تدوير اللاعب للأمام بالنسبة لاتجاهه المحلي الحالي (Local Rotation)
        transform.DORotateQuaternion(originalRotation * Quaternion.Euler(-90, 0, 0), 0.2f);

        // 2. خفض اللاعب باتجاه الأرض
        if (Physics.Raycast(groundCheckPos.position, Vector3.down, out RaycastHit hitInfo, playerHight * 4f, whatIsGround))
        {
            float groundYPos = hitInfo.point.y;
            transform.DOMoveY(groundYPos + 0.5f, 0.15f, false);
        }

        // 3. دفع اللاعب باتجاه وجهه الأمامي الحالي وليس فقط محور Z العالمي
        Vector3 targetPos = transform.position + transform.forward * slideDistance;
        transform.DOMoveZ(targetPos.z, slideDuration).SetEase(Ease.OutQuad);

        // الانتظار حتى تنتهي مدة الانزلاق
        yield return new WaitForSeconds(slideDuration);

        // 4. إرجاع الدوران والدوران الثابت للـ Rigidbody
        transform.DORotateQuaternion(originalRotation, 0.2f);
        rb.freezeRotation = true;
        isSliding = false;
    }
}