using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName="New Item", menuName="Shop/Item")]
public class Item : ScriptableObject

{
public string itemName;
 [SerializeField] private string itemID;
   [SerializeField] private Sprite itemImage;
        [SerializeField] private GameObject itemPrefab;
      public  int itemPrice;
}
