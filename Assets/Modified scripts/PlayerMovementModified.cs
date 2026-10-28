using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;


public class PlayerMovementModified : MonoBehaviour
{
    // قوة الدفع للأمام (متروكة لك إذا كنت تستخدمها لتحريك اللاعب المستمر)
    [SerializeField] float forceValue = 10;

    [Header("Ground Check")]
    // نقطة وهمية نضعها عند قدم اللاعب لإطلاق شعاع يكتشف الأرض
    [SerializeField] Transform groundCheckPos;
    // طول اللاعب (يستخدم لضمان عدم اكتشاف أسطح بعيدة جداً)
    [SerializeField] float playerHight;
    // تحديد ما هي الطبقة (Layer) التي نعتبرها أرضاً (مثل الشارع أو سقف الباص)
    [SerializeField] LayerMask whatIsGround;

    [Header("Slide Settings")]
    // متحكم الأنيميشن الخاص باللاعب
    [SerializeField] private Animator playerAnimator;
    // سرعة الهبوط للأرض إذا قام اللاعب بالتزحلق وهو في الهواء
    [SerializeField] private float fallDuration = 0.15f;
    // مدة التزحلق الكلية قبل أن يعود اللاعب لوضع الركض
    [SerializeField] private float slideDuration = 0.8f;

    [Header("Collider Settings")]
    // المصادم الكبسولي الخاص باللاعب
    [SerializeField] private CapsuleCollider playerCollider;
    // الارتفاع الجديد للمصادم أثناء التزحلق (لكي لا يصطدم بالعقبات العالية)
    [SerializeField] private float slideColliderHeight = 1f;
    // المركز الجديد للمصادم أثناء التزحلق (للحفاظ على أسفل الكبسولة ملامساً للأرض)
    [SerializeField] private Vector3 slideColliderCenter = new Vector3(0, 0.5f, 0);

    // مرجع للمحرك الفيزيائي
    Rigidbody rb;

    // متغير عام للتحكم بحركة اللاعب (يمكن استخدامه لإيقاف اللعبة عند الخسارة)
    public static bool isAllowToMove;

    // متغير عام لمعرفة ما إذا كان اللاعب يتزحلق (لتجنب تداخل الحركات)
    // تم جعله public static لكي تتمكن من قراءته في سكريبت القفز إذا احتجت لذلك
    public static bool isSliding = false;


    // متغيرات لحفظ أبعاد المصادم الأصلية لاسترجاعها بعد التزحلق
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;


    Rigidbody[] boneBodies;
    Collider[] boneColliders;
    private void Awake()
    {
        // جلب المكونات المرتبطة باللاعب تلقائياً إذا لم يتم سحبها في الـ Inspector
        rb = GetComponent<Rigidbody>();

        if (!playerAnimator)
            playerAnimator = GetComponent<Animator>();

        if (!playerCollider)
            playerCollider = GetComponent<CapsuleCollider>();

        // حفظ الأبعاد الأصلية للمصادم قبل بدء اللعب
        if (playerCollider != null)
        {
            originalColliderHeight = playerCollider.height;
            originalColliderCenter = playerCollider.center;
        }

        boneBodies = System.Array.FindAll(GetComponentsInChildren<Rigidbody>(), r => r != rb);
        boneColliders = System.Array.FindAll(GetComponentsInChildren<Collider>(), c => c != playerCollider);

        SetRagdoll(false);
    }

    void Start()
    {
        particle.SetActive(false);
        isAllowToMove = true;

        // الاشتراك في أحداث السحب (Swipes) من سكريبت إدارة اللمس
        if (Swipe.Instance != null)
        {
            Swipe.Instance.OnSwipeDown += HandleSwipeDown;
            Swipe.Instance.OnSwipeLeft += HandleSwipeLeft;
            Swipe.Instance.OnSwipeRight += HandleSwipeRight;
        }
    }

    private void OnDestroy()
    {
        // إلغاء الاشتراك من الأحداث عند تدمير اللاعب أو إعادة تشغيل المرحلة لمنع أخطاء الذاكرة
        if (Swipe.Instance != null)
        {
            Swipe.Instance.OnSwipeDown -= HandleSwipeDown;
            Swipe.Instance.OnSwipeLeft -= HandleSwipeLeft;
            Swipe.Instance.OnSwipeRight -= HandleSwipeRight;
        }
    }


    #region Updated Move L,R (AHMED)
    [SerializeField] float forwardOffset = 0.5f;
    [SerializeField] float radius = 0.5f;
    [SerializeField] float height = 2f;

    [SerializeField] float stepHeight = 0.3f;
    [SerializeField] LayerMask obstacleMask;

    [SerializeField] float stumbleWindow = 2.5f;
    float stumbleTimer;
    bool IsStumbling => stumbleTimer > 0f;

    void FixedUpdate()
    {
        if (!isAllowToMove) return;

        if (stumbleTimer > 0f) stumbleTimer -= Time.fixedDeltaTime;

        if (FrontHit()) Die();
    }
    bool FrontHit()
    {
        Vector3 basePos = transform.position + transform.forward * forwardOffset;

        float h = playerCollider.height;
        Vector3 bottom = basePos + Vector3.up * (stepHeight + radius);
        Vector3 top = basePos + Vector3.up * Mathf.Max(h - radius, stepHeight + radius);

        return Physics.CheckCapsule(bottom, top, radius, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    [SerializeField] float sideStepHeight = 0.3f;
    [SerializeField] float sideRadius = 0.4f;

    bool SideCast(Vector3 dir, float dist, out RaycastHit hit)
    {
        Vector3 basePos = rb.position;
        float h = playerCollider.height; 

        Vector3 bottom = basePos + Vector3.up * (sideStepHeight + sideRadius);
        Vector3 top = basePos + Vector3.up * Mathf.Max(h - sideRadius, sideStepHeight + sideRadius);

        return Physics.CapsuleCast(bottom, top, sideRadius, dir, out hit, dist,
                                   whatIsGround, QueryTriggerInteraction.Ignore);
    }
    bool OnSideHit()
    {
        if (IsStumbling) {  return true; }
        stumbleTimer = stumbleWindow;
        playerAnimator.SetTrigger("Stumble");
        return false;

    }
    public GameObject particle;
    void Die()
    {
        isAllowToMove = false;
        laneTween?.Kill();

        playerAnimator.enabled = false;
        playerCollider.enabled = false;
        rb.isKinematic = true;

        CameraShakeManager.instance.PlayBump();
        CameraShakeManager.instance.DeathCamera();
        SetRagdoll(true);

        foreach (var b in boneBodies)
            b.linearVelocity = Vector3.forward * ChunkManager.instance.chunksSpeed;

        ChunkManager.instance.chunksSpeed = 0f;
        ChunkManager.instance.accelarate = 0f;
        particle.SetActive(true);

    }
    void SetRagdoll(bool on)
    {
        foreach (var b in boneBodies)
        {
            b.isKinematic = !on;
            b.detectCollisions = on;
        }
        foreach (var c in boneColliders)
            c.enabled = on;
    }


    // سبب الاستخدام للحرص على وضع قيمة معينة لضمان
    // وجوداللاعب في تلك النقطة دون اي اخطاء
    // و يمكن تغيرها عند تغير حجم الخريطة الاصلي

    [SerializeField] private float[] laneX = { -4.5f, 0.5f, 4.5f }; // يمين ,منتصف ,يسار
    [SerializeField] private float laneChangeDuration = 0.5f; // المدة التي يحتاجها ليصل لالخط او الطريق الثاني
    [SerializeField] private Transform checkPos;
    private int currentLane = 1; // ابدء بالنصف

    private void HandleSwipeRight(object sender, EventArgs e)
    {
        ChangeLane(1);
    }

    private void HandleSwipeLeft(object sender, EventArgs e)
    {
        ChangeLane(-1);
    }
    //خزن التويين لازالته عند تفعيل العملية بسرعة
    //لتفادي تفعيل اثنتين سوياا و لتفادي الاخطاء
    private Tween laneTween;
    private bool hitObsticle = false;
    private bool stubmle = false;
    private void ChangeLane(int direction)
    {
        if (!isAllowToMove) return;
        if (stubmle) return;
        int targetLane = Mathf.Clamp(currentLane + direction, 0, laneX.Length - 1);
        float laneGap = Mathf.Abs(laneX[targetLane] - laneX[currentLane]);
        laneTween?.Kill();
        Vector3 rayDic = direction == 1 ? Vector3.right : Vector3.left;
        hitObsticle = Physics.Raycast(checkPos.position, rayDic, laneGap, whatIsGround);
        Vector3 end = new Vector3(rayDic.x * laneGap, rayDic.y, rayDic.z);
        Debug.DrawLine(checkPos.position, end, Color.red);
        if (hitObsticle)
        {

            if (SideCast(rayDic, laneGap, out RaycastHit hit))
            {
                bool fatal = OnSideHit();
                float distance = hit.distance;
                stubmle = true;
                Sequence seq = DOTween.Sequence();
                // الحساب الصحيح اولا احسب سرعة تغير الطريق ثم احسب المسافة و هكذا احصل على وقت اصح
                // (باستعمال معادلة: (السرعة = المسافة *الزمن
                float speed = laneGap / laneChangeDuration;

                float startX = rb.position.x;
                float targetX = laneX[currentLane] + distance * direction;
                float homeX = laneX[currentLane];

                float outTime = Mathf.Abs(targetX - startX) / speed;
                float backTime = Mathf.Abs(homeX - targetX) / speed;
                Debug.Log($"Hit {hit.collider.gameObject.name} laneGap = {laneGap} distance {hit.distance} outTime = {outTime} backTime {backTime}");
                seq.Append(rb.DOMoveX(targetX, outTime)
                              .SetEase(Ease.OutQuad)
                              .SetUpdate(UpdateType.Fixed));
                seq.AppendCallback(() =>
                {
                    Debug.Log("Bumping");
                    CameraShakeManager.instance.PlayBump();
                });

                if (fatal)
                {
                    
                    seq.OnComplete(Die);
                }
                else
                {
                    seq.Append(rb.DOMoveX(homeX, backTime)
                                 .SetEase(Ease.OutQuad)
                                 .SetUpdate(UpdateType.Fixed))
                       .OnComplete(() => stubmle = false);
                    
                }

            }
        }
        else
        {
            if (targetLane == currentLane) return;

            currentLane = targetLane;
            //قتل التويين الحيوان لابعاد البقز

            laneTween = rb.DOMoveX(laneX[currentLane], laneChangeDuration)
                          .SetEase(Ease.OutQuad)
                          .SetUpdate(UpdateType.Fixed);
        }

    }
    #endregion

    #region OLD Swipe L,R
    //private void HandleSwipeRight(object sender, EventArgs e)
    //{
    //    if (!isAllowToMove) return;

    //    // التحقق من موقع اللاعب الحالي ونقله للمسار الأيمن باستخدام rb.DOMoveX (لأداء فيزيائي أفضل)
    //    if (currentXPos < middleXPos)
    //    {
    //        rb.DOMoveX(middleXPos, 0.5f, false);
    //        currentXPos = middleXPos;
    //    }
    //    else if (currentXPos < rightXPos)
    //    {
    //        rb.DOMoveX(rightXPos, 0.5f, false);
    //        currentXPos = rightXPos;
    //    }
    //}

    //private void HandleSwipeLeft(object sender, EventArgs e)
    //{
    //    if (!isAllowToMove) return;

    //    // التحقق من موقع اللاعب الحالي ونقله للمسار الأيسر باستخدام rb.DOMoveX
    //    if (currentXPos > middleXPos)
    //    {
    //        rb.DOMoveX(middleXPos, 0.5f, false);
    //        currentXPos = middleXPos;
    //    }
    //    else if (currentXPos > leftXPos)
    //    {
    //        rb.DOMoveX(leftXPos, 0.5f, false);
    //        currentXPos = leftXPos;
    //    }
    //}

    #endregion

    private void HandleSwipeDown(object sender, EventArgs e)
    {
        // منع التزحلق إذا كان اللاعب يتزحلق مسبقاً أو غير مسموح له بالحركة
        if (!isAllowToMove || isSliding) return;

        StartCoroutine(SlideDown());
    }

    void Update()
    {
        if (!isAllowToMove) { return; }

        // نظام حماية (Failsafe): إذا سقط اللاعب تحت الخريطة يتم إعادته للسطح
        if (transform.position.y < -5)
        {
            transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        }
    }

    IEnumerator SlideDown()
    {
        isSliding = true;

        // 1. إعادة ضبط كل الحركات وتشغيل حركة التزحلق
        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger("Run");
            playerAnimator.ResetTrigger("Jump");
            playerAnimator.SetTrigger("Slide");
        }

        // 2. تصغير المصادم لتفادي العقبات العالية
        if (playerCollider != null)
        {
            playerCollider.height = slideColliderHeight;
            playerCollider.center = slideColliderCenter;
        }

        // 3. إطلاق شعاع لاكتشاف الأرضية تحته (سواء كانت الأرض العادية أو سقف باص)
        if (Physics.Raycast(groundCheckPos.position, Vector3.down, out RaycastHit hitInfo, 10f, whatIsGround))
        {
            // النقطة المستهدفة هي مكان الاصطدام + 0.05 لرفع المجسم شعرة عن الأرض ومنع ارتداد الفيزياء
            float targetYPosition = hitInfo.point.y + 0.05f;

            // إذا كان اللاعب أعلى من الأرض بمسافة معينة (في وضع قفز مثلاً)
            if (transform.position.y > targetYPosition + 0.1f)
            {
                // تصفير السرعة العمودية لمنع تعارض الجاذبية مع حركة DOTween
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

                // استخدام rb.DOMoveY بدلاً من transform.DOMoveY لمنع الاختراق الفيزيائي (Tunneling)
                rb.DOMoveY(targetYPosition, fallDuration).SetEase(Ease.Linear);
            }
        }

        // 4. الانتظار حتى تنتهي مدة التزحلق (نفس مدة الأنيميشن تقريباً)
        yield return new WaitForSeconds(slideDuration);

        // 5. استرجاع الحجم الطبيعي للمصادم
        if (playerCollider != null)
        {
            playerCollider.height = originalColliderHeight;
            playerCollider.center = originalColliderCenter;
        }
        GoBackToRunning();
        isSliding = false;
    }

    // دالة لاسترجاع حالة الركض بشكل قسري (تُستدعى عادة من أنيميشن إيفنت أو عند الاصطدام)
    public void GoBackToRunning()
    {
        playerAnimator.SetTrigger("Run");
        playerAnimator.ResetTrigger("Slide");
        playerAnimator.ResetTrigger("Jump");

        // استرجاع الحجم الطبيعي للمصادم للحماية
        if (playerCollider != null)
        {
            playerCollider.height = originalColliderHeight;
            playerCollider.center = originalColliderCenter;
        }
    }

    // Claude Debug for side colliders
    void GetSideCapsule(Vector3 basePos, out Vector3 bottom, out Vector3 top)
    {
        float h = playerCollider != null ? playerCollider.height : height;
        bottom = basePos + Vector3.up * (sideStepHeight + sideRadius);
        top = basePos + Vector3.up * Mathf.Max(h - sideRadius, sideStepHeight + sideRadius);
    }
    void DrawCapsuleGizmo(Vector3 bottom, Vector3 top, float r)
    {
        Gizmos.DrawWireSphere(bottom, r);
        Gizmos.DrawWireSphere(top, r);
        // four lines connecting the two spheres so it reads as a capsule
        Gizmos.DrawLine(bottom + Vector3.right * r, top + Vector3.right * r);
        Gizmos.DrawLine(bottom + Vector3.left * r, top + Vector3.left * r);
        Gizmos.DrawLine(bottom + Vector3.forward * r, top + Vector3.forward * r);
        Gizmos.DrawLine(bottom + Vector3.back * r, top + Vector3.back * r);
    }
    void OnDrawGizmosSelected()
    {
        Vector3 basePos = rb != null ? rb.position : transform.position;

        // current position
        GetSideCapsule(basePos, out Vector3 b0, out Vector3 t0);
        Gizmos.color = Color.white;
        DrawCapsuleGizmo(b0, t0, sideRadius);

        // preview both directions
        for (int d = -1; d <= 1; d += 2)
        {
            int targetLane = Mathf.Clamp(currentLane + d, 0, laneX.Length - 1);
            if (targetLane == currentLane) continue;   // no lane that way

            float laneGap = Mathf.Abs(laneX[targetLane] - laneX[currentLane]);
            Vector3 dir = d == 1 ? Vector3.right : Vector3.left;

            bool didHit = Physics.CapsuleCast(b0, t0, sideRadius, dir, out RaycastHit hit,
                                              laneGap, obstacleMask, QueryTriggerInteraction.Ignore);

            float travel = didHit ? hit.distance : laneGap;
            Vector3 landPos = basePos + dir * travel;

            GetSideCapsule(landPos, out Vector3 b1, out Vector3 t1);

            Gizmos.color = didHit ? Color.red : Color.green;
            DrawCapsuleGizmo(b1, t1, sideRadius);
            Gizmos.DrawLine(b0, b1);   // path of the bottom sphere
            Gizmos.DrawLine(t0, t1);   // path of the top sphere

            if (didHit)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(hit.point, 0.08f);
            }
        }
    }
}
