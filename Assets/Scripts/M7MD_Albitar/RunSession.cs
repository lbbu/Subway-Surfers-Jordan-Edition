using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>المالك الوحيد لدورة الجولة: تجهيز، بدء، إيقاف مؤقت، خسارة، ثم إعادة لعب.</summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public sealed class RunSession : MonoBehaviour
{
    public enum RunState { Ready, Playing, Paused, GameOver }

    [SerializeField] private PlayerMovements player;
    [SerializeField] private Swipe input;
    [SerializeField] private RunnerCameraFollow runnerCamera;
    [SerializeField] private RunnerWorldSource world;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private PlayerFeedback feedback;
    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private bool desktopSessionControls = true;

    public RunState State { get; private set; } = RunState.Ready;
    public string GameOverReason { get; private set; } = "";
    public bool IsInitialized => initialized;
    public event Action<RunState> StateChanged;
    public event Action RunStarted;
    public event Action RunPrepared;
    public event Action<string> RunEnded;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool initialized;
    private PlayerMovements subscribedPlayer;

    // عند تعطيل Domain Reload يبقى بعض حقول الذاكرة؛ نجهز الجولة صراحة عند كل دخول للعب.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializePlaySessions()
    {
        foreach (var session in FindObjectsByType<RunSession>(FindObjectsSortMode.None))
            session.Bootstrap();
    }

    private void Start()
    {
        if (!initialized) Bootstrap();
    }

    private void Bootstrap()
    {
        initialized = false;
        InitializeSession();
        if (startAutomatically) StartRun();
    }

    /// <summary>التقاط نقطة البداية وربط الأحداث؛ تستخدم أيضًا في اختبارات النسخ المؤقتة.</summary>
    public void InitializeSession()
    {
        if (initialized) return;
        Unsubscribe();
        initialized = false;
        if (!player || !input || !runnerCamera || !world || !health)
        {
            if (world) world.SetMoving(false);
            if (input) input.SetGameplayEnabled(false);
            Debug.LogError("RunSession: اربط اللاعب والصحة والإدخال والكاميرا ومصدر حركة الماب.", this);
            return;
        }
        spawnPosition = player.transform.position;
        spawnRotation = player.transform.rotation;
        world.CaptureInitialState();
        runnerCamera.SetTarget(player);
        player.ConfigureSession(input, world);
        player.ReinitializeForSession();
        health.Configure(player, this);
        if (feedback) feedback.Initialize();
        initialized = true;
        Subscribe();
        PrepareRun();
    }

    private void OnEnable()
    {
        if (initialized) Subscribe();
    }

    private void OnDisable()
    {
        if (initialized && State == RunState.Playing) PauseRun();
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribedPlayer == player) return;
        Unsubscribe();
        subscribedPlayer = player;
        if (subscribedPlayer) subscribedPlayer.RunStopped += HandlePlayerStopped;
    }

    private void Unsubscribe()
    {
        if (subscribedPlayer) subscribedPlayer.RunStopped -= HandlePlayerStopped;
        subscribedPlayer = null;
    }

    private void HandlePlayerStopped(string reason) => EndRun(reason);

    /// <summary>إرجاع الماب واللاعب والكاميرا إلى البداية، ومسح المدخلات قبل استقبال جولة جديدة.</summary>
    public void PrepareRun()
    {
        if (!initialized) return;
        world.SetMoving(false);
        input.SetGameplayEnabled(false);
        world.ResetWorld();
        player.Respawn(spawnPosition, spawnRotation, false);
        runnerCamera.SnapToTarget();
        runnerCamera.SetSessionPaused(true);
        GameOverReason = "";
        SetState(RunState.Ready);
        RunPrepared?.Invoke();
    }

    public void StartRun()
    {
        if (!initialized || State != RunState.Ready) return;
        player.ResetRun(true);
        runnerCamera.SetSessionPaused(false);
        world.SetMoving(true);
        input.SetGameplayEnabled(true);
        SetState(RunState.Playing);
        RunStarted?.Invoke();
    }

    public void PauseRun()
    {
        if (!initialized || State != RunState.Playing) return;
        input.SetGameplayEnabled(false);
        world.SetMoving(false);
        player.SetPaused(true);
        runnerCamera.SetSessionPaused(true);
        SetState(RunState.Paused);
    }

    public void ResumeRun()
    {
        if (!initialized || State != RunState.Paused) return;
        player.SetPaused(false);
        runnerCamera.SetSessionPaused(false);
        world.SetMoving(true);
        input.SetGameplayEnabled(true);
        SetState(RunState.Playing);
    }

    /// <summary>الخسارة تنفذ مرة واحدة؛ واجهة الصحة في المرحلة الثالثة ستستدعي هذه الدالة.</summary>
    public void EndRun(string reason)
    {
        if (!initialized || State == RunState.GameOver || State == RunState.Ready) return;
        GameOverReason = string.IsNullOrWhiteSpace(reason) ? "انتهت الجولة" : reason;
        input.SetGameplayEnabled(false);
        world.SetMoving(false);
        // نسمح لأثر الضربة الأخيرة أن ينتهي بصريًا بعد توقف الجولة.
        runnerCamera.SetSessionPaused(false);
        // نغيّر الحالة قبل إيقاف اللاعب لتجنب تكرار الخسارة إذا عاد الحدث من اللاعب.
        SetState(RunState.GameOver);
        player.StopRun(GameOverReason);
        RunEnded?.Invoke(GameOverReason);
    }

    public void RestartRun()
    {
        if (!initialized) return;
        PrepareRun();
        StartRun();
    }

    private void SetState(RunState next)
    {
        if (State == next) return;
        State = next;
        StateChanged?.Invoke(next);
    }

    // مفاتيح مؤقتة للتجربة إلى أن نضيف الواجهة: Enter للبدء، Escape للإيقاف، R لإعادة الجولة.
    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (!desktopSessionControls || !initialized) return;
        bool start = false, pause = false, restart = false;
#if ENABLE_LEGACY_INPUT_MANAGER
        start = Input.GetKeyDown(KeyCode.Return);
        pause = Input.GetKeyDown(KeyCode.Escape);
        restart = Input.GetKeyDown(KeyCode.R);
#elif ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            start = keyboard.enterKey.wasPressedThisFrame;
            pause = keyboard.escapeKey.wasPressedThisFrame;
            restart = keyboard.rKey.wasPressedThisFrame;
        }
#endif
        if (restart) RestartRun();
        else if (pause)
        {
            if (State == RunState.Playing) PauseRun();
            else if (State == RunState.Paused) ResumeRun();
        }
        else if (start) StartRun();
#endif
    }
}

