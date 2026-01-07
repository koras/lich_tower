
using System.IO;
using UnityEngine;

namespace Player
{
    public class SaveManager
    {
        private const string FileName = "save.json";

        private static string GetPath()
        {
            return Path.Combine(Application.persistentDataPath, FileName);
        }

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true); // pretty-print, чтобы человеку приятно было
                string path = GetPath();

                File.WriteAllText(path, json);
             //   Debug.Log($"[SaveManager] Saved to: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Save error: {e}");
            }
        }

        public static bool TryLoad(out SaveData data)
        {
            string path = GetPath();

            if (!File.Exists(path))
            {
                Debug.Log("[SaveManager] No save file, using defaults");
                data = null;
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<SaveData>(json);

                if (data == null)
                {
                    Debug.LogWarning("[SaveManager] Failed to parse save file, data is null");
                    return false;
                }

                Debug.Log($"[SaveManager] Loaded from: {path}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Load error: {e}");
                data = null;
                return false;
            }
        }

        public static void DeleteSave()
        {
            string path = GetPath();

            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log("[SaveManager] Save deleted");
            }
        }
    }
}