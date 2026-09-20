using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    public int coinValue = 1;
    public int scoreValue = 10;
    public float rotationSpeed = 100f;

    [Header("Audio Settings")]
    public AudioClip collectSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    private void Update()
    {
        // دوران الكوين لإعطاء مظهر حركي
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // التأكد من أن الذي اصطدم بالكوين هو اللاعب
        if (other.CompareTag("Player"))
        {
            // 1. تشغيل الصوت في مكان الكوين قبل اختفائها
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
            }

            // 2. تحديث السكور والكوينز في الـ ScoreManager
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddCoin(coinValue, scoreValue);
            }

            // 3. تدمير الكوين
            Destroy(gameObject);
        }
    }
}