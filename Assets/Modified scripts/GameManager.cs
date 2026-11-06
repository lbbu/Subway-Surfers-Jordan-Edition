using Newtonsoft.Json.Bson;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;


    public GameObject deathMenu;
    public GameObject mainMenu;




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


    public void ShowDeathMenu()
    {
        deathMenu.SetActive(true);
        HideMainMenu();
        Debug.Log($"[ShowDeathMenu] DeathMenu set active to {deathMenu.activeSelf}");
    }

    public void ShowMainMenu()
    {
        mainMenu.SetActive(true);
        HideDeathMenu();
    }

    public void HideDeathMenu()
    {
        deathMenu.SetActive(false);
        Debug.Log($"DeathMenu set active to {deathMenu.activeSelf}");
    }

    public void HideMainMenu()
    {
        mainMenu.SetActive(false);
    }
}
