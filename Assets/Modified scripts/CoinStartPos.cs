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
    public float archLength = 8f;
    public float archHeight = 3f;

    private void Start()
    {
        SpawnCoins();
    }

    public void SpawnCoins()
    {
        ChunkManager.instance.RentCoins(transform, coinStyle, coinCount, archLength, archHeight);
    }
}