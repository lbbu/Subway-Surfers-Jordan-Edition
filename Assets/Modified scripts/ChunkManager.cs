using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            PopulateCoinPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public List<GameObject> Chunks = new();
    public List<GameObject> loadedChunks = new();
    public GameObject safeChunk;
    public int activeChunk = 5;
    public float chunksSpeed = 10.0f;
    public float accelarate = 0.1f;
    public float chunkOffset = 30f;
    private void Start()
    {
        if(loadedChunks.Count <= 0)
        {
            LoadStartingChunks();
        }
    }

    private void LoadStartingChunks()
    {
        for (int i = 0; i < activeChunk + 1; i++)
        {
            int indx = Random.Range(0, Chunks.Count - 1);
            Debug.Log($"{indx}");
            GameObject obj = Chunks[indx];
            if(loadedChunks.Count > 0)
            {
                Vector3 lastActiveChPos = loadedChunks[loadedChunks.Count - 1].transform.position;
                loadedChunks.Add(obj);
                obj.SetActive(true);
                obj.transform.position = new Vector3(lastActiveChPos.x, lastActiveChPos.y, lastActiveChPos.z + chunkOffset);
                Chunks.RemoveAt(indx);
            }
            else
            {
                loadedChunks.Add(obj);
                obj.SetActive(true);
                obj.transform.position = new Vector3(0, 0, 30);
                Chunks.RemoveAt(indx);                
            }            
        }
    }

    private void FixedUpdate()
    {
        chunksSpeed += accelarate * Time.deltaTime;
        float movement = chunksSpeed * Time.fixedDeltaTime;

        Debug.Log($" Chunk Add to score {movement}");
        ScoreManagerModified.instance.UpdateScore(Mathf.RoundToInt(movement * 10));

        if (loadedChunks.Count < activeChunk + 1)
        {
            if (Chunks.Count <= 0) return;

            int indx = Random.Range(0, Chunks.Count - 1);
            Debug.Log($"{indx}");
            GameObject obj = Chunks[indx];
            
            Vector3 lastActiveChPos = loadedChunks[loadedChunks.Count - 1].transform.position;
            loadedChunks.Add(obj);
            obj.SetActive(true);
            obj.transform.position = new Vector3 (lastActiveChPos.x, lastActiveChPos.y, lastActiveChPos.z + chunkOffset);
            Chunks.RemoveAt(indx);
        }
        if(safeChunk.activeSelf && safeChunk != null)
        {
            Vector3 pos = safeChunk.transform.position;
            pos.z -= movement;
            safeChunk.transform.position = pos;
            if (pos.z < -60)
            {
               safeChunk.SetActive(false);
            }
        }
        for (int i = 0; i < loadedChunks.Count; ++i)
        {
            Vector3 pos = loadedChunks[i].transform.position;
            pos.z -= movement;
            loadedChunks[i].transform.position = pos;
            if (pos.z < -60)
            {
                GameObject obj = loadedChunks[i];
                loadedChunks.RemoveAt(i);
                obj.SetActive(false);
                Chunks.Add(obj);
            }
        }
    }

    #region CreateCoinsForChunks
    public GameObject coinPrefab;
    public int initialPoolSize = 100;
    public float coinSpacing = 1.5f;

    public List<GameObject> coinPool = new();
    public List<GameObject> activeCoins = new();

    private Dictionary<Transform, List<GameObject>> rentedBy = new();
    private Dictionary<GameObject, Transform> coinOwner = new();
    private Transform coinParent;

    public void PopulateCoinPool()
    {
        if (coinParent == null)
        {
            coinParent = new GameObject("CoinPool").transform;
            coinParent.SetParent(transform);
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject coin = Instantiate(coinPrefab, coinParent);
            coin.SetActive(false);
            coinPool.Add(coin);
        }
    }

    public void RentCoins(Transform owner, CoinStyle style, int count, float archLength, float archHeight)
    {
        if (rentedBy.ContainsKey(owner))
            ReturnCoins(owner);

        List<GameObject> rented = new(count);

        for (int i = 0; i < count; i++)
        {
            GameObject coin = GetCoinFromPool();
            coin.transform.SetParent(owner, false);
            coin.transform.localPosition = GetOffset(style, i, count, archLength, archHeight);
            coin.SetActive(true);

            activeCoins.Add(coin);
            rented.Add(coin);
            coinOwner[coin] = owner;
        }

        rentedBy[owner] = rented;
    }

    public void ReturnCoins(Transform owner)
    {
        if (!rentedBy.TryGetValue(owner, out List<GameObject> rented))
            return;

        foreach (GameObject coin in rented)
        {
            coin.SetActive(false);
            coin.transform.SetParent(coinParent, false);
            activeCoins.Remove(coin);
            coinOwner.Remove(coin);
            coinPool.Add(coin);
        }

        rentedBy.Remove(owner);
    }
    public void ReturnCoin(GameObject coin)
    {
        if (!coinOwner.TryGetValue(coin, out Transform owner))
            return;

        if (rentedBy.TryGetValue(owner, out List<GameObject> group))
        {
            group.Remove(coin);
            if (group.Count == 0)
                rentedBy.Remove(owner);
        }

        coinOwner.Remove(coin);
        activeCoins.Remove(coin);

        coin.SetActive(false);
        coin.transform.SetParent(coinParent, false);
        coinPool.Add(coin);
    }
    private GameObject GetCoinFromPool()
    {
        if (coinPool.Count > 0)
        {
            int last = coinPool.Count - 1;
            GameObject coin = coinPool[last];
            coinPool.RemoveAt(last);
            return coin;
        }
        return Instantiate(coinPrefab, coinParent);
    }

    private Vector3 GetOffset(CoinStyle style, int index, int count, float archLength, float archHeight)
    {
        switch (style)
        {
            case CoinStyle.Arch:
                {
                    
                    float t = count > 1 ? index / (float)(count - 1) : 0f;
                    float z = t * archLength;
                    float y = Mathf.Sin(t * Mathf.PI) * archHeight;
                    return new Vector3(0f, y, z);
                }
            default: 
                {
                   
                    float z = index * coinSpacing;
                    return new Vector3(0f, 0f, z);
                }
        }
    }
    #endregion
}
