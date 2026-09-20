using UnityEngine;
using TMPro; // استخدم TextMeshPro للواجهة

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI Text Components")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI coinsText;

    private int score = 0;
    private int coins = 0;

    private void Awake()
    {
        // إعداد الـ Singleton لسهولة الوصول من أي كود آخر
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateUI();
    }

    // إضافة كوينز زيادة السكور مع كل كوين
    public void AddCoin(int coinValue, int scoreValue)
    {
        coins += coinValue;
        score += scoreValue;
        UpdateUI();
    }

    // إضافة سكور فقط (مثلاً مع الوقت أو تجنب العقبات)
    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
        if (coinsText != null) coinsText.text = "Coins: " + coins;
    }
}