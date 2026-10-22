using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// One animation mixer for locomotion. Each clip keeps its own playback rate;
/// retargeting a blend preserves the currently visible pose and running phase.
/// The controller and source clips remain unchanged.
/// </summary>
public sealed class RunnerAnimationBlender : IDisposable
{
    public enum Motion { Run, Jump, Fall, Slide }

    private const int MotionCount = 4;
    private readonly AnimationClip[] clips;
    private readonly AnimationClipPlayable[] players = new AnimationClipPlayable[MotionCount];
    private readonly float[] weights = new float[MotionCount];
    private readonly float[] sourceWeights = new float[MotionCount];
    private readonly float[] speeds = { 1f, 1f, 1f, 1f };
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private bool initialized, stopped;
    private float blendElapsed, blendDuration;

    public Motion CurrentMotion { get; private set; }
    public float FallWeight => GetWeight(Motion.Fall);

    public RunnerAnimationBlender(Animator animator, AnimationClip run,
        AnimationClip jump, AnimationClip fall, AnimationClip slide)
    {
        clips = new[] { run, jump, fall, slide };
        graph = PlayableGraph.Create("Runner locomotion blend");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        mixer = AnimationMixerPlayable.Create(graph, MotionCount);
        for (int i = 0; i < MotionCount; i++)
        {
            players[i] = AnimationClipPlayable.Create(graph, clips[i]);
            players[i].SetApplyFootIK(false);
            graph.Connect(players[i], 0, mixer, i);
        }
        var output = AnimationPlayableOutput.Create(graph, "Runner animation", animator);
        output.SetSourcePlayable(mixer);
        Reset();
        graph.Play();
    }

    public float GetWeight(Motion motion) => weights[(int)motion];

    /// <summary>A new run starts with a complete pose, never a blend from the bind pose.</summary>
    public void Reset()
    {
        stopped = false;
        initialized = false;
        for (int i = 0; i < MotionCount; i++)
        {
            weights[i] = sourceWeights[i] = 0f;
            speeds[i] = 1f;
            players[i].SetTime(0);
            players[i].SetSpeed(1);
        }
        Play(Motion.Run, 0f);
    }

    public void Play(Motion motion, float duration, float playbackSpeed = 1f)
    {
        int next = (int)motion;
        playbackSpeed = Mathf.Max(0.01f, playbackSpeed);
        if (initialized && motion == CurrentMotion) return;

        // If an interrupted blend still shows this destination, preserve its time.
        // Rewinding a partially visible slide/jump would introduce a pose discontinuity.
        if (motion != Motion.Run && weights[next] < 0.001f)
            players[next].SetTime(0);
        speeds[next] = playbackSpeed;
        players[next].SetSpeed(stopped ? 0 : playbackSpeed);
        for (int i = 0; i < MotionCount; i++) sourceWeights[i] = weights[i];
        CurrentMotion = motion;
        blendElapsed = 0f;
        blendDuration = initialized ? Mathf.Max(0f, duration) : 0f;
        initialized = true;
        if (blendDuration <= 0f) ApplyBlend(1f);
    }

    /// <summary>Advance blend weights once per rendered frame, independently of physics ticks.</summary>
    public void Tick(float deltaTime)
    {
        if (!graph.IsValid() || stopped || deltaTime <= 0f) return;
        if (blendElapsed < blendDuration)
        {
            blendElapsed = Mathf.Min(blendDuration, blendElapsed + deltaTime);
            float t = blendElapsed / blendDuration;
            ApplyBlend(t * t * (3f - 2f * t));
        }
        // A prolonged fall or blocked stand-up must not restart a one-shot action.
        for (int i = 0; i < MotionCount; i++)
        {
            if (clips[i].isLooping || players[i].GetTime() < clips[i].length) continue;
            players[i].SetTime(clips[i].length);
            players[i].SetSpeed(0);
        }
    }

    private void ApplyBlend(float easedTime)
    {
        int next = (int)CurrentMotion;
        for (int i = 0; i < MotionCount; i++)
        {
            weights[i] = Mathf.Lerp(sourceWeights[i], i == next ? 1f : 0f, easedTime);
            mixer.SetInputWeight(i, weights[i]);
        }
    }

    public void SetStopped(bool value)
    {
        if (stopped == value) return;
        stopped = value;
        for (int i = 0; i < MotionCount; i++)
        {
            bool completed = !clips[i].isLooping && players[i].GetTime() >= clips[i].length;
            players[i].SetSpeed(stopped || completed ? 0 : speeds[i]);
        }
    }

#if UNITY_EDITOR
    /// <summary>Evaluate the owned graph for an editor preview without entering Play Mode.</summary>
    public void EvaluatePreview(float deltaTime)
    {
        if (!graph.IsValid()) return;
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        graph.Evaluate(Mathf.Max(0f, deltaTime));
    }
#endif

    public void Pause() { if (graph.IsValid()) graph.Stop(); }
    public void Resume()
    {
        if (!graph.IsValid()) return;
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        graph.Play();
    }
    public void Dispose() { if (graph.IsValid()) graph.Destroy(); }
}
