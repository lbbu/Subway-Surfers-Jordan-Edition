using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>حركة اللاعب واصطدامه على X وY، بينما يتحرك العالم للخلف ويبقى موضع Z ثابتًا.</summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovements : MonoBehaviour
{
    public enum MovementState { Running, Jumping, Falling, Sliding, Diving, Stopped }

    [Header("Moving world (player Z stays at its spawn position)")]
    [Tooltip("Optional. Reads speed only; never moves, accelerates, or edits the manager.")]
    [SerializeField] private RunnerWorldSource worldSpeedSource;
    [FormerlySerializedAs("readChunkManagerSpeed")]
    [SerializeField] private bool readWorldSpeed = true;
    [SerializeField] private Swipe inputSource;
    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private float fallOutHeight = -10f;

    [Header("Lanes (world X, left to right)")]
    [SerializeField] private float[] laneX = { -4.5f, 0.5f, 4.5f };
    [SerializeField, Min(0.05f)] private float laneChangeDuration = 0.22f;
    [SerializeField, Min(0.05f)] private float bumpReturnDuration = 0.18f;

    [Header("Jump")]
    // Starting point: height 2.5-3.4 m, apex 0.32-0.42 s, fall multiplier 1.3-1.8.
    // Height controls clearance; apex time controls floatiness. Gravity = 2h / t².
    // Current bus roofs are about 3 m high. Use the existing ramps or raise jumpHeight.
    [SerializeField, Min(0.1f)] private float jumpHeight = 2.8f;
    [SerializeField, Min(0.1f)] private float timeToJumpApex = 0.38f;
    [SerializeField, Min(1)] private float fallMultiplier = 1.5f;
    [SerializeField, Min(1)] private float terminalFallSpeed = 40f;
    [SerializeField, Range(0, 0.2f)] private float coyoteTime = 0.08f;
    [SerializeField, Range(0, 0.25f)] private float jumpBufferTime = 0.12f;

    [Header("Slide and airborne dive")]
    [SerializeField, Min(0.1f)] private float slideDuration = 0.75f;
    [SerializeField, Min(0.1f)] private float slideColliderHeight = 0.85f;
    [SerializeField, Min(1)] private float diveSpeed = 22f;
    [SerializeField, Min(1)] private float diveGravityMultiplier = 2f;
    [Tooltip("An airborne slide receives at least this much slide time after landing.")]
    [SerializeField, Min(0)] private float minimumSlideAfterLanding = 0.45f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField, Range(0.01f, 0.3f)] private float groundProbeDistance = 0.12f;
    [SerializeField, Min(0.1f)] private float groundStickSpeed = 6f;
    [Tooltip("Maximum distance per movement substep. Smaller values cost more CPU.")]
    [SerializeField, Range(0.05f, 0.5f)] private float maximumMoveStep = 0.15f;
    [SerializeField] private UnityEvent onRunStopped = new UnityEvent();

    [Header("Animation clips (controller and source assets stay unchanged)")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string runState = "Base Layer.Running";
    [SerializeField] private string jumpState = "Base Layer.Jump";
    [SerializeField] private string slideState = "Base Layer.Running Slide";
    [Tooltip("Optional controller state fallback when no standalone fall clip is assigned.")]
    [SerializeField] private string fallState = "";
    [SerializeField] private AnimationClip fallAnimationClip;
    [SerializeField, HideInInspector] private float animationBlendTime = 0.08f;
    [SerializeField] private AnimationClip runAnimationClip;
    [Header("Animation transitions (seconds)")]
    [SerializeField, Range(0.02f, 0.3f)] private float jumpBlendTime = 0.1f;
    [SerializeField, Range(0.02f, 0.4f)] private float fallBlendTime = 0.18f;
    [SerializeField, Range(0.02f, 0.4f)] private float landingBlendTime = 0.16f;
    [SerializeField, Range(0.02f, 0.3f)] private float slideInBlendTime = 0.1f;
    [SerializeField, Range(0.02f, 0.4f)] private float slideOutBlendTime = 0.18f;
    [SerializeField, Range(0.02f, 0.3f)] private float diveBlendTime = 0.12f;
    [Tooltip("Ignore very short support gaps before showing the fall pose.")]
    [SerializeField, Range(0f, 0.15f)] private float fallAnimationDelay = 0.06f;
    [Tooltip("Existing clip reference used only to fit its playback speed to the physical jump.")]
    [SerializeField] private AnimationClip jumpAnimationClip;
    [Tooltip("Existing clip reference used only to fit its playback speed to slideDuration.")]
    [SerializeField] private AnimationClip slideAnimationClip;

    // حالة الحركة تخص هذا اللاعب وحده؛ لا توجد أعلام static تتسرب بين الجولات أو اللاعبين.

    public MovementState State { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsSliding => sliding;
    public bool IsRunning => running && !paused;
    public bool IsPaused => paused;
    public float CurrentSpeed { get; private set; }
    public float VerticalSpeed => verticalSpeed;
    public float SupportHeight { get; private set; }
    public int CurrentLane => currentLane;
    public int TargetLane => targetLane;
    public bool IsChangingLane => changingLane;
    public int SideBumpCount { get; private set; }
    public int LastSideBumpDirection { get; private set; }
    public float FixedZ => fixedZ;
    public float FallAnimationWeight => animationBlender != null ? animationBlender.FallWeight : 0f;
    public string StopReason { get; private set; }
    public event Action<string> RunStopped;
    public event Action SideBumped;
    public event Action<Collider> FrontalCollision;
    public event Action<MovementState> StateChanged;

    private CharacterController controller;
    private Swipe input;
    private readonly RaycastHit[] castHits = new RaycastHit[32];
    private readonly Collider[] overlaps = new Collider[32];
    private float standingHeight, feetOffset, verticalSpeed, simulationTime;
    private Vector3 standingCenter;
    private float jumpRequestUntil = -1f, lastGroundedTime = -10f, slideRemaining;
    private float laneSpeed;
    private int currentLane, targetLane, queuedLaneDirection;
    private bool running, sliding, changingLane, returningFromBump, jumpConsumed;
    private int moveAxis;
    private Vector3 moveDirection;
    private int lastAnimationHash;
    private bool initialized, paused;
    private float fixedZ, worldQueryOffset, worldDistancePerStep;
    private RunnerAnimationBlender animationBlender;
    private bool jumpAnimationActive;
    private float jumpAnimationEndTime;
    private RunnerAnimationBlender.Motion lastAnimationMotion;
    private float JumpFlightTime => timeToJumpApex * (1f + 1f / Mathf.Sqrt(fallMultiplier));
    private float WalkableNormalY => Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);

    private void Awake() => Initialize();

    /// <summary>Idempotent setup, also usable by deterministic validation tools.</summary>
    public void Initialize()
    {
        if (initialized) return;
        controller = GetComponent<CharacterController>();
        // Legacy scenes can retain their components without two physics owners.
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule)
        {
            controller.height = capsule.height;
            controller.radius = capsule.radius;
            controller.center = capsule.center;
            controller.skinWidth = Mathf.Min(0.02f, capsule.radius * 0.1f);
            capsule.enabled = false;
        }
        var body = GetComponent<Rigidbody>();
        if (body)
        {
            body.useGravity = false;
            body.isKinematic = true;
            body.detectCollisions = false;
        }
        if (!playerAnimator) playerAnimator = GetComponentInChildren<Animator>();
        if (playerAnimator) playerAnimator.applyRootMotion = false;
        standingHeight = controller.height;
        standingCenter = controller.center;
        feetOffset = standingCenter.y - standingHeight * 0.5f;
        if (playerAnimator && playerAnimator.runtimeAnimatorController)
        {
            if (!runAnimationClip) runAnimationClip = FindControllerClip(runState);
            if (!jumpAnimationClip) jumpAnimationClip = FindControllerClip(jumpState);
            if (!slideAnimationClip) slideAnimationClip = FindControllerClip(slideState);
            if (runAnimationClip && jumpAnimationClip && fallAnimationClip && slideAnimationClip)
                animationBlender = new RunnerAnimationBlender(playerAnimator, runAnimationClip,
                    jumpAnimationClip, fallAnimationClip, slideAnimationClip);
        }
        initialized = true;
        ResetRun(startAutomatically);
    }

    private void OnEnable() { BindInput(); animationBlender?.Resume(); }
    private void OnDisable() { UnbindInput(); animationBlender?.Pause(); }
    private void OnDestroy() { animationBlender?.Dispose(); }
    private void Update()
    {
        BindInput();
        if (!paused) animationBlender?.Tick(Time.deltaTime);
    }

    // مصدر العالم يتحرك أولًا، ثم يقرأ اللاعب مسافة الخطوة لمعالجة الاصطدام النسبي.
    private void FixedUpdate()
    {
        float distance = readWorldSpeed && worldSpeedSource ? worldSpeedSource.StepDistance : 0f;
        SimulateMovingWorld(Time.fixedDeltaTime, distance);
    }

    /// <summary>حقن مراجع الجولة بدل البحث عن مدير ماب أو إدخال عام داخل الحركة.</summary>
    public void ConfigureSession(Swipe source, RunnerWorldSource world)
    {
        startAutomatically = false;
        inputSource = source;
        worldSpeedSource = world;
        BindInput();
    }

    /// <summary>إعادة إنشاء موارد الأنيميشن عند دخول Play حتى مع تعطيل Domain Reload.</summary>
    public void ReinitializeForSession()
    {
        UnbindInput();
        animationBlender?.Dispose();
        animationBlender = null;
        initialized = false;
        paused = false;
        Initialize();
        BindInput();
    }

    /// <summary>الإيقاف المؤقت يحفظ مسار القفزة والزحلقة، لكنه يلغي الأوامر المخزنة.</summary>
    public void SetPaused(bool value)
    {
        if (paused == value) return;
        paused = value;
        jumpRequestUntil = -1f;
        queuedLaneDirection = 0;
        if (paused) animationBlender?.Pause();
        else animationBlender?.Resume();
    }

    /// <summary>إعادة موضع اللاعب مع تعطيل الـController لحظة النقل لتجنب دفعه بواسطة عائق قديم.</summary>
    public void Respawn(Vector3 position, Quaternion rotation, bool beginRunning)
    {
        Initialize();
        bool controllerWasEnabled = controller.enabled;
        controller.enabled = false;
        RestoreStandingShape();
        transform.SetPositionAndRotation(position, rotation);
        controller.enabled = controllerWasEnabled;
        Physics.SyncTransforms();
        ResetRun(beginRunning);
    }

    private void BindInput()
    {
        var desiredInput = inputSource ? inputSource : Swipe.Instance;
        if (input == desiredInput) return;
        UnbindInput();
        input = desiredInput;
        if (!input) return;
        input.OnSwipeUp += HandleUp;
        input.OnSwipeDown += HandleDown;
        input.OnSwipeLeft += HandleLeft;
        input.OnSwipeRight += HandleRight;
    }

    private void UnbindInput()
    {
        if (!input) { input = null; return; }
        input.OnSwipeUp -= HandleUp;
        input.OnSwipeDown -= HandleDown;
        input.OnSwipeLeft -= HandleLeft;
        input.OnSwipeRight -= HandleRight;
        input = null;
    }

    private void HandleUp(object sender, EventArgs e) => RequestJump();
    private void HandleDown(object sender, EventArgs e) => RequestSlide();
    private void HandleLeft(object sender, EventArgs e) => RequestLaneChange(-1);
    private void HandleRight(object sender, EventArgs e) => RequestLaneChange(1);

    /// <summary>Reset at the current position; a future restart system owns respawning.</summary>
    public void ResetRun(bool beginRunning = true)
    {
        if (!initialized) return;
        paused = false;
        animationBlender?.Resume();
        RestoreStandingShape();
        currentLane = NearestLane(transform.position.x);
        targetLane = currentLane;
        changingLane = Mathf.Abs(transform.position.x - laneX[currentLane]) > 0.01f;
        laneSpeed = Mathf.Abs(transform.position.x - laneX[currentLane]) / laneChangeDuration;
        returningFromBump = false;
        queuedLaneDirection = 0;
        sliding = false;
        verticalSpeed = simulationTime = slideRemaining = 0;
        jumpRequestUntil = -1;
        lastGroundedTime = -10;
        jumpConsumed = false;
        CurrentSpeed = 0f;
        fixedZ = transform.position.z;
        worldQueryOffset = 0f;
        running = beginRunning;
        StopReason = "";
        SideBumpCount = 0;
        LastSideBumpDirection = 0;
        IsGrounded = ProbeGround(groundProbeDistance, out var ground);
        SupportHeight = IsGrounded ? ground.point.y : transform.position.y + feetOffset;
        lastAnimationHash = 0;
        lastAnimationMotion = RunnerAnimationBlender.Motion.Run;
        jumpAnimationActive = false;
        animationBlender?.Reset();
        SetState(beginRunning ? MovementState.Running : MovementState.Stopped, true);
    }

    public void RequestJump()
    {
        if (IsRunning) jumpRequestUntil = simulationTime + jumpBufferTime;
    }

    public void RequestSlide()
    {
        if (!IsRunning || sliding) return;
        jumpRequestUntil = -1;
        sliding = true;
        slideRemaining = slideDuration;
        SetHeight(Mathf.Clamp(slideColliderHeight, controller.radius * 2f, standingHeight));
        if (!IsGrounded)
        {
            verticalSpeed = -Mathf.Min(diveSpeed, terminalFallSpeed);
            jumpConsumed = true;
        }
        UpdateState();
    }

    public void RequestLaneChange(int direction)
    {
        if (!IsRunning || direction == 0 || returningFromBump) return;
        direction = Math.Sign(direction);
        if (changingLane) { queuedLaneDirection = direction; return; }
        targetLane = Mathf.Clamp(currentLane + direction, 0, laneX.Length - 1);
        if (targetLane == currentLane) return;
        changingLane = true;
        laneSpeed = Mathf.Abs(laneX[targetLane] - transform.position.x) / laneChangeDuration;
    }

    public void StopRun(string reason = "Stopped")
    {
        if (!running) return;
        paused = false;
        animationBlender?.Resume();
        running = false;
        CurrentSpeed = verticalSpeed = 0;
        queuedLaneDirection = 0;
        jumpRequestUntil = -1;
        StopReason = reason;
        SetState(MovementState.Stopped);
        RunStopped?.Invoke(reason);
        onRunStopped.Invoke();
    }

    /// <summary>Simulate against a stationary world. For a moving map use SimulateMovingWorld.</summary>
    public void Simulate(float deltaTime) => SimulateMovingWorld(deltaTime, 0f);

    /// <summary>
    /// Call AFTER the world moved, with its actual backward distance for this step.
    /// Disable this component when another system calls this API manually.
    /// Only reads world colliders. No world transforms or ChunkManager values are changed.
    /// </summary>
    public void SimulateMovingWorld(float deltaTime, float backwardDistance)
    {
        if (!initialized || deltaTime <= 0f || !controller.enabled) return;
        LockForwardPosition();
        if (!IsRunning) return;
        Physics.SyncTransforms();
        float dt = Mathf.Min(deltaTime, 0.1f);
        backwardDistance = Mathf.Max(0f, backwardDistance);
        CurrentSpeed = backwardDistance / deltaTime;
        float gravityBound = 2f * jumpHeight / (timeToJumpApex * timeToJumpApex) *
            Mathf.Max(fallMultiplier, diveGravityMultiplier);
        float verticalBound = Mathf.Min(terminalFallSpeed, Mathf.Abs(verticalSpeed) + gravityBound * dt);
        float distanceBound = Mathf.Max(backwardDistance,
            (changingLane ? laneSpeed : 0f) * dt, verticalBound * dt);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distanceBound / maximumMoveStep));
        worldDistancePerStep = backwardDistance / steps;
        // In the CURRENT world snapshot, the previous relative player position is
        // behind the anchor. Sweep queries traverse that distance, not the player.
        worldQueryOffset = -backwardDistance;
        for (int i = 0; i < steps && IsRunning; i++) StepMovement(dt / steps);
        worldQueryOffset = 0f;
        LockForwardPosition();
        if (transform.position.y < fallOutHeight) StopRun("Fell out of the level");
        UpdateState();
        // Re-evaluate a prolonged descent even when its physical state stays Falling.
        PlayStateAnimation();
        if (!isActiveAndEnabled) animationBlender?.Tick(dt);
    }

    private void LockForwardPosition()
    {
        if (Mathf.Abs(transform.position.z - fixedZ) < 0.000001f) return;
        Vector3 position = transform.position;
        position.z = fixedZ;
        transform.position = position;
    }

    private void StepMovement(float dt)
    {
        simulationTime += dt;
        bool wasGrounded = IsGrounded;
        IsGrounded = verticalSpeed <= 0 && ProbeGround(groundProbeDistance, out _);
        if (IsGrounded)
        {
            lastGroundedTime = simulationTime;
            jumpConsumed = false;
        }

        if (jumpRequestUntil >= simulationTime && !jumpConsumed &&
            (IsGrounded || simulationTime - lastGroundedTime <= coyoteTime) &&
            (!sliding || CanStand()))
        {
            sliding = false;
            RestoreStandingShape();
            verticalSpeed = 2f * jumpHeight / timeToJumpApex;
            jumpAnimationActive = true;
            jumpAnimationEndTime = simulationTime + JumpFlightTime;
            jumpRequestUntil = -1;
            jumpConsumed = true;
            IsGrounded = false;
        }

        float gravity = 2f * jumpHeight / (timeToJumpApex * timeToJumpApex);
        float multiplier = sliding && !IsGrounded ? diveGravityMultiplier :
            verticalSpeed < 0 ? fallMultiplier : 1f;
        float oldVerticalSpeed = verticalSpeed;
        verticalSpeed = IsGrounded ? -groundStickSpeed :
            Mathf.Max(verticalSpeed - gravity * multiplier * dt, -terminalFallSpeed);
        float verticalTravel = IsGrounded ? -groundStickSpeed * dt :
            (oldVerticalSpeed + verticalSpeed) * 0.5f * dt;

        // Vertical/lateral movement is local. Forward travel is a relative sweep only.
        Move(Vector3.up * verticalTravel, 3);
        StepLane(dt);
        if (!IsRunning) return;
        Vector3 incomingTravel = Vector3.forward * worldDistancePerStep;
        if (Sweep(incomingTravel, out var front) && front.normal.z < -0.5f)
        {
            HitFront(front.collider);
            return;
        }
        Move(incomingTravel, 2);
        worldQueryOffset += worldDistancePerStep;
        if (!IsRunning) return;

        // Follow descending support without snapping an ascending jump down.
        float slopeTravel = worldDistancePerStep * Mathf.Tan(controller.slopeLimit * Mathf.Deg2Rad);
        float snapDistance = groundProbeDistance + (wasGrounded ? slopeTravel : 0f);
        IsGrounded = verticalSpeed <= 0 && ProbeGround(snapDistance, out var support);
        if (IsGrounded)
        {
            if (ProbeGround(snapDistance, out support))
            {
                float gap = transform.position.y + feetOffset - support.point.y;
                if (gap > controller.skinWidth) Move(Vector3.down * gap, 3);
                SupportHeight = support.point.y;
            }
            verticalSpeed = -groundStickSpeed;
            lastGroundedTime = simulationTime;
            jumpConsumed = false;
            if (!wasGrounded && sliding)
                slideRemaining = Mathf.Max(slideRemaining, minimumSlideAfterLanding);
        }

        if (sliding && IsGrounded)
        {
            // Air time never consumes the grounded slide window.
            slideRemaining -= dt;
            if (slideRemaining <= 0 && CanStand())
            {
                sliding = false;
                RestoreStandingShape();
            }
        }
    }

    private void StepLane(float dt)
    {
        if (!changingLane) return;
        float goal = laneX[targetLane];
        float dx = Mathf.MoveTowards(transform.position.x, goal, laneSpeed * dt) - transform.position.x;
        Vector3 lateral = Vector3.right * dx;
        if (Sweep(lateral, out var side))
        {
            float safe = Mathf.Max(0, side.distance - controller.skinWidth);
            Move(Vector3.right * Mathf.Sign(dx) * Mathf.Min(Mathf.Abs(dx), safe), 1);
            BeginSideBump();
        }
        else Move(lateral, 1);

        if (Mathf.Abs(transform.position.x - laneX[targetLane]) > 0.015f) return;
        currentLane = targetLane;
        changingLane = returningFromBump = false;
        int queued = queuedLaneDirection;
        queuedLaneDirection = 0;
        if (queued != 0) RequestLaneChange(queued);
    }

    private void BeginSideBump()
    {
        if (!changingLane || returningFromBump) return;
        LastSideBumpDirection = Math.Sign(laneX[targetLane] - laneX[currentLane]);
        returningFromBump = true;
        targetLane = currentLane;
        queuedLaneDirection = 0;
        laneSpeed = Mathf.Max(1, Mathf.Abs(transform.position.x - laneX[currentLane]) / bumpReturnDuration);
        SideBumpCount++;
        OnSideBumpPursuerApproachRequested();
        SideBumped?.Invoke();
    }

    private void OnSideBumpPursuerApproachRequested()
    {
        // TODO: Bring the pursuer closer here when that system is implemented.
    }

    private void HitFront(Collider obstacle)
    {
        if (!IsRunning) return;
        StopRun("Frontal collision: " + (obstacle ? obstacle.name : "obstacle"));
        FrontalCollision?.Invoke(obstacle);
    }

    private CollisionFlags Move(Vector3 displacement, int axis)
    {
        moveAxis = axis;
        moveDirection = displacement;
        // Resolve each move at the same relative-world position as the sweeps.
        // The offset exists only inside this collision calculation; every public
        // simulation step and rendered frame ends at the captured Z anchor.
        Vector3 queryPosition = transform.position;
        queryPosition.z = fixedZ + worldQueryOffset;
        transform.position = queryPosition;
        Physics.SyncTransforms();
        var flags = controller.Move(displacement);
        LockForwardPosition();
        if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
        moveAxis = 0;
        return flags;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y >= WalkableNormalY) return;
        if (moveAxis == 1 && Mathf.Abs(hit.normal.x) > 0.5f &&
            Vector3.Dot(moveDirection, hit.normal) < 0) BeginSideBump();
        else if (moveAxis == 2 && hit.normal.z < -0.5f) HitFront(hit.collider);
    }

    private bool Sweep(Vector3 displacement, out RaycastHit nearest)
    {
        nearest = default;
        float length = displacement.magnitude;
        if (length < 0.000001f) return false;
        Capsule(transform.position + Vector3.forward * worldQueryOffset,
            controller.height, controller.center, out var a, out var b);
        int count = Physics.CapsuleCastNonAlloc(a, b,
            Mathf.Max(0.01f, controller.radius - controller.skinWidth),
            displacement / length, castHits, length + controller.skinWidth,
            collisionMask, QueryTriggerInteraction.Ignore);
        float distance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = castHits[i];
            if (IsSelf(hit.collider) || hit.normal.y >= WalkableNormalY ||
                Vector3.Dot(displacement, hit.normal) >= -0.000001f) continue;
            if (hit.distance < distance) { nearest = hit; distance = hit.distance; }
        }
        return distance < float.PositiveInfinity;
    }

    private bool ProbeGround(float distance, out RaycastHit nearest, float lift = 0f)
    {
        nearest = default;
        float radius = Mathf.Max(0.01f, controller.radius - controller.skinWidth);
        Vector3 origin = transform.position + new Vector3(controller.center.x,
            feetOffset + radius + 0.06f + lift, controller.center.z + worldQueryOffset);
        int count = Physics.CapsuleCastNonAlloc(origin, origin, radius, Vector3.down,
            castHits, distance + 0.06f + lift, collisionMask, QueryTriggerInteraction.Ignore);
        float best = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = castHits[i];
            if (IsSelf(hit.collider) || hit.normal.y < WalkableNormalY) continue;
            if (hit.distance < best) { nearest = hit; best = hit.distance; }
        }
        return best < float.PositiveInfinity;
    }

    private bool CanStand()
    {
        Capsule(transform.position + Vector3.forward * worldQueryOffset,
            standingHeight, standingCenter, out var a, out var b);
        // Skin margin excludes the floor contact while still testing the added headroom.
        int count = Physics.OverlapCapsuleNonAlloc(a, b,
            Mathf.Max(0.01f, controller.radius - controller.skinWidth * 2f),
            overlaps, collisionMask, QueryTriggerInteraction.Ignore);
        if (count == overlaps.Length) return false; // Fail closed if the query is saturated.
        for (int i = 0; i < count; i++) if (!IsSelf(overlaps[i])) return false;
        return true;
    }

    private bool IsSelf(Collider other) => !other || other.transform == transform ||
        other.transform.IsChildOf(transform);

    private void Capsule(Vector3 position, float height, Vector3 center, out Vector3 a, out Vector3 b)
    {
        Vector3 worldCenter = position + center;
        float halfSegment = Mathf.Max(0, height * 0.5f - controller.radius);
        a = worldCenter + Vector3.up * halfSegment;
        b = worldCenter - Vector3.up * halfSegment;
    }

    private void SetHeight(float height)
    {
        controller.height = height;
        controller.center = new Vector3(standingCenter.x, feetOffset + height * 0.5f, standingCenter.z);
        controller.stepOffset = Mathf.Min(0.2f, height * 0.2f);
    }

    private void RestoreStandingShape()
    {
        controller.height = standingHeight;
        controller.center = standingCenter;
        controller.stepOffset = Mathf.Min(0.2f, standingHeight * 0.2f);
    }

    private int NearestLane(float x)
    {
        int nearest = 0;
        for (int i = 1; i < laneX.Length; i++)
            if (Mathf.Abs(x - laneX[i]) < Mathf.Abs(x - laneX[nearest])) nearest = i;
        return nearest;
    }

    private void UpdateState()
    {
        SetState(!IsRunning ? MovementState.Stopped :
            sliding ? (IsGrounded ? MovementState.Sliding : MovementState.Diving) :
            IsGrounded ? MovementState.Running :
            verticalSpeed > 0 ? MovementState.Jumping : MovementState.Falling);
    }

    private void SetState(MovementState next, bool force = false)
    {
        if (!force && State == next) return;
        State = next;
        PlayStateAnimation();
        StateChanged?.Invoke(next);
    }

    private void PlayStateAnimation()
    {
        if (!playerAnimator) return;
        if (State == MovementState.Stopped)
        {
            if (animationBlender != null) animationBlender.SetStopped(true);
            else playerAnimator.speed = 0f;
            return;
        }

        if (IsGrounded || sliding) jumpAnimationActive = false;
        // A flip is authored as one complete action. Do not replace it with the
        // upright fall pose at its midpoint just because vertical velocity changes sign.
        // A longer drop beyond that action, or walking off a roof, still uses Fall.
        bool completingJump = State == MovementState.Falling && jumpAnimationActive &&
            simulationTime <= jumpAnimationEndTime;
        bool briefSupportGap = State == MovementState.Falling && !jumpAnimationActive &&
            simulationTime - lastGroundedTime < fallAnimationDelay;
        var motion = State == MovementState.Jumping || completingJump
            ? RunnerAnimationBlender.Motion.Jump
            : sliding ? RunnerAnimationBlender.Motion.Slide
            : State == MovementState.Falling && !briefSupportGap
                ? RunnerAnimationBlender.Motion.Fall : RunnerAnimationBlender.Motion.Run;
        float duration = motion == RunnerAnimationBlender.Motion.Jump ? jumpBlendTime :
            motion == RunnerAnimationBlender.Motion.Fall ? fallBlendTime :
            motion == RunnerAnimationBlender.Motion.Slide
                ? (State == MovementState.Diving ? diveBlendTime : slideInBlendTime)
                : lastAnimationMotion == RunnerAnimationBlender.Motion.Slide
                    ? slideOutBlendTime : landingBlendTime;
        float speed = motion == RunnerAnimationBlender.Motion.Jump && jumpAnimationClip
            ? jumpAnimationClip.length / Mathf.Max(0.1f, JumpFlightTime)
            : motion == RunnerAnimationBlender.Motion.Slide && slideAnimationClip
                ? slideAnimationClip.length / Mathf.Max(0.1f, slideDuration) : 1f;
        playerAnimator.speed = 1f;
        if (animationBlender != null)
        {
            animationBlender.SetStopped(false);
            animationBlender.Play(motion, duration, speed);
        }
        else if (playerAnimator.runtimeAnimatorController)
        {
            // Compatibility for scenes that have not assigned all standalone clips.
            string path = motion == RunnerAnimationBlender.Motion.Run ? runState :
                motion == RunnerAnimationBlender.Motion.Jump ? jumpState :
                motion == RunnerAnimationBlender.Motion.Slide ? slideState : fallState;
            if (string.IsNullOrWhiteSpace(path)) return;
            int hash = Animator.StringToHash(path);
            playerAnimator.speed = speed;
            if (hash != lastAnimationHash && playerAnimator.HasState(0, hash))
            {
                playerAnimator.CrossFadeInFixedTime(hash,
                    duration > 0 ? duration : animationBlendTime, 0);
                lastAnimationHash = hash;
            }
        }
        lastAnimationMotion = motion;
    }

    private AnimationClip FindControllerClip(string statePath)
    {
        if (string.IsNullOrWhiteSpace(statePath)) return null;
        string clipName = statePath.Substring(statePath.LastIndexOf('.') + 1);
        foreach (var clip in playerAnimator.runtimeAnimatorController.animationClips)
            if (clip && string.Equals(clip.name, clipName, StringComparison.OrdinalIgnoreCase))
                return clip;
        return null;
    }

#if UNITY_EDITOR
    /// <summary>Evaluate the same animation graph used in play on a temporary preview rig.</summary>
    public void PreviewAnimation(float deltaTime) => animationBlender?.EvaluatePreview(paused ? 0f : deltaTime);
#endif

    // Existing clip event: animation cannot override slide clearance or airborne physics.
    public void GoBackToRunning()
    {
        if (IsGrounded && !sliding && IsRunning) PlayStateAnimation();
    }

    private void OnValidate()
    {
        if (laneX == null || laneX.Length == 0) laneX = new[] { -4.5f, 0.5f, 4.5f };
        Array.Sort(laneX);
        maximumMoveStep = Mathf.Max(0.05f, maximumMoveStep);
        laneChangeDuration = Mathf.Max(0.05f, laneChangeDuration);
        bumpReturnDuration = Mathf.Max(0.05f, bumpReturnDuration);
        timeToJumpApex = Mathf.Max(0.1f, timeToJumpApex);
        jumpHeight = Mathf.Max(0.1f, jumpHeight);
        terminalFallSpeed = Mathf.Max(1, terminalFallSpeed);
    }
}

