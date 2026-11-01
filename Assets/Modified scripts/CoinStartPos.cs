using System.Collections.Generic;
using UnityEngine;

public enum CoinStyle
{
    Straight,
    Arch
}

public class CoinStartPos : MonoBehaviour
{
    public CoinStyle coinStyle;
    public int coinCount = 5;

    public void SpawnCoins()
    {
        ChunkManager.instance.RentCoins(transform, coinStyle, coinCount);
    }

    public void ReturnCoins()
    {

        ChunkManager.instance.ReturnCoins(transform);
    }
}