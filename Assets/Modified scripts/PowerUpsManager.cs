using System;
using System.Collections.Generic;
using UnityEngine;



[Serializable]
public class LootEntry
{
    public GameObject item;
    [Min(0f)] public float weight = 1f;
}

public class PowerUpsManager : MonoBehaviour
{
    [Min(0f)] public float nothing = 1f;
    [Min(0f)] public float powerUps = 1f;

    public List<LootEntry> entries = new List<LootEntry>();
    public GameObject ActiveObject;

    private void Start()
    {
        PopulatePowerUps();
    }

    public void ActivatePowerUp()
    {
        PopulatePowerUps();
    }

    public void DeActivatePowerUp()
    {
        Destroy(ActiveObject);
    }

    public void PopulatePowerUps()
    {
        if (CheckPopulation())
        {
            ActiveObject = Instantiate(PickPowerUp(), transform.position, Quaternion.identity);
            Debug.Log($"Picked Power UP {ActiveObject.name}");
        }
        else
        {

        }
    }

    public bool CheckPopulation()
    {
        float total = nothing + powerUps;
        if (total <= 0f) return false;

        float roll = UnityEngine.Random.Range(0f, total);
        Debug.Log($"[CheckPopulation] Picking Power UP {roll < powerUps}");
        return roll < powerUps;
    }

    public GameObject PickPowerUp()
    {

        float total = 0f;
        foreach (var e in entries)
            total += e.weight;

        if (total <= 0f) return null;

        float roll = UnityEngine.Random.Range(0f, total);

        foreach (var e in entries)
        {
            roll -= e.weight;
            if (roll <= 0f && e.weight > 0f)
                return e.item;
        }

        return null;

    }
}
