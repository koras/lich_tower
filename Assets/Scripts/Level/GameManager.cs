using UnityEngine;
using Heroes; 
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;


namespace Level
{
    public class GameManager : MonoBehaviour
    {
        
        [Header("Настройка затемнения")] 
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private bool fadeOnStart = true;
        [SerializeField] private bool disableOnComplete = false;

        [SerializeField] private GameObject spriteRenderer;
      
         private SpriteRenderer _spriteRenderer;
        private Color originalColor;
        
        
        public static GameManager Instance { get; private set; }

        [Header("События игры")] 
        
        [Header("Переход на сцены")] 
        
        [SerializeField] private string sceneNameWin = "LevelWin"; 
        [SerializeField] private string sceneNameLose = "LevelLose";
        
        
        [SerializeField] private GameObject  _prefubWin; 
        [SerializeField] private GameObject  _prefubLose; 
        
        public UnityEvent onGameWin;
        public UnityEvent onGameLose;
        
     //   public GameObject onGameObjectWin;
     //   public GameObject onGameObjectLose;

        [Header("Отладка")] [SerializeField] private bool debugMode = true;

        private bool _gameEnded = false;

        
        
        
        void Start()
        {
              _spriteRenderer = spriteRenderer.GetComponent<SpriteRenderer>();
              
            if (_spriteRenderer == null)
            {
                Debug.LogError("SpriteFadeIn: SpriteRenderer не найден!");
                return;
            }

            originalColor = _spriteRenderer.color;
        
     
            
        }
        
        public void StartFadeIn()
        {
            StartCoroutine(FadeInCoroutine());
        }
        
        
        IEnumerator FadeInCoroutine()
        {
            // Устанавливаем начальную прозрачность
            Color startColor = originalColor;
            startColor.a = 0f;
            _spriteRenderer.color = startColor;

            // Включаем рендерер если он был выключен
            _spriteRenderer.enabled = true;

            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            
                Color newColor = originalColor;
                newColor.a = alpha;
                _spriteRenderer.color = newColor;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Устанавливаем финальный цвет
            //     spriteRenderer.color = originalColor;

            if (disableOnComplete)
            {
                enabled = false;
            }
        }

        // Для вызова из других скриптов
        public void FadeIn(float customDuration = 0.5f)
        {
            fadeDuration = customDuration;
            StartFadeIn();
        }
        private void Awake()
        {
            _spriteRenderer.gameObject.SetActive(true);
           
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

           // StatsCollector.SubmitFinalStats(isWin);
            
            
            if (_gameEnded) return;
            _gameEnded = true;
            
            int sessionId = Player.GameAPIService.Instance.GetCurrentSessionId();
            var payload = Player.GameStatsCollector.I.BuildPayload(isWin, sessionId);
            StartCoroutine(Player.GameAPIService.Instance.SendFinalStats(payload));
            
            Transform fp = transform;
            Vector2 spawnPos = fp.position;
            spawnPos.y -= 0.1f;
            
            if (fadeOnStart)
            {
                StartFadeIn();
            }
            
            // Вызываем события
            if (isWin)
            {
                
                
                
               var win =  Instantiate(_prefubWin, new Vector3(spawnPos.x, spawnPos.y, 0), Quaternion.identity);
               var winLoseComponent = _prefubWin.GetComponent<WinLose>();
               
               
               if (winLoseComponent != null)
               {
                   winLoseComponent.Load();
               }
               
               
               // if (winLoseComponent != null)
               // {
               //     // Устанавливаем задержку если нужно
               //     //  winLoseComponent._delayBeforeTransition = 2f; // например
               //     // Запускаем корутину перехода
               //     winLoseComponent.StartCoroutine(winLoseComponent.TransitionAfterDelay());
               // }
                
                
                
                
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
                
                Instantiate(_prefubLose, new Vector3(spawnPos.x, spawnPos.y, 0), Quaternion.identity);

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