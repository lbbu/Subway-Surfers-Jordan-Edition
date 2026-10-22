using UnityEngine;

/// <summary>وصف صف العملات على قطعة الطريق دون اعتماد على مدير الفريق القديم.</summary>
public sealed class RunnerCoinRow : MonoBehaviour
{
    public bool arch;
    [Min(0)] public int count = 5;
    [Min(0.1f)] public float spacing = 1.5f;
    [Min(0)] public float archLength = 8f;
    [Min(0)] public float archHeight = 3f;
    public Vector3 Offset(int index)
    {
        float t = count > 1 ? index / (float)(count - 1) : 0;
        return arch ? new Vector3(0, Mathf.Sin(t * Mathf.PI) * archHeight, t * archLength)
            : new Vector3(0, 0, index * spacing);
    }
}
