using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public sealed class PooledVFX : MonoBehaviour
{
    public event EventHandler<VFXFinishedEventArgs> onVFXFinished;

    public sealed class VFXFinishedEventArgs : EventArgs
    {
        public readonly PooledVFX pooledVFX;

        public VFXFinishedEventArgs(PooledVFX pooledVFX)
        {
            this.pooledVFX = pooledVFX;
        }
    }

    public VFX_ID Id { get; private set; }
    public bool IsRented { get; private set; }
    public bool IsPlaying => IsRented && particle.IsAlive(true);

    private ParticleSystem particle;
    private ParticleSystem[] particles;
    private ParticleSystem.MinMaxGradient[] originalColors;
    private float[] originalSimulationSpeeds;
    private Vector3 originalScale;
    private VFXFinishedEventArgs finishedEventArgs;

    internal void Initialize(VFX_ID id)
    {
        Id = id;
        particle = GetComponent<ParticleSystem>();
        particles = GetComponentsInChildren<ParticleSystem>(true);
        originalColors = new ParticleSystem.MinMaxGradient[particles.Length];
        originalSimulationSpeeds = new float[particles.Length];
        originalScale = transform.localScale;
        finishedEventArgs = new VFXFinishedEventArgs(this);

        for (int i = 0; i < particles.Length; i++)
        {
            var main = particles[i].main;
            originalColors[i] = main.startColor;
            originalSimulationSpeeds[i] = main.simulationSpeed;

            // The pool controls playback and the lifetime of these GameObjects.
            main.loop = false;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    internal void Play(VFXPlayParams parameters)
    {
        Quaternion rotation = parameters.Rotation;
        if (Quaternion.Dot(rotation, rotation) < 0.000001f)
        {
            rotation = Quaternion.identity;
        }

        transform.SetPositionAndRotation(parameters.Position, rotation);
        transform.localScale = originalScale * (parameters.Scale ?? 1f);

        for (int i = 0; i < particles.Length; i++)
        {
            var main = particles[i].main;
            main.startColor = parameters.Color.HasValue
                ? new ParticleSystem.MinMaxGradient(parameters.Color.Value)
                : originalColors[i];
            main.simulationSpeed = parameters.SimulationSpeed
                ?? originalSimulationSpeeds[i];
        }

        enabled = true;
        IsRented = true;
        gameObject.SetActive(true);
        particle.Play(true);
    }

    private void Update()
    {
        if (IsRented && !particle.IsAlive(true))
        {
            onVFXFinished?.Invoke(this, finishedEventArgs);
        }
    }

    private void OnDisable()
    {
        // Disabling a rented effect cancels it and returns it to its pool.
        if (IsRented)
        {
            onVFXFinished?.Invoke(this, finishedEventArgs);
        }
    }

    internal void ReturnToPool()
    {
        // Clear the rental flag first to prevent OnDisable from returning twice.
        IsRented = false;
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false);
    }
}
