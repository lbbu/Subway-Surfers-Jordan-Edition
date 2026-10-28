using TMPro;
using UnityEngine;

public class ScoreManagerModified : MonoBehaviour
{

    public static ScoreManagerModified instance;

    public int Low; // 999 999
    public int High; // 1 000 000 = 1

    const int Base = 1000000; //للقسمة 

    public int HighScoreHigh;
    public int HighScoreLow;

    public float scoreModifier = 1.0f;


    [Header("UI-Elements")]
    public TextMeshProUGUI highestScore;
    public TextMeshProUGUI currentScore;

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
    void Start()
    {
        HighScoreHigh = PlayerPrefs.GetInt("HighScoreHigh", 0);
        HighScoreLow = PlayerPrefs.GetInt("HighScoreLow", 0);

        if( HighScoreHigh > 0 ||  HighScoreLow > 0)
        {
            highestScore.text = DisplayHighestScore();
        }
        else
        {
            highestScore.gameObject.SetActive(false);
        }
    }

    public void NewGame()
    {
        Low = 0;
        High = 0;
    }

    public void UpdateScore(int amount)
    {
        int scoreAddition = Mathf.RoundToInt(amount * scoreModifier);
        Debug.Log($" Score Manager{scoreAddition}");
        AddScore(scoreAddition);
        UpdateScoreTexts();
        CheckHighScore();
    }

    public void AddScore(int amount)
    {
        Low += amount;
        High += Low / Base;
        Low %= Base;
    }

    public void CheckHighScore()
    {
        bool isHigher = High > HighScoreHigh ||
                       (High == HighScoreHigh && Low > HighScoreLow);

        if (isHigher)
        {
            HighScoreHigh = High;
            HighScoreLow = Low;
            PlayerPrefs.SetInt("HighScoreHigh", HighScoreHigh);
            PlayerPrefs.SetInt("HighScoreLow", HighScoreLow);
            PlayerPrefs.Save();
            highestScore.gameObject.SetActive(false);

            // Add score beaten effect
        }
    }

    public void UpdateScoreTexts()
    {
        currentScore.text = Display();
    }

    // D6 for (high = 3 and low = 1222) it makes the shown value = (3 001 222) rather than (31222)
    public string Display() =>
    High > 0 ? $"{High}{Low:D6}" : Low.ToString(); 

    public string DisplayHighestScore() => 
        HighScoreHigh > 0 ? $"{HighScoreHigh}{HighScoreLow:D6}" : HighScoreLow.ToString();
}
