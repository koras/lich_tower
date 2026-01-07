using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Heroes;
using Level;


namespace Level
{
    public class SpawnManager : MonoBehaviour
    {
        public static SpawnManager Instance { get; private set; }

        [Header("Настройки спауна")] [SerializeField]
        private float initialSpawnDelay = 90f; // 1.5 минуты до первого спауна

        [SerializeField] private float spawnIntervalMin = 90f; // 1.5 минуты
        [SerializeField] private float spawnIntervalMax = 180f; // 3 минуты
        [SerializeField] private int minUnitsPerPlatform = 3;
        [SerializeField] private int maxUnitsPerPlatform = 3;

        [Header("Платформы")] 
        
        [SerializeField] private List<SpawnPlatform> spawnPlatforms = new List<SpawnPlatform>();

        [Header("Настройки волн")] 
        
        [SerializeField]
        private SpawnPlatform.State[] unitTypes = new[]
        {
            SpawnPlatform.State.Skeleton,
            SpawnPlatform.State.SkeletonArcher,
            SpawnPlatform.State.GobArcher,
            SpawnPlatform.State.OrcWar
        };

        private float timeUntilNextSpawn;
        private bool isFirstSpawn = true;
        private List<HeroesBase> spawnedUnits = new List<HeroesBase>();

        // Событие для UI
        public delegate void SpawnTimerUpdateHandler(float timeRemaining);

        public event SpawnTimerUpdateHandler OnSpawnTimerUpdate;
        public event System.Action OnWaveSpawned;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Автоматически находим все платформы, если не назначены вручную
            if (spawnPlatforms.Count == 0)
            {
                spawnPlatforms = FindObjectsOfType<SpawnPlatform>().ToList();
            }
        }

        private void Start()
        {
            ResetSpawnTimer();
            Debug.Log($"Старт менеджера спауна. Первая волна через {initialSpawnDelay} секунд");
        }

        private void Update()
        {
            if (spawnPlatforms.Count == 0) return;

            timeUntilNextSpawn -= Time.deltaTime;

            // Обновляем UI
            OnSpawnTimerUpdate?.Invoke(timeUntilNextSpawn);

            if (timeUntilNextSpawn <= 0)
            {
                SpawnWave();
                ResetSpawnTimer();
            }
        }

        private void ResetSpawnTimer()
        {
            if (isFirstSpawn)
            {
                timeUntilNextSpawn = initialSpawnDelay;
                isFirstSpawn = false;
            }
            else
            {
                timeUntilNextSpawn = Random.Range(spawnIntervalMin, spawnIntervalMax);
            }

            Debug.Log($"Следующая волна через {timeUntilNextSpawn:F0} секунд");
        }

        private void SpawnWave()
        {
            Debug.Log("=== НАЧАЛО ВОЛНЫ ===");
            int totalSpawned = 0;

            foreach (var platform in spawnPlatforms)
            {
                if (platform == null) continue;

                int unitsToSpawn = Random.Range(minUnitsPerPlatform, maxUnitsPerPlatform + 1);

                for (int i = 0; i < unitsToSpawn; i++)
                {
                    SpawnPlatform.State randomType = unitTypes[Random.Range(0, unitTypes.Length)];
                    HeroesBase unit = platform.InvokeSpawn(randomType);

                    if (unit != null)
                    {
                        spawnedUnits.Add(unit);
                        totalSpawned++;

                        // Подписываемся на смерть юнита
                        unit.OnDeath += () => RemoveUnit(unit);
                    }
                }

                Debug.Log($"Платформа {platform.name} заспаунила {unitsToSpawn} юнитов");
            }

            Debug.Log($"=== ВОЛНА ЗАКОНЧИЛАСЬ. Всего заспаунено: {totalSpawned} ===");
            OnWaveSpawned?.Invoke();
        }

        private void RemoveUnit(HeroesBase unit)
        {
            if (spawnedUnits.Contains(unit))
            {
                spawnedUnits.Remove(unit);
                Debug.Log($"Юнит удален из списка. Осталось: {spawnedUnits.Count}");
            }
        }

        // API для других скриптов
        public float GetTimeUntilNextSpawn() => timeUntilNextSpawn;
        public int GetActiveUnitsCount() => spawnedUnits.Count;
        public List<SpawnPlatform> GetActivePlatforms() => spawnPlatforms;

        public void AddPlatform(SpawnPlatform platform)
        {
            if (!spawnPlatforms.Contains(platform))
            {
                spawnPlatforms.Add(platform);
            }
        }

        public void RemovePlatform(SpawnPlatform platform)
        {
            spawnPlatforms.Remove(platform);
        }

        public void ForceSpawnWave()
        {
            SpawnWave();
            ResetSpawnTimer();
        }

        public void ClearAllUnits()
        {
            foreach (var unit in spawnedUnits.ToList())
            {
                if (unit != null)
                    Destroy(unit.gameObject);
            }

            spawnedUnits.Clear();
        }

        private void OnDestroy()
        {
            // Отписываемся от всех событий
            foreach (var unit in spawnedUnits)
            {
                if (unit != null)
                    unit.OnDeath -= () => RemoveUnit(unit);
            }
        }
    }
}