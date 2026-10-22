using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// Mixes the existing controller with a standalone fall clip at runtime.
/// Neither the Animator Controller asset nor the AnimationClip is modified.
/// </summary>
public sealed class RunnerFallAnimation : IDisposable
{
    private PlayableGraph graph;
    private AnimatorControllerPlayable baseController;
    private AnimationClipPlayable fallClip;
    private AnimationMixerPlayable mixer;
    private readonly float blendTime;
    private bool falling, stopped;
    public float FallWeight { get; private set; }

    public RunnerFallAnimation(Animator animator, AnimationClip clip, float transitionTime)
    {
        blendTime = Mathf.Max(0.01f, transitionTime);
        graph = PlayableGraph.Create("Runner fall overlay");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        baseController = AnimatorControllerPlayable.Create(graph, animator.runtimeAnimatorController);
        fallClip = AnimationClipPlayable.Create(graph, clip);
        fallClip.SetApplyFootIK(false);
        mixer = AnimationMixerPlayable.Create(graph, 2);
        graph.Connect(baseController, 0, mixer, 0);
        graph.Connect(fallClip, 0, mixer, 1);
        mixer.SetInputWeight(0, 1f);
        mixer.SetInputWeight(1, 0f);
        var output = AnimationPlayableOutput.Create(graph, "Runner animation", animator);
        output.SetSourcePlayable(mixer);
        graph.Play();
    }

    public void SetFalling(bool value)
    {
        if (value && !falling) fallClip.SetTime(0);
        falling = value;
    }

    public bool CrossFadeBase(int stateHash, float duration)
    {
        if (!baseController.HasState(0, stateHash)) return false;
        baseController.CrossFadeInFixedTime(stateHash, duration, 0, 0f);
        return true;
    }

    public void SetBaseSpeed(float speed) => baseController.SetSpeed(stopped ? 0 : speed);

    public void SetStopped(bool value)
    {
        stopped = value;
        fallClip.SetSpeed(stopped ? 0 : 1);
        if (stopped) baseController.SetSpeed(0);
    }

    public void Tick(float deltaTime)
    {
        if (!graph.IsValid() || stopped) return;
        FallWeight = Mathf.MoveTowards(FallWeight, falling ? 1f : 0f, deltaTime / blendTime);
        mixer.SetInputWeight(0, 1f - FallWeight);
        mixer.SetInputWeight(1, FallWeight);
    }

#if UNITY_EDITOR
    /// <summary>Evaluate an editor preview without entering Play Mode.</summary>
    public void EvaluatePreview(float deltaTime)
    {
        if (!graph.IsValid()) return;
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        graph.Evaluate(Mathf.Max(0f, deltaTime));
    }
#endif

    public void Pause() { if (graph.IsValid()) graph.Stop(); }
    public void Resume() { if (graph.IsValid()) graph.Play(); }
    public void Dispose() { if (graph.IsValid()) graph.Destroy(); }
}
