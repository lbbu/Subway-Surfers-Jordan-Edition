using UnityEngine;

public class CoinModified : MonoBehaviour
{
  
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            coins.instance.addcoin();
            ChunkManager.instance.ReturnCoin(gameObject);

        
            
        }
    }
}
