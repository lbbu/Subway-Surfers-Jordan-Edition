using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>One owner for forward motion, lanes, gravity, jumping and sliding.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovements : MonoBehaviour
{
    public enum MovementState { Running, Jumping, Falling, Sliding, Diving, Stopped }

    [Header("Forward running (world +Z)")]
    [SerializeField, Min(0)] private float startSpeed = 10f;
    [SerializeField, Min(0)] private float acceleration = 0.35f;
    [SerializeField, Min(0)] private float maximumSpeed = 32f;
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

    [Header("Existing animations (no controller edits required)")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string runState = "Base Layer.Running";
    [SerializeField] private string jumpState = "Base Layer.Jump";
    [SerializeField] private string slideState = "Base Layer.Running Slide";
    [Tooltip("Leave empty now. Set a full Animator state path after adding a fall animation.")]
    [SerializeField] private string fallState = "";
    [SerializeField, Min(0)] private float animationBlendTime = 0.08f;
    [Tooltip("Existing clip reference used only to fit its playback speed to the physical jump.")]
    [SerializeField] private AnimationClip jumpAnimationClip;
    [Tooltip("Existing clip reference used only to fit its playback speed to slideDuration.")]
    [SerializeField] private AnimationClip slideAnimationClip;

    // Kept for compatibility with existing callers.
    public static bool isAllowToMove;
    public static bool isSliding;

    public MovementState State { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsSliding => sliding;
    public bool IsRunning => running && isAllowToMove;
    public float CurrentSpeed { get; private set; }
    public float VerticalSpeed => verticalSpeed;
    public float SupportHeight { get; private set; }
    public int CurrentLane => currentLane;
    public int TargetLane => targetLane;
    public bool IsChangingLane => changingLane;
    public int SideBumpCount { get; private set; }
    public string StopReason { get; private set; }
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
    private bool initialized;
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
        initialized = true;
        ResetRun(startAutomatically);
    }

    private void OnEnable() { BindInput(); }
    private void OnDisable() { UnbindInput(); }
    private void Update()
    {
        BindInput();
        Simulate(Time.deltaTime);
    }

    private void BindInput()
    {
        if (input == Swipe.Instance) return;
        UnbindInput();
        input = Swipe.Instance;
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
        RestoreStandingShape();
        currentLane = NearestLane(transform.position.x);
        targetLane = currentLane;
        changingLane = Mathf.Abs(transform.position.x - laneX[currentLane]) > 0.01f;
        laneSpeed = Mathf.Abs(transform.position.x - laneX[currentLane]) / laneChangeDuration;
        returningFromBump = false;
        queuedLaneDirection = 0;
        sliding = isSliding = false;
        verticalSpeed = simulationTime = slideRemaining = 0;
        jumpRequestUntil = -1;
        lastGroundedTime = -10;
        jumpConsumed = false;
        CurrentSpeed = Mathf.Min(startSpeed, maximumSpeed);
        running = isAllowToMove = beginRunning;
        StopReason = "";
        SideBumpCount = 0;
        IsGrounded = ProbeGround(groundProbeDistance, out var ground);
        SupportHeight = IsGrounded ? ground.point.y : transform.position.y + feetOffset;
        lastAnimationHash = 0;
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
        sliding = isSliding = true;
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
        running = isAllowToMove = false;
        CurrentSpeed = verticalSpeed = 0;
        queuedLaneDirection = 0;
        jumpRequestUntil = -1;
        StopReason = reason;
        SetState(MovementState.Stopped);
        onRunStopped.Invoke();
    }

    /// <summary>Advance movement. Disable this component when driving it manually.</summary>
    public void Simulate(float deltaTime)
    {
        if (!initialized || !IsRunning || deltaTime <= 0 || !controller.enabled) return;
        // Bound catch-up after a long stall; collision travel remains subdivided.
        float dt = Mathf.Min(deltaTime, 0.1f);
        float gravityBound = 2f * jumpHeight / (timeToJumpApex * timeToJumpApex) *
            Mathf.Max(fallMultiplier, diveGravityMultiplier);
        float verticalBound = Mathf.Min(terminalFallSpeed, Mathf.Abs(verticalSpeed) + gravityBound * dt);
        float distanceBound = Mathf.Max(Mathf.Min(maximumSpeed, CurrentSpeed + acceleration * dt),
            changingLane ? laneSpeed : 0, verticalBound) * dt;
        int steps = Mathf.Max(1, Mathf.CeilToInt(distanceBound / maximumMoveStep));
        float stepTime = dt / steps;
        for (int i = 0; i < steps && IsRunning; i++) StepMovement(stepTime);
        if (transform.position.y < fallOutHeight) StopRun("Fell out of the level");
        UpdateState();
    }

    private void StepMovement(float dt)
    {
        simulationTime += dt;
        CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, maximumSpeed, acceleration * dt);
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
            sliding = isSliding = false;
            RestoreStandingShape();
            verticalSpeed = 2f * jumpHeight / timeToJumpApex;
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

        // Vertical first: jumping must clear an obstacle before forward contact.
        Move(Vector3.up * verticalTravel, 3);
        StepLane(dt);
        if (!IsRunning) return;
        Vector3 forward = Vector3.forward * (CurrentSpeed * dt);
        if (Sweep(forward, out var front) && front.normal.z < -0.5f)
        {
            float safe = Mathf.Max(0, front.distance - controller.skinWidth);
            Move(Vector3.forward * Mathf.Min(forward.z, safe), 2);
            HitFront(front.collider);
            return;
        }
        Move(forward, 2);
        if (!IsRunning) return;

        // Keep contact down slopes without snapping a rising jump to the ground.
        float snapDistance = groundProbeDistance;
        if (wasGrounded) snapDistance += CurrentSpeed * dt *
            Mathf.Tan(controller.slopeLimit * Mathf.Deg2Rad);
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
                sliding = isSliding = false;
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
        var flags = controller.Move(displacement);
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
        Capsule(transform.position, controller.height, controller.center, out var a, out var b);
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

    private bool ProbeGround(float distance, out RaycastHit nearest)
    {
        nearest = default;
        float radius = Mathf.Max(0.01f, controller.radius - controller.skinWidth);
        Vector3 origin = transform.position + new Vector3(controller.center.x,
            feetOffset + radius + 0.06f, controller.center.z);
        int count = Physics.CapsuleCastNonAlloc(origin, origin, radius, Vector3.down,
            castHits, distance + 0.06f, collisionMask, QueryTriggerInteraction.Ignore);
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
        Capsule(transform.position, standingHeight, standingCenter, out var a, out var b);
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
        if (!playerAnimator || !playerAnimator.runtimeAnimatorController) return;
        if (State == MovementState.Stopped) { playerAnimator.speed = 0; return; }
        playerAnimator.speed = 1f;
        if (jumpAnimationClip && (State == MovementState.Jumping ||
            (State == MovementState.Falling && string.IsNullOrWhiteSpace(fallState))))
        {
            float flightTime = timeToJumpApex * (1f + 1f / Mathf.Sqrt(fallMultiplier));
            playerAnimator.speed = jumpAnimationClip.length / Mathf.Max(0.1f, flightTime);
        }
        if (slideAnimationClip && (State == MovementState.Sliding || State == MovementState.Diving))
            playerAnimator.speed = slideAnimationClip.length / Mathf.Max(0.1f, slideDuration);
        string path = State == MovementState.Running ? runState :
            State == MovementState.Jumping ? jumpState :
            State == MovementState.Sliding || State == MovementState.Diving ? slideState :
            State == MovementState.Falling ? fallState : "";
        if (string.IsNullOrWhiteSpace(path)) return;
        int hash = Animator.StringToHash(path);
        if (hash == lastAnimationHash || !playerAnimator.HasState(0, hash)) return;
        playerAnimator.CrossFadeInFixedTime(hash, animationBlendTime, 0);
        lastAnimationHash = hash;
    }

    // Existing clip event: animation cannot override slide clearance or airborne physics.
    public void GoBackToRunning()
    {
        if (IsGrounded && !sliding && IsRunning) PlayStateAnimation();
    }

    private void OnValidate()
    {
        if (laneX == null || laneX.Length == 0) laneX = new[] { -4.5f, 0.5f, 4.5f };
        Array.Sort(laneX);
        maximumSpeed = Mathf.Max(0, maximumSpeed);
        startSpeed = Mathf.Clamp(startSpeed, 0, maximumSpeed);
        maximumMoveStep = Mathf.Max(0.05f, maximumMoveStep);
        laneChangeDuration = Mathf.Max(0.05f, laneChangeDuration);
        bumpReturnDuration = Mathf.Max(0.05f, bumpReturnDuration);
        timeToJumpApex = Mathf.Max(0.1f, timeToJumpApex);
        jumpHeight = Mathf.Max(0.1f, jumpHeight);
        terminalFallSpeed = Mathf.Max(1, terminalFallSpeed);
    }
}
