using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>مخزن العملات مستقل عن توليد الطريق؛ يحتفظ بالنسخ بين الجولات.</summary>
public sealed class RunnerCoinPool : MonoBehaviour
{
    [SerializeField] private RunnerPooledCoin prefab;
    [SerializeField] private RunSession session;
    [SerializeField] private PlayerMovements player;
    [SerializeField] private RunnerTrackWorld world;
    [SerializeField, Min(0)] private int prewarmCount = 256;
    public event Action<Vector3> CoinPicked;
    private readonly Stack<RunnerPooledCoin> available = new Stack<RunnerPooledCoin>();
    private readonly Dictionary<RunnerPooledCoin, RunnerTrackSegment> active = new Dictionary<RunnerPooledCoin, RunnerTrackSegment>();
    private readonly List<RunnerPooledCoin> releaseBuffer = new List<RunnerPooledCoin>();
    private struct Pickup { public RunnerPooledCoin coin; public Vector3 start; public float elapsed; }
    private readonly List<Pickup> collecting = new List<Pickup>();
    // حركة جمع قصيرة تُجمّد عند الإيقاف، ثم تعاد العملة للمخزن بدل حذفها.
    private void Update()
    {
        if (session && session.State != RunSession.RunState.Playing) return;
        for (int i = collecting.Count - 1; i >= 0; i--)
        {
            var item = collecting[i]; item.elapsed += Time.deltaTime;
            if (!item.coin || item.elapsed >= .18f) { if(item.coin) Return(item.coin); collecting.RemoveAt(i); continue; }
            float t = item.elapsed / .18f;
            item.coin.transform.position = item.start + Vector3.up * (t * .8f);
            item.coin.transform.localScale = prefab.transform.localScale * (1f - t);
            collecting[i] = item;
        }
    }
    public int Collected { get; private set; }
    public int CreatedCount { get; private set; }
    public int ActiveCount => active.Count;
    public event Action<int> CoinCollected;

    public void ResetCounter() { if(Application.isPlaying && prefab) while(CreatedCount < prewarmCount) { var spare=CreateCoin(); spare.gameObject.SetActive(false); available.Push(spare); } Collected = 0; foreach (var item in collecting) if(item.coin) Return(item.coin); collecting.Clear(); releaseBuffer.Clear(); foreach (var coin in active.Keys) releaseBuffer.Add(coin); foreach (var coin in releaseBuffer) Return(coin); }
    public void Populate(RunnerTrackSegment segment)
    {
        if (!Application.isPlaying || !prefab) return;
        Release(segment);
        foreach (var row in segment.GetComponentsInChildren<RunnerCoinRow>())
        {
            if (!row.isActiveAndEnabled) continue;
            for (int i = 0; i < row.count; i++)
            {
                RunnerPooledCoin coin = available.Count > 0 ? available.Pop() : CreateCoin();
                coin.transform.SetParent(row.transform, false);
                coin.transform.localScale = prefab.transform.localScale;
                coin.transform.localPosition = row.Offset(i, player, world);
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
        if (session && session.State != RunSession.RunState.Playing) return;
        if (!active.ContainsKey(coin)) return;
        Vector3 point = coin.transform.position;
        active.Remove(coin);
        coin.transform.SetParent(transform, true);
        collecting.Add(new Pickup { coin = coin, start = point });
        CoinPicked?.Invoke(point);
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
