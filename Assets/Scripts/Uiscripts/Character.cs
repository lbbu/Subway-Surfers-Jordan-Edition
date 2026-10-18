

using UnityEngine;

public class Character : MonoBehaviour
{
  
    [SerializeField] private bool[] unlockedCharacters;
    [SerializeField] private TMPro.TMP_Text priceText;
    [SerializeField] private int[] characterPrices;

    [SerializeField] private GameObject selectButton;
    [SerializeField] private GameObject buyButton;
  [SerializeField] private string[] characterNames;
[SerializeField] private Transform characterContainer;
    private GameObject currentCharacterobj;


    [SerializeField] private coins playerCoins;

public int currentCharacter = 0;

    private void Start()
    {
        ShowCharacter();
    }
void spawnercharacter()
    {
        if(currentCharacterobj!=null)
        {
            Destroy(currentCharacterobj);
        }
      GameObject prefab = Resources.Load<GameObject>(
        "Charactermodels/" + characterNames[currentCharacter]
    );
      if(prefab!=null)
        {
    currentCharacterobj = Instantiate(prefab, characterContainer);
        }
        else
        {
            Debug.LogError("Prefab not found for character: " + characterNames[currentCharacter]);
        }
    }
    public void ChangeCharacter(int num)
    {
        currentCharacter += num;

        if (currentCharacter >=characterNames.Length)
            currentCharacter = 0;

        if (currentCharacter < 0)
            currentCharacter = characterNames.Length - 1;

        ShowCharacter();
    }

   public void leftbutton()
    {
        ChangeCharacter(-1);
    }

  
public void ShowCharacter()
{
     spawnercharacter();
  
    bool isUnlocked = unlockedCharacters[currentCharacter];

    selectButton.SetActive(isUnlocked);
    buyButton.SetActive(!isUnlocked);

    priceText.text = characterPrices[currentCharacter].ToString();
}
public void SelectCharacter(int index)//اختيار
    {
        if(index < 0 || index >= characterNames.Length)
        {
          
            return;
        }
        currentCharacter = index;
        ShowCharacter();
    }

    public void BuyCharacter()
    {
        if (unlockedCharacters[currentCharacter])
            return;

        int price = characterPrices[currentCharacter];

        if (playerCoins.SpendCoins(price))
        {
            unlockedCharacters[currentCharacter] = true;

            ShowCharacter();

            Debug.Log("Character purchased!");
        }
        else
        {
            Debug.Log("Not enough coins!");
        }
    }
}
