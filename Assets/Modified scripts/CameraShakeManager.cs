using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager instance;
    public CinemachineCamera cam;
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPos = cam.GetComponent<CinemachineFollow>().FollowOffset;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cam.GetComponent<CinemachineFollow>().FollowOffset = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.GetComponent<CinemachineFollow>().FollowOffset = originalPos;
        // Death Cam
        // FollowOffset
        // Rotation
    }

    public void PlayBump(float duration = 0.25f, float magnitude = 0.15f, float frequency = 30f)
    {
        StartCoroutine(Shake(duration, magnitude));
    }

    public void DeathCamera()
    {
        cam.GetComponent<CinemachineFollowZoom>().Width = 3;
    }
}
