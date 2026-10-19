using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItem : MonoBehaviour
{
  
 
    [Header("Items")]
    [SerializeField] private List<Item> items = new List<Item>();

    [Header("UI")]
    [SerializeField] private Button[] buyButtons;
    [SerializeField] private TMP_Text[] priceText;

     coins instance_coins;
   
   void Start()
    {
         ShowPrices();
        SetupButtons();
    }

    private void ShowPrices()
    {
        for (int i = 0; i < items.Count && i < priceText.Length; i++)
        {
            priceText[i].text = items[i].itemPrice.ToString();
        }
    }

    private void SetupButtons()
    {
        for (int i = 0; i < buyButtons.Length && i < items.Count; i++)
        {
            int itemID = i;

            buyButtons[i].onClick.AddListener(
                () => BuyItem(itemID)
            );
        }
    }

  public  void BuyItem(int itemID)
    {
       if(itemID < 0 || itemID >= items.Count)
        {
       return;
       
        }
      Item selectedItem = items[itemID];
      
        
        


Debug.Log("Item Bought: " + selectedItem.itemName
    + " for " + selectedItem.itemPrice);
           
    
    
       void SaveData(ref GameData data)
       

    {
        
    }
        void LoadData(GameData data)
    {
        
    }

}
}
