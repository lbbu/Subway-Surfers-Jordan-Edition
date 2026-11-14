using System.Collections.Generic;
using UnityEngine;
public class lanespwoner : MonoBehaviour
{
    public List<GameObject> prefabs = new();
    public Transform parent;

    void Start()
    {
        int index = Random.Range(0, prefabs.Count);

        Debug.Log($"prefabs {prefabs[index].name}");

        Instantiate(prefabs[index], parent);
    }





    void Update()
    {


    }
}
