using System.IO;
using UnityEngine;

public class FileDataHandler<T> where T : class
{
    private string dataDirPath;
    private string dataFileName;

    private string FullPath => Path.Combine(dataDirPath, dataFileName);

    public FileDataHandler(string dataDirPath, string dataFileName)
    {
        this.dataDirPath  = dataDirPath;
        this.dataFileName = dataFileName;
    }

    public T Load()
    {
        if (!File.Exists(FullPath))
        {
            Debug.Log($"[FileDataHandler] No save file found, using defaults: {FullPath}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(FullPath);
            return JsonUtility.FromJson<T>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FileDataHandler] Load failed: {ex.Message}");
            return null;
        }
    }

    public void Save(T data)
    {
        if (data == null) return;

        try
        {
            Directory.CreateDirectory(dataDirPath);
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(FullPath, json);
            Debug.Log($"[FileDataHandler] Saved → {FullPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FileDataHandler] Save failed: {ex.Message}");
        }
    }
}
