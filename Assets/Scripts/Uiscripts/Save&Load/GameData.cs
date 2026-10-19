using UnityEngine;

[System.Serializable]
public class GameData
{
    public int coins; 

    public GameData()
    {
        Debug.Log("GameData constructor called");
        this.coins = 0;
    }
}