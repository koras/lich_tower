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

        [Header("Ссылка на объект затемнения")]
        [SerializeField] private GameObject fadeOverlayObject; // Лучше ссылаться напрямую в инспекторе

        [SerializeField]  private SpriteRenderer _spriteRenderer;
        private Color originalColor;

        public static GameManager Instance { get; private set; }

        [Header("События игры")] 
        private string _statGame = "StatGame";
        [SerializeField] private GameObject _prefubWin;
        [SerializeField] private GameObject _prefubLose;

        [Header("Отладка")] 
        [SerializeField] private bool debugMode = true;

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
            
            // НЕ инициализируем здесь spriteRenderer, т.к. объект может быть на другой сцене
            // Вместо этого ищем при старте первой сцены
        }

        void Start()
        {
            // Находим объекты при старте первой сцены
          //  InitializeFadeOverlay();
            
            if (_spriteRenderer != null)
            {
                originalColor = _spriteRenderer.color;
            }
            else
            {
                Debug.LogWarning("[GameManager] Не удалось найти SpriteRenderer для затемнения");
            }
        }

        /// <summary>
        /// Инициализирует объект затемнения
        /// </summary>
        private void InitializeFadeOverlay()
        {
            // Сначала проверяем, не назначен ли объект в инспекторе
            if (fadeOverlayObject != null)
            {
                _spriteRenderer = fadeOverlayObject.GetComponent<SpriteRenderer>();
                if (_spriteRenderer != null)
                {
                    Debug.Log($"[GameManager] Найден SpriteRenderer для затемнения: {fadeOverlayObject.name}");
                    _spriteRenderer.gameObject.SetActive(false);
                    return;
                }
            }
            
            // Если не назначен в инспекторе, ищем по тегу
            GameObject fadeObject = GameObject.FindGameObjectWithTag("FadeOverlay");
            
            if (fadeObject != null)
            {
                //    fadeOverlayObject = fadeObject;
                _spriteRenderer = fadeObject.GetComponent<SpriteRenderer>();
                
                if (_spriteRenderer != null)
                {
                    Debug.Log($"[GameManager] Найден SpriteRenderer по тегу: {fadeObject.name}");
                    _spriteRenderer.gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogError($"[GameManager] У объекта с тегом 'FadeOverlay' нет компонента SpriteRenderer! Объект: {fadeObject.name}");
                    
                    // Попробуем найти SpriteRenderer у потомков
                    _spriteRenderer = fadeObject.GetComponentInChildren<SpriteRenderer>(true);
                    if (_spriteRenderer != null)
                    {
                        Debug.Log($"[GameManager] Найден SpriteRenderer у потомков объекта: {fadeObject.name}");
                        _spriteRenderer.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                Debug.LogError("[GameManager] Не найден объект с тегом 'FadeOverlay' на сцене!");
                
                // Дополнительный поиск по имени (если тег не настроен)
                fadeObject = GameObject.Find("FadeOverlay");
                if (fadeObject == null)
                    fadeObject = GameObject.Find("UILevelCanvas/TheEndBackground");
                
                if (fadeObject != null)
                {
                    _spriteRenderer = fadeObject.GetComponent<SpriteRenderer>();
                    if (_spriteRenderer != null)
                    {
                        Debug.Log($"[GameManager] Найден SpriteRenderer по имени: {fadeObject.name}");
                        _spriteRenderer.gameObject.SetActive(false);
                    }
                }
            }
        }

        public void StartFadeIn()
        {
            if (_spriteRenderer == null)
            {
                Debug.LogError("[GameManager] Нельзя начать затемнение: SpriteRenderer не найден!");
                return;
            }
            
            StartCoroutine(FadeInCoroutine());
        }

        IEnumerator FadeInCoroutine()
        {
            if (_spriteRenderer == null) yield break;
            
            // Устанавливаем начальную прозрачность
            Color startColor = originalColor;
            startColor.a = 0f;
            _spriteRenderer.color = startColor;

            // Включаем рендерер если он был выключен
            _spriteRenderer.enabled = true;
            _spriteRenderer.gameObject.SetActive(true);

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
            Color finalColor = originalColor;
            finalColor.a = 1f;
            _spriteRenderer.color = finalColor;

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
            ResetGame();
            
            // Находим объекты на новой сцене
            RefreshSceneReferences();
    
            if (debugMode)
            {
                Debug.Log($"[GameManager] Загружена сцена: {scene.name}, ссылки обновлены");
            }
        }

        /// <summary>
        /// Вызывается при смерти героя
        /// </summary>
        public void OnHeroDeath(HeroesBase.Hero heroType)
        {
            Debug.Log($"[GameManager] OnHeroDeath ");
            if (_gameEnded) return;

            // Проверяем смерть Лича (игрока)
            if (heroType == HeroesBase.Hero.Lich)
            {
                if (debugMode) Debug.Log("[GameManager] 💀 ПРОИГРЫШ: Умер Лич (главный герой)");
                GameOver(false); // проигрыш
            }
            // Проверяем смерть Шамана (вражеского босса)
            else if (heroType == HeroesBase.Hero.Shaman)
            {
                if (debugMode) Debug.Log("[GameManager] 🎉 ПОБЕДА: Умер Шаман (вражеский босс)");
                GameOver(true); // победа
            }
        }

        private void GameOver(bool isWin)
        {
            // Убеждаемся, что _spriteRenderer инициализирован
            if (_spriteRenderer == null)
            {

                Debug.LogError("[GameManager] Критическая ошибка: SpriteRenderer для затемнения не найден!");
                // Продолжаем без затемнения

            }

            if (_gameEnded) return;
            _gameEnded = true;
            
            int sessionId = Player.GameAPIService.Instance.GetCurrentSessionId();
            var payload = Player.GameStatsCollector.I.BuildPayload(isWin, sessionId);
            StartCoroutine(Player.GameAPIService.Instance.SendFinalStats(payload));
            
            Transform fp = transform;
            Vector2 spawnPos = fp.position;
            spawnPos.y -= 0.1f;
            
            if (fadeOnStart && _spriteRenderer != null)
            {
                StartFadeIn();
            }

            // Вызываем события
            if (isWin)
            {
                if (_prefubWin != null)
                {
                    _prefubWin.SetActive(true);
                    _prefubWin.GetComponent<WinLose>()?.Load();
                    Debug.Log("ПОБЕДА!");
                }
            }
            else
            {
                if (_prefubLose != null)
                {
                    _prefubLose.SetActive(true);
                    _prefubLose.GetComponent<WinLose>()?.Load();
                    Debug.Log("ПРОИГРЫШ!");
                }
            }

            if (!string.IsNullOrEmpty(_statGame))
            {
                StartCoroutine(LoadSceneWithDelay(_statGame, 2f));
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
        /// Обновляет ссылки на объекты текущей сцены
        /// </summary>
        private void RefreshSceneReferences()
        {
         //   InitializeFadeOverlay();
    
         if (debugMode) Debug.LogWarning("[GameManager] RefreshSceneReferences");
         
            if (fadeOverlayObject == null)
            {
                fadeOverlayObject = GameObject.Find("CameraGame/Main Camera/TheEndBackground");
                if (fadeOverlayObject != null) 
                {
                    fadeOverlayObject.SetActive(false);
                    
                    _spriteRenderer = fadeOverlayObject.GetComponent<SpriteRenderer>();
                    if (debugMode) Debug.Log($"[GameManager] Найден Background объект: {fadeOverlayObject.name}");
                    if (debugMode) Debug.Log($"[GameManager] установили _spriteRenderer");
                }
                else
                {
                    if (debugMode) Debug.LogWarning("[GameManager] Не найден Background объект");
                }
            }
            else
            {
                
                if (debugMode) Debug.LogWarning("[GameManager] RefreshSceneReferences");
            }


            // Аналогично для других объектов
            if (_prefubWin == null)
            {
                _prefubWin = GameObject.Find("CameraGame/Main Camera/TheEndWin");
                if (_prefubWin != null) 
                {
                    _prefubWin.SetActive(false);
                    if (debugMode) Debug.Log($"[GameManager] Найден Win объект: {_prefubWin.name}");
                }
                else
                {
                    if (debugMode) Debug.LogWarning("[GameManager] Не найден Win объект");
                }
            }

            if (_prefubLose == null)
            {
                _prefubLose = GameObject.Find("CameraGame/Main Camera/TheEndLose");
                if (_prefubLose != null) 
                {
                    _prefubLose.SetActive(false);
                    if (debugMode) Debug.Log($"[GameManager] Найден Lose объект: {_prefubLose.name}");
                }
                else
                {
                    if (debugMode) Debug.LogWarning("[GameManager] Не найден Lose объект");
                }
            }
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
        
        /// <summary>
        /// Метод для ручной установки FadeOverlay (можно вызывать из других скриптов)
        /// </summary>
        public void SetFadeOverlay(GameObject fadeObject)
        {
            if (fadeObject != null)
            {
                fadeOverlayObject = fadeObject;
                _spriteRenderer = fadeObject.GetComponent<SpriteRenderer>();
                
                if (_spriteRenderer != null)
                {
                    Debug.Log($"[GameManager] Установлен FadeOverlay: {fadeObject.name}");
                    _spriteRenderer.gameObject.SetActive(false);
                }
            }
        }
    }
}