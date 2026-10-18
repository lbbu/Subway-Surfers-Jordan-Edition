
using UnityEngine;
using TMPro;

public class coins : MonoBehaviour, IGameData
{
    private bool collected = false;

    [SerializeField] private TextMeshProUGUI coinText;
    private int numofcoins = 0;

    private void OnTriggerEnter(Collider combine)
    {
        

        if (!collected && combine.CompareTag("coins"))
        {
            ++numofcoins;
            coinText.text = numofcoins.ToString();
        }
    }

    public int GetCoins()
    {
        return numofcoins;
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || numofcoins < amount)
        {
            return false;
        }

        numofcoins -= amount;

        if (coinText != null)
        {
            coinText.text = numofcoins.ToString();
        }

        return true;
    }

    public void LoadData(GameData data)
    {
        numofcoins = data.coins;

        if (coinText != null)
        {
            coinText.text = numofcoins.ToString();
        }
    }

    public void SaveData(ref GameData data)
    {

        data.coins = numofcoins;
    }
}
