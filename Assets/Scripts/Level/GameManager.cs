using UnityEngine;
using Heroes; 
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Level
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("События игры")] 
        
        [Header("Переход на сцены")] 
        
        [SerializeField] private string sceneNameWin = "LevelWin"; 
        [SerializeField] private string sceneNameLose = "LevelLose"; 
        
        public UnityEvent onGameWin;
        public UnityEvent onGameLose;
        
     //   public GameObject onGameObjectWin;
     //   public GameObject onGameObjectLose;

        [Header("Отладка")] [SerializeField] private bool debugMode = true;

        private bool _gameEnded = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Подписываемся на событие загрузки сцены
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDestroy()
        {
            // Отписываемся от события
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Вызывается при загрузке новой сцены
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Сбрасываем состояние игры при загрузке новой сцены
            ResetGame();
            
            // Логируем загрузку сцены
            if (debugMode)
            {
            //    Debug.Log($"Загружена сцена: {scene.name}, GameManager сброшен");
            }
        }
        /// <summary>
        /// Вызывается при смерти героя
        /// </summary>
        public void OnHeroDeath(HeroesBase.Hero heroType)
        {
            if (_gameEnded) return;

            // Проверяем смерть Лича (игрока)
            if (heroType == HeroesBase.Hero.Lich)
            {
                if (debugMode) Debug.Log("💀 ПРОИГРЫШ: Умер Лич (главный герой)");
                GameOver(false); // проигрыш
            }
            // Проверяем смерть Шамана (вражеского босса)
            else if (heroType == HeroesBase.Hero.Shaman)
            {
                if (debugMode) Debug.Log("🎉 ПОБЕДА: Умер Шаман (вражеский босс)");
                GameOver(true); // победа
            }
        }

        private void GameOver(bool isWin)
        {
            if (_gameEnded) return;
            _gameEnded = true;

            // Вызываем события
            if (isWin)
            { 
                onGameWin?.Invoke();
                Debug.Log("════════════════════════════════");
                Debug.Log("            ПОБЕДА!");
                Debug.Log("════════════════════════════════");
                
                // Загружаем сцену победы
                if (!string.IsNullOrEmpty(sceneNameWin))
                {
                    StartCoroutine(LoadSceneWithDelay(sceneNameWin, 2f));
                }
            }
            else
            { 
                onGameLose?.Invoke();
                Debug.Log("════════════════════════════════");
                Debug.Log("           ПРОИГРЫШ!");
                Debug.Log("════════════════════════════════");
                
                // Загружаем сцену проигрыша
                if (!string.IsNullOrEmpty(sceneNameLose))
                {
                    StartCoroutine(LoadSceneWithDelay(sceneNameLose, 2f));
                }
            }
            
            // Останавливаем всех юнитов
            StopAllUnits();
        }
        
        
        /// <summary>
        /// Загружает сцену с задержкой
        /// </summary>
        private System.Collections.IEnumerator LoadSceneWithDelay(string sceneName, float delay)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(sceneName);
        }
        
        
        /// <summary>
        /// Останавливает всех юнитов
        /// </summary>
        private void StopAllUnits()
        {
            var allUnits = FindObjectsOfType<WarriorAI>();
            foreach (var unit in allUnits)
            {
                unit.SetIsStoppedAgent();
            }
        }
        /// <summary>
        /// Сбросить состояние игры (для рестарта)
        /// </summary>
        public void ResetGame()
        {
            _gameEnded = false;
            Time.timeScale = 1f;
        }
    }
}