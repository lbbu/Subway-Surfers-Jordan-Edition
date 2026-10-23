using System;
using UnityEngine;

/// <summary>تسلسل قابل للإعادة: مسار مفتوح دائمًا، ومسافة قرار بين العوائق محسوبة للسرعة القصوى.</summary>
public sealed class RunnerPatternDirector : MonoBehaviour
{
    [SerializeField] private PlayerMovements player;
    [SerializeField] private int seed = 20261023;
    [SerializeField] private bool randomizeSeed;
    [SerializeField, Min(10)] private float warmupDistance = 45f;
    [SerializeField, Min(100)] private float fullDifficultyDistance = 1200f;
    [SerializeField, Min(1.1f)] private float minimumDecisionSeconds = 1.25f;
    private System.Random random;
    private double lastEnd;
    private int lastSafeLane = 1;
    public int ActiveSeed { get; private set; }
    public int GeneratedChallenges { get; private set; }
    public float Difficulty { get; private set; }
    public double LastGap { get; private set; }
    public void ResetSequence()
    {
        ActiveSeed = randomizeSeed ? Environment.TickCount : seed;
        random = new System.Random(ActiveSeed); lastEnd = -10000; lastSafeLane = 1; GeneratedChallenges = 0; Difficulty = 0;
    }
    public void Prepare(RunnerTrackSegment segment, double distance, float maximumSpeed)
    {
        if (!Application.isPlaying) return;
        if (random == null) ResetSequence();
        var layout = segment.GetComponent<RunnerPatternLayout>(); if (!layout) return;
        double startDistance = distance + segment.StartZ - player.FixedZ;
        Difficulty = Mathf.Clamp01((float)(startDistance - warmupDistance) / fullDifficultyDistance);
        int maxPattern = Difficulty < .2f ? 1 : Difficulty < .5f ? 2 : 4;
        int pattern = random.Next(1, maxPattern + 1);
        float centre = (segment.StartZ + segment.EndZ) * .5f;
        double enter = distance + centre - player.FixedZ - (pattern == 4 ? 9.5f : 1f);
        double end = distance + centre - player.FixedZ + (pattern == 4 ? 7.2f : 1f);
        double requiredGap = maximumSpeed * Mathf.Lerp(2f, minimumDecisionSeconds, Difficulty);
        // نرفض النمط إذا لم يتسع داخل القطعة أو لم يترك مهلة تغيير مسار بعد التحدي السابق.
        bool fits = pattern != 4 || segment.Length >= 21f;
        if (startDistance < warmupDistance || !fits || enter - lastEnd < requiredGap)
        { layout.Apply(0, lastSafeLane); return; }
        int safe = Mathf.Clamp(lastSafeLane + random.Next(-1, 2), 0, 2);
        layout.Apply(pattern, safe);
        LastGap = enter - lastEnd; lastEnd = end; lastSafeLane = safe; GeneratedChallenges++;
    }
}
