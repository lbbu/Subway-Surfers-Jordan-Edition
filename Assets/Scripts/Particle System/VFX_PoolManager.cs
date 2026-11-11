using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class VFX_PoolManager : MonoBehaviour
{
    [SerializeField] private List<VFXPoolEntry> entries = new List<VFXPoolEntry>();

    private readonly Dictionary<VFX_ID, Queue<PooledVFX>> availableById =
        new Dictionary<VFX_ID, Queue<PooledVFX>>();

    public static VFX_PoolManager Instance { get; private set; }

    private bool isReady;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one VFX_PoolManager can be active.", this);
            enabled = false;
            return;
        }

        Instance = this;
        InitializePools();
    }

    private void InitializePools()
    {
        // Validate every entry before creating any pooled GameObjects.
        foreach (VFXPoolEntry entry in entries)
        {
            if (entry == null || entry.Prefab == null || entry.PrewarmCount < 1)
            {
                Debug.LogError("Each VFX entry needs a prefab and a positive count.", this);
                availableById.Clear();
                return;
            }

            if (availableById.ContainsKey(entry.Id))
            {
                Debug.LogError($"Duplicate VFX ID: {entry.Id}.", this);
                availableById.Clear();
                return;
            }

            foreach (ParticleSystem system in entry.Prefab.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (!IsPositiveFinite(system.main.simulationSpeed))
                {
                    Debug.LogError($"VFX {entry.Id} needs a positive simulation speed on every Particle System.", this);
                    availableById.Clear();
                    return;
                }
            }

            availableById.Add(entry.Id, new Queue<PooledVFX>(entry.PrewarmCount));
        }

        foreach (VFXPoolEntry entry in entries)
        {
            var poolParent = new GameObject($"{entry.Id}_Pool");
            poolParent.transform.SetParent(transform, false);

            // Keep clones inactive until initialization has finished.
            poolParent.SetActive(false);
            Queue<PooledVFX> available = availableById[entry.Id];

            for (int i = 0; i < entry.PrewarmCount; i++)
            {
                PooledVFX effect = Instantiate(entry.Prefab, poolParent.transform);
                effect.gameObject.SetActive(false);
                effect.Initialize(entry.Id);
                effect.onVFXFinished += HandleVFXFinished;
                available.Enqueue(effect);
            }

            poolParent.SetActive(true);
        }

        isReady = true;
    }

    public bool TryPlay(VFX_ID id)
    {
        return TryPlay(id, transform.position, transform.rotation);
    }

    public bool TryPlay(VFX_ID id, Color color)
    {
        return TryPlay(id, transform.position, transform.rotation, color);
    }

    public bool TryPlay(
        VFX_ID id,
        Vector3 position,
        Quaternion? rotation = null,
        Color? color = null,
        float? scale = null,
        float? simulationSpeed = null)
    {
        return TryPlay(id, new VFXPlayParams(position, rotation)
        {
            Color = color,
            Scale = scale,
            SimulationSpeed = simulationSpeed
        });
    }

    public bool TryPlay(VFX_ID id, VFXPlayParams parameters)
    {
        if (!isReady || !isActiveAndEnabled)
        {
            return false;
        }

        if ((parameters.Scale.HasValue && !IsPositiveFinite(parameters.Scale.Value)) ||
            (parameters.SimulationSpeed.HasValue &&
             !IsPositiveFinite(parameters.SimulationSpeed.Value)))
        {
            Debug.LogWarning("VFX scale and simulation speed must be positive finite values.", this);
            return false;
        }

        if (!availableById.TryGetValue(id, out Queue<PooledVFX> available) ||
            available.Count == 0)
        {
            return false;
        }

        PooledVFX effect = available.Peek();
        if (!effect.transform.parent.gameObject.activeInHierarchy)
        {
            return false;
        }

        available.Dequeue();
        effect.Play(parameters);
        return true;
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && !float.IsInfinity(value);
    }

    internal void HandleVFXFinished(object sender, PooledVFX.VFXFinishedEventArgs e)
    {
        PooledVFX effect = e.pooledVFX;
        if (!isReady || effect == null || !effect.IsRented)
        {
            return;
        }

        effect.ReturnToPool();
        availableById[effect.Id].Enqueue(effect);
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        isReady = false;
        Instance = null;

        foreach (PooledVFX effect in GetComponentsInChildren<PooledVFX>(true))
        {
            effect.onVFXFinished -= HandleVFXFinished;
        }

        availableById.Clear();
    }
}
