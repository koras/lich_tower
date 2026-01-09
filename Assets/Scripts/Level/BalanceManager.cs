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

        private BalanceConfigRoot _root;

        private string PersistentPath => Path.Combine(Application.persistentDataPath, FileName);
        private string StreamingPath => Path.Combine(Application.streamingAssetsPath, FileName);

        private void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log($"[BalanceManager] persistent: {PersistentPath}");
            Debug.Log($"[BalanceManager] streaming:  {StreamingPath}");

            EnsureConfigExists();
            Load();
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