using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager instance;

    public class Building
    {
        public GameObject go;
        public Rigidbody rb;
        public float baseLength;
        public float baseCenterOffsetZ;
        public Vector3 baseScale;
        public float length;
        public float centerOffsetZ;
        public Vector3 pos;
        public bool active;
        public int side;
    }

    [Serializable]
    public class Layer
    {
        public string name = "Layer";
        public bool enabled = true;

        [Header("Prefabs & Pool")]
        public List<GameObject> prefabs = new List<GameObject>();
        [Min(1)] public int instancesPerPrefab = 3;

        [Header("Spawning")]
        public bool spawnOnce = false;
        [Min(1)] public int pickSize = 2;
        [Range(0f, 1f)] public float leftChance = 0.5f;
        public float gap = 2f;
        [Range(0f, 1f)] public float emptyChance = 0f;
        public Vector2 emptyLength = new Vector2(5f, 15f);
        public float startOffsetZ = 0f;
        public float spawnDistance = 150f;

        [Header("Placement")]
        public float sideOffset = 0f;
        public Vector2 height = Vector2.zero;
        public Vector2 scale = Vector2.one;

        [Header("Movement")]
        [Range(0f, 10f)] public float speedMultiplier = 1f;
        public float despawnZ = -60f;

        [NonSerialized] public List<Building> pool;
        [NonSerialized] public List<Building> active;
        [NonSerialized] public float[] tailZ;
        [NonSerialized] public Transform poolRoot;
        [NonSerialized] public Transform[] containers;
    }

    [SerializeField] private List<Layer> layers = new List<Layer>();

    [Header("Anchors")]
    [SerializeField] private Transform leftAnchor;
    [SerializeField] private Transform rightAnchor;

    [SerializeField] private float fallbackLength = 10f;

    private Transform Anchor(int side) => side == 0 ? leftAnchor : rightAnchor;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        foreach (Layer l in layers) BuildLayer(l);
        ResetSpawner();
    }

    private void BuildLayer(Layer l)
    {
        l.pool = new List<Building>();
        l.active = new List<Building>();
        l.tailZ = new float[2];
        l.containers = new Transform[2];

        l.poolRoot = new GameObject(l.name + "_Pool").transform;
        l.poolRoot.SetParent(transform, false);

        for (int s = 0; s < 2; s++)
        {
            Transform c = new GameObject(l.name + (s == 0 ? "_Left" : "_Right")).transform;
            c.SetParent(Anchor(s), false);
            l.containers[s] = c;
        }

        foreach (GameObject prefab in l.prefabs)
        {
            if (prefab == null) continue;

            for (int i = 0; i < l.instancesPerPrefab; i++)
            {
                GameObject go = Instantiate(prefab, l.poolRoot);
                go.SetActive(true);
                go.transform.SetPositionAndRotation(leftAnchor.position, leftAnchor.rotation);

                Building b = new Building
                {
                    go = go,
                    rb = go.GetComponent<Rigidbody>(),
                    baseScale = go.transform.localScale
                };
                Measure(b);
                go.SetActive(false);
                l.pool.Add(b);
            }
        }
    }

    private void Measure(Building b)
    {
        Renderer[] rends = b.go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
        {
            b.baseLength = fallbackLength;
            b.baseCenterOffsetZ = 0f;
            return;
        }

        Bounds bounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) bounds.Encapsulate(rends[i].bounds);

        b.baseLength = bounds.size.z;
        b.baseCenterOffsetZ = bounds.center.z - b.go.transform.position.z;
    }

    private Building GetRandomAvailable(Layer l)
    {
        int count = 0;
        foreach (Building b in l.pool) if (!b.active) count++;
        if (count == 0) return null;

        int pick = Random.Range(0, count);
        foreach (Building b in l.pool)
        {
            if (b.active) continue;
            if (pick-- == 0) return b;
        }
        return null;
    }

    private bool IsUsable(Layer l) => l.enabled && l.pool != null && l.pool.Count > 0;

    public void Move(float movement)
    {
        foreach (Layer l in layers)
        {
            if (!IsUsable(l)) continue;
            MoveLayer(l, movement * l.speedMultiplier);
        }
    }

    private void MoveLayer(Layer l, float m)
    {
        l.tailZ[0] -= m;
        l.tailZ[1] -= m;

        for (int i = l.active.Count - 1; i >= 0; i--)
        {
            Building b = l.active[i];
            b.pos.z -= m;

            if (b.rb != null) b.rb.MovePosition(b.pos);
            else b.go.transform.position = b.pos;

            float farEdge = b.pos.z + b.centerOffsetZ + b.length * 0.5f;
            if (farEdge < l.despawnZ) Recycle(l, b, i);
        }

        if (l.spawnOnce) return;

        int guard = 0;
        while (NeedsMore(l) && guard++ < 20)
            if (!SpawnBatch(l, true)) break;
    }

    public void ResetSpawner()
    {
        foreach (Layer l in layers)
        {
            if (!IsUsable(l)) continue;

            for (int i = l.active.Count - 1; i >= 0; i--) Recycle(l, l.active[i], i);

            l.tailZ[0] = leftAnchor.position.z + l.startOffsetZ;
            l.tailZ[1] = rightAnchor.position.z + l.startOffsetZ;

            int guard = 0;
            while (NeedsMore(l) && guard++ < 200)
            {

                if (!SpawnBatch(l, false))
                {
                    Debug.LogWarning("BackgroundManager: pool of layer '" + l.name + "' is too small to fill the street. Increase instancesPerPrefab or add prefabs.");
                    break;
                }
            }
        }
    }

    private bool NeedsMore(Layer l, int side) => l.tailZ[side] < Anchor(side).position.z + l.spawnDistance;
    private bool NeedsMore(Layer l) => NeedsMore(l, 0) || NeedsMore(l, 1);

    private int ChooseSide(Layer l)
    {
        bool needL = NeedsMore(l, 0), needR = NeedsMore(l, 1);
        if (needL && !needR) return 0;
        if (needR && !needL) return 1;
        return Random.value < l.leftChance ? 0 : 1;
    }

    private bool SpawnBatch(Layer l, bool snapToSpawn)
    {
        bool progressed = false;

        for (int i = 0; i < l.pickSize; i++)
        {
            int side = ChooseSide(l);

            if (Random.value < l.emptyChance)
            {
                float minLen = Mathf.Max(0.1f, Mathf.Min(l.emptyLength.x, l.emptyLength.y));
                float maxLen = Mathf.Max(minLen, Mathf.Max(l.emptyLength.x, l.emptyLength.y));
                l.tailZ[side] += Random.Range(minLen, maxLen);
                progressed = true;
                continue;
            }

            Building b = GetRandomAvailable(l);
            if (b == null) break;

            Place(l, b, side, snapToSpawn);
            progressed = true;
        }
        return progressed;
    }

    private void Place(Layer l, Building b, int side, bool snapToSpawn)
    {
        Transform anchor = Anchor(side);

        float s = Random.Range(Mathf.Min(l.scale.x, l.scale.y), Mathf.Max(l.scale.x, l.scale.y));
        b.length = b.baseLength * s;
        b.centerOffsetZ = b.baseCenterOffsetZ * s;

        float nearEdge = l.tailZ[side] + l.gap;

        if (snapToSpawn)
            nearEdge = Mathf.Max(nearEdge, anchor.position.z + l.spawnDistance);

        float centerZ = nearEdge + b.length * 0.5f;
        float pivotZ = centerZ - b.centerOffsetZ;

        float dir = side == 0 ? -1f : 1f;
        Vector3 pos = anchor.position + anchor.right * dir * l.sideOffset;
        pos.y += Random.Range(Mathf.Min(l.height.x, l.height.y), Mathf.Max(l.height.x, l.height.y));
        pos.z = pivotZ;

        b.side = side;
        b.pos = pos;

        b.go.transform.SetParent(l.containers[side], true);
        b.go.transform.SetPositionAndRotation(pos, anchor.rotation);
        b.go.transform.localScale = b.baseScale * s;
        if (b.rb != null) b.rb.position = pos;

        b.go.SetActive(true);
        b.active = true;
        l.active.Add(b);

        l.tailZ[side] = nearEdge + b.length;
    }

    private void Recycle(Layer l, Building b, int activeIndex)
    {
        b.go.SetActive(false);
        b.go.transform.SetParent(l.poolRoot, true);
        b.active = false;
        l.active.RemoveAt(activeIndex);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (leftAnchor != null) Gizmos.DrawWireSphere(leftAnchor.position, 1f);
        if (rightAnchor != null) Gizmos.DrawWireSphere(rightAnchor.position, 1f);

        Gizmos.color = Color.red;
        foreach (Layer l in layers)
        {
            if (l == null || !l.enabled) continue;

            if (leftAnchor != null)
            {
                Vector3 p = leftAnchor.position - leftAnchor.right * l.sideOffset;
                Gizmos.DrawLine(new Vector3(p.x - 5f, p.y, l.despawnZ), new Vector3(p.x + 5f, p.y, l.despawnZ));
            }
            if (rightAnchor != null)
            {
                Vector3 p = rightAnchor.position + rightAnchor.right * l.sideOffset;
                Gizmos.DrawLine(new Vector3(p.x - 5f, p.y, l.despawnZ), new Vector3(p.x + 5f, p.y, l.despawnZ));
            }
        }
    }
}