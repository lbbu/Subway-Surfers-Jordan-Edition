using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ChunkParent : MonoBehaviour
{
    public List<CoinStartPos> coinsPos = new();
    public void BuildTheCoinStartPositions()
    {
        coinsPos.Clear();
        CoinStartPos[] coins = GetComponentsInChildren<CoinStartPos>();
        coinsPos = coins.ToList();
    }

    public void HideChunk()
    {
        for(int i = 0; i < coinsPos.Count; i++)
        {
            coinsPos[i].ReturnCoins();
        }
    }

    public void ActivateChunk()
    {
        for (int i = 0; i < coinsPos.Count; i++)
        {
            coinsPos[i].SpawnCoins();
        }
    }
}
