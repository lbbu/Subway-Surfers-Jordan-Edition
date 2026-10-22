using UnityEngine;

public class CoinModified : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ChunkManager.instance.ReturnCoin(gameObject);
        }
    }
}
