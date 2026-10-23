using UnityEngine;

/// <summary>تجاوب بصري وصوتي منفصل عن الصحة والفيزياء؛ يهز المجسم دون تحريك كبسولة اللاعب.</summary>
[DefaultExecutionOrder(200)]
public sealed class PlayerFeedback : MonoBehaviour
{
    [SerializeField] private PlayerMovements player;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private RunSession session;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private RunnerCameraFollow runnerCamera;
    [SerializeField] private RunnerEffectPool effects;
    [SerializeField, Range(0, 1)] private float cameraShakeScale = 1f;
    [SerializeField] private bool enableHaptics = false;
    [SerializeField, Min(0)] private float bodyShakeDistance = .14f;
    [SerializeField, Min(.05f)] private float impactDuration = .28f;
    [SerializeField, Range(0, 15)] private float impactTilt = 7f;
    [SerializeField, Range(0, 10)] private float laneLean = 3f;
    [SerializeField, Min(.02f)] private float slideEmissionInterval = .09f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landingSound;
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private bool captured, bound;
    private float impactAge = 100, direction = 1, strength = 1, slideTimer, lean;
    public bool IsImpactActive => impactAge < impactDuration;
    public void Initialize()
    {
        if (!captured && visualRoot) { basePosition = visualRoot.localPosition; baseRotation = visualRoot.localRotation; captured = true; }
        Bind(); if (effects) effects.Initialize(); ResetFeedback();
    }
    private void OnEnable() => Bind();
    private void OnDisable() { Unbind(); ResetFeedback(); }
    private void Bind()
    {
        if (bound || !player || !health || !session) return;
        health.Damaged += OnDamaged; player.Jumped += OnJumped; player.Landed += OnLanded;
        session.RunPrepared += ResetFeedback; session.StateChanged += OnSessionState;
        bound = true;
    }
    private void Unbind()
    {
        if (!bound) return;
        if (health) health.Damaged -= OnDamaged;
        if (player) { player.Jumped -= OnJumped; player.Landed -= OnLanded; }
        if (session) { session.RunPrepared -= ResetFeedback; session.StateChanged -= OnSessionState; }
        bound = false;
    }
    public void SetAccessibility(float shake, bool haptics) { cameraShakeScale = Mathf.Clamp01(shake); enableHaptics = haptics; }

    public void ResetFeedback()
    {
        impactAge = 100; slideTimer = lean = 0;
        if (captured && visualRoot) { visualRoot.localPosition = basePosition; visualRoot.localRotation = baseRotation; }
        if (effects) effects.Clear(); if (audioSource) audioSource.Stop();
    }
    private void OnSessionState(RunSession.RunState state)
    {
        bool paused = state == RunSession.RunState.Paused;
        if (effects) effects.SetPaused(paused);
        if (audioSource) { if (paused) audioSource.Pause(); else audioSource.UnPause(); }
    }
    private void OnDamaged(RunnerImpact impact)
    {
        impactAge = 0; direction = impact.Direction == 0 ? 1 : impact.Direction;
        strength = impact.Type == RunnerImpact.Kind.Side ? 1f : 1.4f;
        if (runnerCamera) runnerCamera.PlayImpact((int)direction, strength * cameraShakeScale);
        if (effects) effects.Emit(RunnerEffectPool.Effect.Impact, impact.Point, strength, player.CurrentSpeed);
        PlaySound(impactSound);
        // مفعّل باختيار المصمم فقط، والتجربة الفعلية للهاتف تتم على الجهاز.
#if UNITY_ANDROID || UNITY_IOS
        if (enableHaptics && !Application.isEditor) Handheld.Vibrate();
#endif
    }
    private void OnJumped()
    {
        if (effects) effects.Emit(RunnerEffectPool.Effect.Dust, player.transform.position, .65f, player.CurrentSpeed);
        PlaySound(jumpSound);
    }
    private void OnLanded(float speed)
    {
        if (effects) effects.Emit(RunnerEffectPool.Effect.Dust, player.transform.position, Mathf.Clamp(speed / 15f, .5f, 1.5f), player.CurrentSpeed);
        PlaySound(landingSound);
    }
    private void PlaySound(AudioClip clip) { if (audioSource && clip) audioSource.PlayOneShot(clip); }
    private void LateUpdate() => Tick(Time.deltaTime);
    public void Tick(float deltaTime)
    {
        if (!captured || !visualRoot || !session || session.State == RunSession.RunState.Paused || deltaTime <= 0) return;
        float envelope = 0, wave = 0;
        if (IsImpactActive)
        {
            impactAge = Mathf.Min(impactDuration, impactAge + deltaTime);
            envelope = 1 - impactAge / impactDuration;
            wave = Mathf.Sin(impactAge * 55f) * envelope * envelope * direction * strength;
        }
        float desiredLean = player.IsRunning && player.IsChangingLane ? -Mathf.Sign(player.TargetLane - player.CurrentLane) * laneLean : 0;
        lean = Mathf.Lerp(lean, desiredLean, 1 - Mathf.Exp(-deltaTime * 14));
        visualRoot.localPosition = basePosition + Vector3.right * (wave * bodyShakeDistance);
        visualRoot.localRotation = baseRotation * Quaternion.Euler(envelope * strength * 4, 0, lean - wave * impactTilt);
        if (player.IsRunning && player.IsGrounded && player.IsSliding)
        {
            slideTimer -= deltaTime;
            if (slideTimer <= 0) { slideTimer = slideEmissionInterval; if (effects) effects.Emit(RunnerEffectPool.Effect.Slide, player.transform.position, 1, player.CurrentSpeed); }
        }
        else slideTimer = 0;
    }
}
