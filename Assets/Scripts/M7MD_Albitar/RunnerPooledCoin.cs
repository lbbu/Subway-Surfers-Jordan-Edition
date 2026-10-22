using UnityEngine;

/// <summary>عملة تعود للمخزن بعد جمعها بدل تدميرها وإعادة إنشائها.</summary>
public sealed class RunnerPooledCoin : MonoBehaviour
{
    private RunnerCoinPool pool;
    public void Bind(RunnerCoinPool owner) => pool = owner;
    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerMovements>();
        if (pool && player && player.IsRunning) pool.Collect(this);
    }
}
