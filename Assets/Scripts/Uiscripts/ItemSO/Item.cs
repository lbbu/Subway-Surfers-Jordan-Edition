using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName="New Item", menuName="Shop/Item")]
public class Item : ScriptableObject

{

public string itemName;
public string itemIDName;
public Sprite itemImage;
     public GameObject itemPrefab;
      public  int itemPrice;
}
