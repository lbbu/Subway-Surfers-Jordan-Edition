
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class coins : MonoBehaviour, IGameData
{
    private bool collected = false;
    public static coins instance;

    [SerializeField] private TextMeshProUGUI coinText;
   public int numofcoins = 0;
   void Awake()
    {
        if(instance==null)
        {
            instance=this;
        }
    }
public void addcoin()
    {
        numofcoins++;
        Debug.Log($"coins={numofcoins}");

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
