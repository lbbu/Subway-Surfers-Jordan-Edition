using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>مخزن العملات مستقل عن توليد الطريق؛ يحتفظ بالنسخ بين الجولات.</summary>
public sealed class RunnerCoinPool : MonoBehaviour
{
    [SerializeField] private RunnerPooledCoin prefab;
    private readonly Stack<RunnerPooledCoin> available = new Stack<RunnerPooledCoin>();
    private readonly Dictionary<RunnerPooledCoin, RunnerTrackSegment> active = new Dictionary<RunnerPooledCoin, RunnerTrackSegment>();
    private readonly List<RunnerPooledCoin> releaseBuffer = new List<RunnerPooledCoin>();
    public int Collected { get; private set; }
    public int CreatedCount { get; private set; }
    public int ActiveCount => active.Count;
    public event Action<int> CoinCollected;

    public void ResetCounter() => Collected = 0;
    public void Populate(RunnerTrackSegment segment)
    {
        if (!Application.isPlaying || !prefab) return;
        Release(segment);
        foreach (var row in segment.GetComponentsInChildren<RunnerCoinRow>())
        {
            for (int i = 0; i < row.count; i++)
            {
                RunnerPooledCoin coin = available.Count > 0 ? available.Pop() : CreateCoin();
                coin.transform.SetParent(row.transform, false);
                coin.transform.localPosition = row.Offset(i);
                coin.transform.localRotation = prefab.transform.localRotation;
                coin.Bind(this);
                active.Add(coin, segment);
                coin.gameObject.SetActive(true);
            }
        }
    }

    private RunnerPooledCoin CreateCoin()
    {
        CreatedCount++;
        return Instantiate(prefab, transform);
    }

    public void Collect(RunnerPooledCoin coin)
    {
        // القاموس يمنع احتساب العملة مرتين عند وصول أكثر من Trigger في نفس الخطوة.
        if (!active.ContainsKey(coin)) return;
        Return(coin);
        Collected++;
        CoinCollected?.Invoke(Collected);
    }

    public void Release(RunnerTrackSegment segment)
    {
        releaseBuffer.Clear();
        foreach (var item in active) if (item.Value == segment) releaseBuffer.Add(item.Key);
        foreach (var coin in releaseBuffer) Return(coin);
    }

    private void Return(RunnerPooledCoin coin)
    {
        active.Remove(coin);
        if (!coin) return;
        coin.gameObject.SetActive(false);
        coin.transform.SetParent(transform, false);
        available.Push(coin);
    }
}
