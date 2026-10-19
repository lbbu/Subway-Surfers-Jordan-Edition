using UnityEngine;

public interface IGameData
{
    //شروط اي سكربت عنجه هذول فنكشن يشغله بشتغل من manager
    void LoadData(GameData data);
    void SaveData(ref GameData data);
}