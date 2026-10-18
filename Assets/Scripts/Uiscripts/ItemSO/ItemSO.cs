using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item")]
public class ItemSO : ScriptableObject
{
    public string itemName;
   [TextArea] public string itemDescription;
    public Sprite itemIcon;
    public bool isGold;
}