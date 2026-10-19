
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ManagerGame : MonoBehaviour
{
    [Header("File storage config")]
    [SerializeField] private string fileName;//مكان تخزين ملف الحفض

    private GameData gamedata;

    private List<IGameData> datapresistanceObject;
    private FileDatahandle datahandler;

    public static ManagerGame instance { get; private set; }

    private void Awake()
    {
        if (instance != null)
        {
            Debug.Log("Found more than one ManagerGame!");
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        this.datahandler = new FileDatahandle(
            Application.persistentDataPath,
            fileName
        );

        this.datapresistanceObject = FindAllDataPersistenceData();//مكان الحفض

        LoadSystem();
    }

    public void NewGame()
    {
        Debug.Log("New game initialized");
        this.gamedata = new GameData();
      
        
        
    }

    public void SaveSystem()
    {
        foreach (IGameData datapersistanceObj in datapresistanceObject)
        {
            datapersistanceObj.SaveData(ref gamedata);
        }

        Debug.Log("Saved coins count = " + gamedata.coins);

        datahandler.Save(gamedata);
    }

    public void LoadSystem()
    {
        this.gamedata = datahandler.Load();

        if (this.gamedata == null)
        {
            NewGame();
        }

        foreach (IGameData datapersistanceObj in datapresistanceObject)
        {
            datapersistanceObj.LoadData(gamedata);
        }

        Debug.Log("Loaded coin count = " + gamedata.coins);
        Debug.Log("Loaded data from: " + datahandler.dataDirPath );
    }

    private List<IGameData> FindAllDataPersistenceData()
    {
        IEnumerable<IGameData> datapresistanceObject =
            FindObjectsOfType<MonoBehaviour>().OfType<IGameData>();

        return new List<IGameData>(datapresistanceObject);
    }
}

