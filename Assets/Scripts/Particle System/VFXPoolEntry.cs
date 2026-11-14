using System;
using UnityEngine;

[Serializable]
public sealed class VFXPoolEntry
{
    public VFX_ID Id;
    public PooledVFX Prefab;
    [Min(1)] public int PrewarmCount = 4;
}
