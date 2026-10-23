using System;
using UnityEngine;

/// <summary>نتيجة الجولة تعتمد على المسافة الفعلية والعملات، والحفظ يحدث عند نهاية الجولة فقط.</summary>
public sealed class RunScore : MonoBehaviour
{
    [SerializeField] private RunSession session;
    [SerializeField] private RunnerTrackWorld world;
    [SerializeField] private RunnerCoinPool coins;
    [SerializeField, Min(0)] private int pointsPerMetre = 1;
    [SerializeField, Min(0)] private int pointsPerCoin = 10;
    [SerializeField] private string saveKey = "JordanRunner.M7MD.BestScore";
    public double Distance => world ? world.DistanceTravelled : 0;
    public int Coins => coins ? coins.Collected : 0;
    public int Points => (int)Math.Min(int.MaxValue, Math.Floor(Distance * pointsPerMetre) + (double)Coins * pointsPerCoin);
    public int Best { get; private set; }
    private bool bound;
    private void OnEnable() => Initialize();
    private void OnDisable() { if (bound && session) session.RunEnded -= SaveResult; bound = false; }
    public void Initialize()
    {
        Best = PlayerPrefs.GetInt(saveKey, 0);
        if (!bound && session) { session.RunEnded += SaveResult; bound = true; }
    }
    private void SaveResult(string reason)
    {
        if (Points <= Best) return;
        Best = Points; PlayerPrefs.SetInt(saveKey, Best); PlayerPrefs.Save();
    }
}
