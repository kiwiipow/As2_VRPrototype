using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class ScoreRecord
{
    public int score;
    public string date;
}

[Serializable]
internal class RecordsData
{
    public List<ScoreRecord> records = new List<ScoreRecord>();
}

public static class RecordsManager
{
    private const int MaxStoredRecords = 50;
    private const string FileName = "records.json";

    private static RecordsData data;

    public static event Action RecordsChanged;

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        data = null;
        RecordsChanged = null;
    }

    public static IReadOnlyList<ScoreRecord> Records
    {
        get
        {
            EnsureLoaded();
            return data.records;
        }
    }

    public static int AddRecord(int score)
    {
        EnsureLoaded();

        var newRecord = new ScoreRecord
        {
            score = score,
            date = DateTime.Now.ToString("yyyy-MM-dd")
        };

        int index = 0;
        while (index < data.records.Count && data.records[index].score >= score)
            index++;

        data.records.Insert(index, newRecord);

        if (data.records.Count > MaxStoredRecords)
            data.records.RemoveRange(MaxStoredRecords, data.records.Count - MaxStoredRecords);

        Save();
        RecordsChanged?.Invoke();

        return index < MaxStoredRecords ? index + 1 : 0;
    }

    public static void ClearAll()
    {
        data = new RecordsData();
        Save();
        RecordsChanged?.Invoke();
    }

    private static void EnsureLoaded()
    {
        if (data != null)
            return;

        data = new RecordsData();

        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                RecordsData loaded = JsonUtility.FromJson<RecordsData>(json);
                if (loaded != null && loaded.records != null)
                    data = loaded;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RecordsManager] Could not load records: {e.Message}");
        }
    }

    private static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data));
        }
        catch (Exception e)
        {
            Debug.LogError($"[RecordsManager] Could not save records: {e.Message}");
        }
    }
}
