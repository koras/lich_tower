using System.IO;
using UnityEngine;
using Level;
namespace Config
{
    public static class HeroCostStorage
    {
        private const string FileName = "hero_costs.json";

        private static HeroCostData _data;
        private static bool _loaded;

        private static string GetPath()
        {
            return Path.Combine(Application.persistentDataPath, FileName);
        }

        public static void Load()
        {
            if (_loaded) return;

            string path = GetPath();

            if (!File.Exists(path))
            {
                Debug.Log("[HeroCostStorage] Файл не найден, создаю дефолтный конфиг");
                _data = CreateDefaultData();
                Save(); // сразу сохраним дефолты
                _loaded = true;
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                _data = JsonUtility.FromJson<HeroCostData>(json);

                if (_data == null)
                {
                    Debug.LogWarning("[HeroCostStorage] Не удалось распарсить hero_costs.json, создаю дефолтный");
                    _data = CreateDefaultData();
                    Save();
                }

                _loaded = true;
      //          Debug.Log($"[HeroCostStorage] Загружен конфиг из: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HeroCostStorage] Ошибка загрузки: {e}");
                _data = CreateDefaultData();
                Save();
                _loaded = true;
            }
        }

        public static void Save()
        {
            if (_data == null)
                _data = CreateDefaultData();

            try
            {
                string json = JsonUtility.ToJson(_data, true); // pretty-print
                string path = GetPath();
                File.WriteAllText(path, json);
                Debug.Log($"[HeroCostStorage] Сохранён конфиг в: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HeroCostStorage] Ошибка сохранения: {e}");
            }
        }

        private static HeroCostData CreateDefaultData()
        {
            var data = new HeroCostData();

            // Здесь задаёшь дефолтные цены
            data.Entries.Add(new HeroCostEntry { Hero = SpawnInHero.State.Skeleton,        Cost = 1 });
            data.Entries.Add(new HeroCostEntry { Hero = SpawnInHero.State.SkeletonArcher,  Cost = 2 });
            data.Entries.Add(new HeroCostEntry { Hero = SpawnInHero.State.GobArcher,       Cost = 3 });
            data.Entries.Add(new HeroCostEntry { Hero = SpawnInHero.State.OrcWar,          Cost = 4 });

            return data;
        }

        public static int GetCost(SpawnInHero.State hero)
        {
            Load();

            foreach (var e in _data.Entries)
            {
                if (e.Hero == hero)
                    return e.Cost;
            }

            Debug.LogWarning($"[HeroCostStorage] Нет записи для {hero}, возвращаю 0");
            return 0;
        }

        public static void SetCost(SpawnInHero.State hero, int cost)
        {
            Load();

            var entry = _data.Entries.Find(e => e.Hero == hero);
            if (entry == null)
            {
                entry = new HeroCostEntry { Hero = hero, Cost = cost };
                _data.Entries.Add(entry);
            }
            else
            {
                entry.Cost = cost;
            }

            Save();
        }

        public static HeroCostData GetRawData()
        {
            Load();
            return _data;
        }
    }
}