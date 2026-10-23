using UnityEngine;

/// <summary>رد فعل الجمع يأتي من حدث واحد بعد احتساب العملة، مع حد ثابت لنسخ البارتكلز.</summary>
public sealed class RunnerCoinFeedback : MonoBehaviour
{
    [SerializeField] private RunnerCoinPool coins;
    [SerializeField] private RunSession session;
    [SerializeField] private ParticleSystem burstPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip collectSound;
    private ParticleSystem[] bursts;
    private int cursor;
    private bool bound;
    private float lastSound = -10;
    private void OnEnable() => Initialize();
    public void Initialize()
    {
        if (!bound && coins && session)
        { coins.CoinPicked += Picked; session.RunPrepared += Clear; session.StateChanged += StateChanged; bound = true; }
        if (!Application.isPlaying || !burstPrefab || (bursts != null && bursts.Length > 0 && bursts[0])) return;
        bursts = new ParticleSystem[6];
        for (int i = 0; i < bursts.Length; i++) { bursts[i] = Instantiate(burstPrefab, transform); bursts[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
    }
    private void OnDisable()
    {
        if (bound) { if (coins) coins.CoinPicked -= Picked; if (session) { session.RunPrepared -= Clear; session.StateChanged -= StateChanged; } }
        bound = false; Clear();
    }
    private void Picked(Vector3 point)
    {
        if (bursts != null && bursts.Length > 0)
        {
            var burst = bursts[cursor++ % bursts.Length]; burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            burst.transform.position = point; burst.Play(); burst.Emit(7);
        }
        // العملات المتجاورة لا تشغّل عشرات الأصوات في نفس اللحظة.
        if (audioSource && collectSound && Time.unscaledTime - lastSound > .055f)
        { audioSource.PlayOneShot(collectSound, .65f); lastSound = Time.unscaledTime; }
    }
    private void Clear()
    {
        cursor = 0; lastSound = -10;
        if (bursts != null) foreach (var burst in bursts) if (burst) burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (audioSource) audioSource.Stop();
    }
    private void StateChanged(RunSession.RunState state)
    {
        if (bursts == null) return;
        foreach (var burst in bursts) if (burst)
        { if (state == RunSession.RunState.Paused && burst.isPlaying) burst.Pause(); else if (state != RunSession.RunState.Paused && burst.isPaused) burst.Play(); }
    }
}
