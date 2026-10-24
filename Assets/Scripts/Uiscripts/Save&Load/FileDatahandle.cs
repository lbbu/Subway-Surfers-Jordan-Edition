
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class FileDatahandle : MonoBehaviour
{
public string dataDirPath = "";
    private string dataFileName = "";

    public FileDatahandle(string dataDirPath, string dataFileName)
    {
        this.dataDirPath = dataDirPath;
        this.dataFileName = dataFileName;
    }

    public GameData Load()
    {
        string fullpath = Path.Combine(dataDirPath, dataFileName);
        GameData LoadData = null;

        if (File.Exists(fullpath))
        {
            try
            {
                string dataToLoad = "";

                using (FileStream stream = new FileStream(fullpath, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        dataToLoad = reader.ReadToEnd();
                    }

                    LoadData = JsonUtility.FromJson<GameData>(dataToLoad);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error occured when trying to load data from file: "
                    + fullpath + "\n" + e);
            }
        }

        return LoadData;
    }

    public void Save(GameData data)
    {
        string fullpath = Path.Combine(dataDirPath, dataFileName);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullpath));

            string dataToStore = JsonUtility.ToJson(data, true);

            using (FileStream stream = new FileStream(fullpath, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(dataToStore);
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error occured when trying to save data file: "
                + fullpath + "\n" + e);
        }
    }
}

