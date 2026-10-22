using System.Collections.Generic;
using UnityEngine;

/// <summary>ربط مؤقت مع مدير الماب القديم حتى استبداله في المرحلة الثانية؛ لا يغيّر ملفه الأصلي.</summary>
[DisallowMultipleComponent]
public sealed class LegacyChunkSessionBridge : RunnerWorldSource
{
    [SerializeField] private ChunkManager source;

    private struct SegmentSnapshot
    {
        public GameObject Object;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool Active;
    }

    private readonly List<GameObject> initialLoaded = new List<GameObject>();
    private readonly List<GameObject> initialAvailable = new List<GameObject>();
    private readonly List<SegmentSnapshot> segments = new List<SegmentSnapshot>();
    private readonly List<CoinStartPos> coinOwners = new List<CoinStartPos>();
    private float initialSpeed;
    private bool captured;

    public override float CurrentSpeed => source ? source.chunksSpeed : 0f;
    public override bool IsMoving => source && source.isActiveAndEnabled;

    public override void CaptureInitialState()
    {
        captured = false;
        segments.Clear();
        coinOwners.Clear();
        initialLoaded.Clear();
        initialAvailable.Clear();
        if (!source) return;
        initialSpeed = source.chunksSpeed;
        initialLoaded.AddRange(source.loadedChunks);
        initialAvailable.AddRange(source.Chunks);
        var seen = new HashSet<GameObject>();
        CaptureSegments(initialLoaded, seen);
        CaptureSegments(initialAvailable, seen);
        captured = true;
    }

    private void CaptureSegments(List<GameObject> objects, HashSet<GameObject> seen)
    {
        foreach (var item in objects)
        {
            if (!item || !seen.Add(item)) continue;
            segments.Add(new SegmentSnapshot
            {
                Object = item, Position = item.transform.position,
                Rotation = item.transform.rotation, Active = item.activeSelf
            });
            coinOwners.AddRange(item.GetComponentsInChildren<CoinStartPos>(true));
        }
    }

    public override void SetMoving(bool moving)
    {
        if (source) source.enabled = moving;
    }

    public override void ResetWorld()
    {
        if (!source || !captured) return;
        source.enabled = false;

        // إعادة العملات عبر واجهة المدير نفسه تحافظ على ملكيتها وقوائم الـPool.
        // في وضع التحرير نختبر ترتيب الشوارع فقط، دون إنشاء عملات أو تشغيل Awake يدويًا.
        if (Application.isPlaying)
            foreach (var owner in coinOwners)
                if (owner) source.ReturnCoins(owner.transform);

        foreach (var item in segments)
        {
            if (!item.Object) continue;
            item.Object.transform.SetPositionAndRotation(item.Position, item.Rotation);
            item.Object.SetActive(item.Active);
        }
        source.loadedChunks.Clear();
        source.loadedChunks.AddRange(initialLoaded);
        source.Chunks.Clear();
        source.Chunks.AddRange(initialAvailable);
        source.chunksSpeed = initialSpeed;
        Physics.SyncTransforms();

        if (!Application.isPlaying) return;
        foreach (var owner in coinOwners)
        {
            if (!owner || !owner.gameObject.activeInHierarchy) continue;
            source.RentCoins(owner.transform, owner.coinStyle, owner.coinCount,
                owner.archLength, owner.archHeight);
        }
    }
}

