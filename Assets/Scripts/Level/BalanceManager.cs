using System;
using System.IO;
using UnityEngine;
using Heroes;
using Level;


namespace Level
{
    public class BalanceManager : MonoBehaviour
    {
        public static BalanceManager I { get; private set; }

        private const string FileName = "balance.json";
        public string GetPersistentPath() => PersistentPath;
        public bool IsLoaded => _root != null && _root.difficulties != null;
        private BalanceConfigRoot _root;

        private string PersistentPath => Path.Combine(Application.persistentDataPath, FileName);
        private string StreamingPath => Path.Combine(Application.streamingAssetsPath, FileName);

        private void Awake()
        {
            if (I != null)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log($"[BalanceManager] persistent: {PersistentPath}");
            Debug.Log($"[BalanceManager] streaming:  {StreamingPath}");

            EnsureConfigExists();
            Load();
        }

        public bool TryGetBalanceRaw(HeroesBase.Hero hero, GameDifficulty difficulty, out HeroBalance hb)
        {
            hb = null;
            if (_root?.difficulties == null) return false;

            string diffStr = difficulty.ToString();
            string heroStr = hero.ToString();

            foreach (var block in _root.difficulties)
            {
                if (block == null || block.difficulty != diffStr) continue;
                if (block.heroes == null) continue;

                foreach (var item in block.heroes)
                {
                    if (item == null) continue;
                    if (item.hero == heroStr)
                    {
                        hb = item;
                        return true;
                    }
                }
            }

            return false;
        }

        public void SetHeroBalance(HeroesBase.Hero hero, GameDifficulty difficulty, HeroBalanceData data)
        {
            if (_root == null)
                _root = new BalanceConfigRoot { difficulties = new System.Collections.Generic.List<DifficultyBlock>() };

            string diffStr = difficulty.ToString();
            string heroStr = hero.ToString();

            // найти или создать блок сложности
            DifficultyBlock block = null;
            foreach (var b in _root.difficulties)
            {
                if (b != null && b.difficulty == diffStr)
                {
                    block = b;
                    break;
                }
            }

            if (block == null)
            {
                block = new DifficultyBlock
                    { difficulty = diffStr, heroes = new System.Collections.Generic.List<HeroBalance>() };
                _root.difficulties.Add(block);
            }

            if (block.heroes == null) block.heroes = new System.Collections.Generic.List<HeroBalance>();

            // найти или создать героя
            HeroBalance hb = null;
            foreach (var h in block.heroes)
            {
                if (h != null && h.hero == heroStr)
                {
                    hb = h;
                    break;
                }
            }

            if (hb == null)
            {
                hb = new HeroBalance { hero = heroStr };
                block.heroes.Add(hb);
            }

            hb.maxHp = data.MaxHp;
            hb.maxMana = data.MaxMana;
            hb.xpReward = data.XpReward;
            hb.damage = data.Damage;
            hb.cost = data.Cost;
        }


        public void SaveToDisk()
        {
            try
            {
                if (_root == null)
                {
                    Debug.LogWarning("[BalanceManager] Nothing to save (root is null).");
                    return;
                }

                string json = JsonUtility.ToJson(_root, true);
                File.WriteAllText(PersistentPath, json);
                Debug.Log($"[BalanceManager] Saved balance to: {PersistentPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BalanceManager] Save error: {e}");
            }
        }

        public void ReloadFromDisk()
        {
            Load();
        }

        public void ResetToDefaultFromStreaming()
        {
            try
            {
                if (!File.Exists(StreamingPath))
                {
                    Debug.LogWarning($"[BalanceManager] No default in StreamingAssets: {StreamingPath}");
                    return;
                }

                File.Copy(StreamingPath, PersistentPath, overwrite: true);
                Debug.Log($"[BalanceManager] Reset balance from StreamingAssets to: {PersistentPath}");
                Load();
            }
            catch (Exception e)
            {
                Debug.LogError($"[BalanceManager] ResetToDefault error: {e}");
            }
        }

        private void EnsureConfigExists()
        {
            // Если в persistentDataPath нет balance.json, копируем дефолтный из StreamingAssets
            if (File.Exists(PersistentPath)) return;

            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
            // На Android StreamingAssets читается не как обычный файл (jar), нужен UnityWebRequest.
            // Чтобы не расписывать простыню, проще положить дефолт как TextAsset в Resources.
            Debug.LogWarning("[BalanceManager] Android: лучше положить дефолтный баланс в Resources и скопировать оттуда.");
#else
                if (File.Exists(StreamingPath))
                {
                    File.Copy(StreamingPath, PersistentPath);
                    Debug.Log($"[BalanceManager] balance.json copied to: {PersistentPath}");
                }
                else
                {
                    Debug.LogWarning($"[BalanceManager] No balance.json in StreamingAssets: {StreamingPath}");
                }
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[BalanceManager] EnsureConfigExists error: {e}");
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(PersistentPath))
                {
                    Debug.LogWarning("[BalanceManager] No balance.json found, using null config.");
                    _root = null;
                    return;
                }

                string json = File.ReadAllText(PersistentPath);
                _root = JsonUtility.FromJson<BalanceConfigRoot>(json);

                if (_root == null || _root.difficulties == null)
                    Debug.LogWarning("[BalanceManager] balance.json parsed but empty.");
                else
                    Debug.Log($"[BalanceManager] Loaded balance from: {PersistentPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BalanceManager] Load error: {e}");
                _root = null;
            }
        }

        public bool TryGetHeroBalance(HeroesBase.Hero hero, GameDifficulty difficulty, out HeroBalanceData data)
        {
            data = default;

            if (_root?.difficulties == null) return false;

            string diffStr = difficulty.ToString(); // "Easy"/"Normal"/"Hard"
            string heroStr = hero.ToString();

            foreach (var block in _root.difficulties)
            {
                if (block == null || block.difficulty != diffStr) continue;
                if (block.heroes == null) continue;

                foreach (var hb in block.heroes)
                {
                    if (hb == null) continue;
                    if (hb.hero != heroStr) continue;

                    data = new HeroBalanceData
                    {
                        MaxHp = hb.maxHp,
                        MaxMana = hb.maxMana,
                        XpReward = hb.xpReward,
                        Damage = hb.damage,
                        Cost = hb.cost
                    };
                    return true;
                }
            }

            return false;
        }
    }
}