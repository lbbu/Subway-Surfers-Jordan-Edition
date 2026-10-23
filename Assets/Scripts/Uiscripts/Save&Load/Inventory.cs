using UnityEngine;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using Unity.VisualScripting;

public class Inventory : MonoBehaviour
{
   public static Inventory instance;
  public List<Item> items=new();

      public Dictionary<string,Item> itemsown = new ();
  public Dictionary<string, int> itemCounts = new();


  private void Awake()
  {
   if(instance==null)
      {
       instance=this;
       DontDestroyOnLoad(this);
      }
  }
   

   
   public void additem(Item item)
   {

      if(!itemsown.ContainsKey(item.itemIDName))
      {
               itemsown[item.itemIDName]=item;
         items.Add(item);
         itemCounts[item.itemIDName] = 1;
        
   }
      else
      {
          itemCounts[item.itemIDName]++;
      }
   }
   
}
