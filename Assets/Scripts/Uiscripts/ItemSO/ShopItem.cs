using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data.Common;
using Unity.VisualScripting;

public class ShopItem : MonoBehaviour
{
     public List<Item> items=new();
 
    [Header("Items")]
    

    [Header("UI")]
    [SerializeField] private List<Button> buyButtons;
    [SerializeField] private TMP_Text[] priceText;
  


     private GameData data;
     public ShopItem instance;
   
   void Start()
    {
        data = ManagerGame.instance.gamedata;
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
       for(int i=0; i<items.Count; i++)
        {
            if(buyButtons[i]!=null)
            {
                Item item =items[i];

             buyButtons[i].onClick.RemoveAllListeners();
             buyButtons[i].onClick.AddListener(()=>BuyItem(item));
              
            }

        }
    }

  public  void BuyItem(Item item)
    {
        
       
      
        if(item!=null)
        {
            if(coins.instance.numofcoins>=item.itemPrice)
            {
                Inventory.instance.additem( item);
                coins.instance.SpendCoins(item.itemPrice);
            }
        }
   
      
        
        


           
    }
  
}
