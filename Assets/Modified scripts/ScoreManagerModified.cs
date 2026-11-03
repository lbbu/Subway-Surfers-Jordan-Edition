using TMPro;
using UnityEngine;
using DG.Tweening;

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
        startPos = bonusScoreText.rectTransform.anchoredPosition;
        bonusScoreText.alpha = 0f;
        addBonusText.alpha = 0f;
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
        //Debug.Log($" Score Manager{scoreAddition}");
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




    #region Coins && Challanges

    public TextMeshProUGUI bonusScoreText;
    public TextMeshProUGUI addBonusText;

    private Vector3 startPos;

    public int bonusScore = 0;
    public float holdTimer = 3f; // Add item that makes it hold longer and add one that opens
                                 // challanges like dodge the busses at last second score = distanceToTarget

    private int displayedScore = 0; // Movement to the numbers

    public float countSpeed = 400f;
    private Tween countTween;
    private Sequence hideSeq;


    public void AddBonusScore(int amount)
    {
        if (amount <= 0) return;

        bonusScore += amount;

        var rt = bonusScoreText.rectTransform;
        bool wasHidden = bonusScoreText.alpha <= 0.01f;


        countTween?.Kill();
        hideSeq?.Kill();


        rt.DOKill();
        bonusScoreText.DOKill();
        rt.anchoredPosition = startPos;
        bonusScoreText.alpha = 1f;

        if (wasHidden)
        {
            rt.localScale = Vector3.zero;
            rt.DOScale(2f, 0.2f).SetEase(Ease.OutBack)
              .OnComplete(() => rt.DOScale(1f, 0.1f))
              .SetLink(gameObject);
        }
        else
        {
            rt.localScale = Vector3.one;
            rt.DOPunchScale(Vector3.one * 1.3f, 0.2f).SetLink(gameObject);
        }

        float duration = Mathf.Max(0.2f, (bonusScore - displayedScore) / countSpeed);

        countTween = DOVirtual.Int(displayedScore, bonusScore, duration, v =>
        {
            displayedScore = v;
            bonusScoreText.text = v.ToString("N0");
        })
        .SetEase(Ease.Linear)
        .SetLink(gameObject)
        .OnComplete(StartHide);
    }

    private void StartHide()
    {
        var rt = bonusScoreText.rectTransform;

        hideSeq = DOTween.Sequence()
            .AppendInterval(holdTimer)
            .Append(bonusScoreText.DOFade(0f, 0f))
            .OnComplete(() =>
            {
                AddToCurrentScore(bonusScore);
                bonusScore = 0;
                displayedScore = 0;
            })
            .SetLink(gameObject);
    }

    Sequence moveBonusTween;
    public void AddToCurrentScore(int bonus)
    {
        addBonusText.rectTransform.position = bonusScoreText.rectTransform.position;
        addBonusText.alpha = 1f;
        addBonusText.text = bonus.ToString();
        moveBonusTween = DOTween.Sequence();

        moveBonusTween.Append(addBonusText.rectTransform.DOMove(currentScore.rectTransform.position, 0.1f))
           .AppendCallback(() => AddScore(bonus))                          
           .Append(currentScore.rectTransform.DOScale(1.2f, 0.2f))
           .Append(currentScore.rectTransform.DOScale(1f, 0.1f))           
           .OnComplete(() => addBonusText.alpha = 0f)                      
           .SetLink(gameObject);

    }
    #endregion
}
