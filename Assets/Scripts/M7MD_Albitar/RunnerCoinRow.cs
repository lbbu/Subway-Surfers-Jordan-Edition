using UnityEngine;

/// <summary>وصف صف العملات على قطعة الطريق دون اعتماد على مدير الفريق القديم.</summary>
public sealed class RunnerCoinRow : MonoBehaviour
{
    public bool arch;
    [Min(0)] public int count = 5;
    [Min(0.1f)] public float spacing = 1.5f;
    [Min(0)] public float archLength = 8f;
    [Min(0)] public float archHeight = 3f;
    [Tooltip("تمديد القوس حسب سرعة الوصول وزمن قفزة اللاعب؛ القيم اليدوية تبقى بديلًا عند تعطيله.")]
    public bool matchPlayerJump = true;

    // نثبت التوزيع عند تجهيز القطعة، فلا تتحرك العملات فجأة أمام اللاعب مع كل تغير صغير للسرعة.
    public Vector3 Offset(int index, PlayerMovements player, RunnerTrackWorld world)
    {
        if (!arch || !matchPlayerJump || !player || !world) return Offset(index);
        float fraction = count > 1 ? index / (float)(count - 1) : 0f;
        float elapsed = fraction * player.JumpArcDuration;
        float speedAtTakeoff = world.PredictSpeedAfterDistance(transform.position.z - player.FixedZ);
        Vector3 worldOffset = Vector3.forward * world.PredictTravelDistance(elapsed, speedAtTakeoff)
            + Vector3.up * player.JumpArcHeight(elapsed);
        // اتجاه اللعب عالمي +Z؛ التحويل يحافظ على المسافة حتى لو تغير Scale للأب.
        return transform.InverseTransformVector(worldOffset);
    }

    public Vector3 Offset(int index)
    {
        float t = count > 1 ? index / (float)(count - 1) : 0;
        return arch ? new Vector3(0, Mathf.Sin(t * Mathf.PI) * archHeight, t * archLength)
            : new Vector3(0, 0, index * spacing);
    }
}
