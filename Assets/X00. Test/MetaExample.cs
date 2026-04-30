using System.IO;
using UnityEngine;

public class MetaExample : MonoBehaviour
{
    public static MetaExample Instance;

    public MetaProgressData Data { get; private set; }

    private string savePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "meta_progress.json");
        Load();

        OnRunEnded(100, 3, true);
    }

    public void AddGold(int amount)
    {
        Data.totalGold += amount;
        Save();
    }

    public void TrySetHighestFloor(int floor)
    {
        if (floor > Data.highestFloor)
        {
            Data.highestFloor = floor;
            Save();
        }
    }

    public void UnlockReward(string rewardId)
    {
        if (!Data.unlockedRewards.Contains(rewardId))
        {
            Data.unlockedRewards.Add(rewardId);
            Save();
        }
    }

    public bool IsUnlocked(string rewardId)
    {
        return Data.unlockedRewards.Contains(rewardId);
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"메타 진행 저장 완료: {savePath}");
    }

    public void Load()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            Data = JsonUtility.FromJson<MetaProgressData>(json);
            Debug.Log("메타 진행 불러오기 완료");
        }
        else
        {
            Data = new MetaProgressData();
            Save();
            Debug.Log("새 메타 진행 데이터 생성");
        }
    }

    public void ResetMetaData()
    {
        Data = new MetaProgressData();
        Save();
        Debug.Log("메타 진행 초기화");
    }

    public void OnRunEnded(int earnedGold, int reachedFloor, bool unlockedNewCard)
    {
        Instance.AddGold(earnedGold);
        Instance.TrySetHighestFloor(reachedFloor);

        if (unlockedNewCard)
        {
            Instance.UnlockReward("Card_Fireball");
        }
    }
}