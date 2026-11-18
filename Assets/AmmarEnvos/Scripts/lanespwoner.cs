using System.Collections.Generic;
using UnityEngine;
public class lanespwoner : MonoBehaviour
{
    public List<GameObject> prefabs = new();
    public Transform parent;
    public List<float> lanes = new();
    public Vector2 zOffset;

    void Start()
    {
        methood();
    }

    void Update()
    {


    }


    void methood()
    {

        for (int i = 0; i < lanes.Count; i++)
        {
            float zPos = Random.Range(zOffset.x, zOffset.y);
            int index = Random.Range(0, prefabs.Count);
            Debug.Log($"prefabs {prefabs[index].name}");
            
            GameObject  gameobject = Instantiate(prefabs[index], parent);
            gameobject.transform.localPosition = new Vector3(lanes[i], 0,zPos);    

           

        }

    }

}
