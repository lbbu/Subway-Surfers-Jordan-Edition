using UnityEngine;

/// <summary>عدد ثابت من مؤثرات قصيرة يعاد استخدامها؛ لا Instantiate عند كل هبوط أو اصطدام.</summary>
public sealed class RunnerEffectPool : MonoBehaviour
{
    public enum Effect { Impact, Dust, Slide }
    [SerializeField] private ParticleSystem impactPrefab;
    [SerializeField] private ParticleSystem dustPrefab;
    [SerializeField] private ParticleSystem slidePrefab;
    [SerializeField, Range(2, 12)] private int copiesPerEffect = 4;
    private ParticleSystem[] instances;
    private int[] cursors;
    public int Capacity => instances == null ? 0 : instances.Length;
    public void Initialize()
    {
        if (instances != null && instances.Length > 0 && instances[0]) return;
        var prefabs = new[] { impactPrefab, dustPrefab, slidePrefab };
        instances = new ParticleSystem[copiesPerEffect * prefabs.Length]; cursors = new int[3];
        for (int type = 0; type < 3; type++) for (int i = 0; i < copiesPerEffect; i++)
        {
            if (!prefabs[type]) continue;
            var effect = Instantiate(prefabs[type], transform); effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            instances[type * copiesPerEffect + i] = effect;
        }
    }
    public ParticleSystem Emit(Effect type, Vector3 position, float strength, float worldSpeed)
    {
        Initialize(); int group = (int)type;
        var effect = instances[group * copiesPerEffect + cursors[group]];
        cursors[group] = (cursors[group] + 1) % copiesPerEffect;
        if (!effect) return null;
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.transform.position = position;
        var velocity = effect.velocityOverLifetime;
        velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = 0; velocity.y = 0; velocity.z = -Mathf.Max(0, worldSpeed);
        effect.Play(true);
        effect.Emit(Mathf.RoundToInt((type == Effect.Impact ? 16 : type == Effect.Slide ? 5 : 12) * Mathf.Clamp(strength, .3f, 2f)));
        return effect;
    }
    public void SetPaused(bool paused)
    {
        if (instances == null) return;
        foreach (var effect in instances) if (effect)
        { if (paused && effect.isPlaying) effect.Pause(true); else if (!paused && effect.isPaused) effect.Play(true); }
    }
    public void Clear()
    {
        if (instances == null) return;
        foreach (var effect in instances) if (effect) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
