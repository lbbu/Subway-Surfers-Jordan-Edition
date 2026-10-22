using System;
using UnityEngine;

/// <summary>روحان: ضربة جانبية تفتح مهلة التعافي، وضربة ثانية قبل انتهائها أو ضربة أمامية تنهي الجولة.</summary>
[DisallowMultipleComponent]
public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField] private PlayerMovements player;
    [SerializeField] private RunSession session;
    [SerializeField, Min(0.1f)] private float recoveryDuration = 8f;
    public int Hearts { get; private set; } = 2;
    public bool IsDead => Hearts == 0;
    public float RecoveryRemaining { get; private set; }
    public float RecoveryDuration => recoveryDuration;
    public event Action<int> HealthChanged;
    public event Action<RunnerImpact> Damaged;
    public event Action Recovered;
    public event Action Died;
    public event Action PursuerApproachRequested;
    private int lastSequence;
    private PlayerMovements boundPlayer;
    private RunSession boundSession;

    public void Configure(PlayerMovements motor, RunSession run)
    { Unbind(); player = motor; session = run; Bind(); }
    private void OnEnable() => Bind();
    private void OnDisable() => Unbind();
    private void Bind()
    {
        if (boundPlayer == player && boundSession == session) return;
        Unbind(); boundPlayer = player; boundSession = session;
        if (boundPlayer) boundPlayer.ImpactDetected += ApplyImpact;
        if (boundSession) { boundSession.RunPrepared += ResetHealth; boundSession.RunEnded += HandleExternalEnd; }
    }
    private void Unbind()
    {
        if (boundPlayer) boundPlayer.ImpactDetected -= ApplyImpact;
        if (boundSession) { boundSession.RunPrepared -= ResetHealth; boundSession.RunEnded -= HandleExternalEnd; }
        boundPlayer = null; boundSession = null;
    }
    public void ResetHealth()
    { Hearts = 2; RecoveryRemaining = 0; lastSequence = 0; HealthChanged?.Invoke(Hearts); }
    private void Update() => Tick(Time.deltaTime);
    public void Tick(float deltaTime)
    {
        // زمن التعافي تابع للجولة؛ الإيقاف المؤقت لا يمنح اللاعب تعافيًا مجانيًا.
        if (!session || session.State != RunSession.RunState.Playing || Hearts != 1 || deltaTime <= 0) return;
        RecoveryRemaining = Mathf.Max(0, RecoveryRemaining - deltaTime);
        if (RecoveryRemaining > 0) return;
        Hearts = 2; HealthChanged?.Invoke(Hearts); Recovered?.Invoke();
    }
    public void ApplyImpact(RunnerImpact impact)
    {
        if (!session || session.State != RunSession.RunState.Playing || IsDead || impact.Sequence <= lastSequence) return;
        lastSequence = impact.Sequence;
        Hearts = impact.Type == RunnerImpact.Kind.Side ? Hearts - 1 : 0;
        RecoveryRemaining = Hearts == 1 ? recoveryDuration : 0;
        HealthChanged?.Invoke(Hearts);
        Damaged?.Invoke(impact);
        if (Hearts == 1) PursuerApproachRequested?.Invoke(); // مكان ربط المطارد لاحقًا؛ لا ننفذ حركة مطارد هنا.
        if (!IsDead) return;
        Died?.Invoke();
        session.EndRun(impact.Type == RunnerImpact.Kind.Side ? "Second side collision" :
            impact.Type == RunnerImpact.Kind.Front ? "Frontal collision" : "Fell out of the level");
    }
    private void HandleExternalEnd(string reason)
    {
        RecoveryRemaining = 0;
        if (IsDead) return;
        Hearts = 0; HealthChanged?.Invoke(Hearts); Died?.Invoke();
    }
}
