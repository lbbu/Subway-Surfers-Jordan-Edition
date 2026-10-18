using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SubwayRunnerMovement : MonoBehaviour
{
    [Header("إعدادات السرعة والمسارات")]
    public float forwardSpeed = 10f;    // سرعة الجري للأمام تلقائياً
    public float laneDistance = 3f;     // المسافة بين كل مسار وآخر
    public float laneChangeSpeed = 10f; // سرعة الانتقال بين المسارات

    [Header("إعدادات القفز والجاذبية")]
    public float jumpForce = 7f;
    public float gravity = 20f;
    private float yVelocity;

    [Header("إعدادات الانزلاق (Slide)")]
    public float slideDuration = 1f;    // مدة الانزلاق بالثواني
    private bool isSliding = false;
    private float slideTimer;

    // المسار الحالي: 0 = يسار، 1 = منتصف، 2 = يمين
    private int currentLane = 1;

    // متغيرات اكتشاف السحب باللمس
    private Vector2 touchStartPosition;
    private Vector2 touchEndPosition;
    public float minSwipeDistance = 50f; // الحد الأدنى لمسافة السحب لاعتبارها حركة

    private CharacterController controller;
    private float originalHeight;
    private Vector3 originalCenter;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        // حفظ أبعاد كبسولة اللاعب الأصلية لاستعادتها بعد الانزلاق
        originalHeight = controller.height;
        originalCenter = controller.center;
    }

    void Update()
    {
        // 1. اكتشاف السحب عن طريق اللمس
        DetectSwipe();

        // 2. حساب موقع الهدف بناءً على المسار الحالي (X Axis)
        Vector3 targetPosition = transform.position;

        if (currentLane == 0)
            targetPosition.x = -laneDistance; // مسار اليسار
        else if (currentLane == 1)
            targetPosition.x = 0;             // مسار المنتصف
        else if (currentLane == 2)
            targetPosition.x = laneDistance;  // مسار اليمين

        // 3. حساب حركة الـ Y (القفز والجاذبية)
        if (controller.isGrounded)
        {
            if (yVelocity < 0) yVelocity = -1f; // تثبيت اللاعب على الأرض
        }
        else
        {
            yVelocity -= gravity * Time.deltaTime; // تطبيق الجاذبية
        }

        // 4. إدارة مؤقت الانزلاق
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0)
            {
                StopSlide();
            }
        }

        // 5. دمج الحركة الشاملة (الأمام + الانتقال بين المسارات + القفز)
        Vector3 moveVector = Vector3.zero;

        // الحركة للأمام
        moveVector.z = forwardSpeed;

        // التنعيم والانتقال السلس بين المسارات
        float xPosition = Mathf.Lerp(transform.position.x, targetPosition.x, Time.deltaTime * laneChangeSpeed);
        moveVector.x = (xPosition - transform.position.x) / Time.deltaTime;

        // إضافة حركة القفز/السقوط
        moveVector.y = yVelocity;

        // تطبيق الحركة
        controller.Move(moveVector * Time.deltaTime);
    }

    // --- نظام اكتشاف السحب (Swipe Detection) ---
    void DetectSwipe()
    {
        // إذا كان هناك لمس للشاشة
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                touchEndPosition = touch.position;
                AnalyzeSwipe();
            }
        }

        // ** ميزة إضافية للتحكم بالأسهم من الكومبيوتر أثناء التطوير والاختبار **
        if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveLane(false);
        if (Input.GetKeyDown(KeyCode.RightArrow)) MoveLane(true);
        if (Input.GetKeyDown(KeyCode.UpArrow)) Jump();
        if (Input.GetKeyDown(KeyCode.DownArrow)) Slide();
    }

    void AnalyzeSwipe()
    {
        Vector2 swipeDelta = touchEndPosition - touchStartPosition;

        // التأكد من أن مسافة السحب كافية
        if (swipeDelta.magnitude < minSwipeDistance) return;

        // تحديد ما إذا كان السحب أفقياً أم عمودياً
        if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
        {
            // سحب أفقي (يمين أو يسار)
            if (swipeDelta.x > 0)
                MoveLane(true);  // يمين
            else
                MoveLane(false); // يسار
        }
        else
        {
            // سحب عمودي (أعلى أو أسفل)
            if (swipeDelta.y > 0)
                Jump();  // أعلى
            else
                Slide(); // أسفل
        }
    }

    // --- أفعال الشخصية ---
    void MoveLane(bool goingRight)
    {
        if (goingRight && currentLane < 2)
        {
            currentLane++;
        }
        else if (!goingRight && currentLane > 0)
        {
            currentLane--;
        }
    }

    void Jump()
    {
        if (controller.isGrounded)
        {
            yVelocity = jumpForce;
            if (isSliding) StopSlide(); // إلغاء الانزلاق إذا قفز
        }
    }

    void Slide()
    {
        if (!isSliding)
        {
            isSliding = true;
            slideTimer = slideDuration;

            // تصغير حجم اصطدام الكبسولة للنصف ليمر من تحت العوائق
            controller.height = originalHeight / 2f;
            controller.center = new Vector3(originalCenter.x, originalCenter.y / 2f, originalCenter.z);

            // إذا كان في الهواء وطلب الانزلاق، يسقط بسرعة للأرض
            if (!controller.isGrounded)
            {
                yVelocity = -jumpForce * 2f;
            }
        }
    }

    void StopSlide()
    {
        isSliding = false;
        // إعادة الحجم الطبيعي للاعب
        controller.height = originalHeight;
        controller.center = originalCenter;
    }
}