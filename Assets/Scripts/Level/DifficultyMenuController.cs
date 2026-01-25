using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Level.Loading;

namespace Level
{
    public class DifficultyMenuController : MonoBehaviour
    {
        [Header("Кнопки сложности")] 
        
        [SerializeField] private Button easyButton;

        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;

        [Header("Красная рамка (Image) на каждой кнопке")] [SerializeField]
        private Image easyHighlight; // красный квадрат у "Легко"

        [SerializeField] private Image normalHighlight; // красный квадрат у "Не легко"
        [SerializeField] private Image hardHighlight; // красный квадрат у "Сложно"

        [Header("Кнопка Start")] [SerializeField]
        private Button startButton;

        [SerializeField] private Image startButtonImage; // Image компонента на кнопке "Начать"
        [SerializeField] private Sprite startDefaultSprite; // спрайт до нажатия
        [SerializeField] private Sprite startPressedSprite; // спрайт после нажатия

        [Header("Переход")] 
        
        [SerializeField] private string nextSceneName = "PinchLevel"; // имя сцены для перехода
        [SerializeField, Min(0f)] private float startDelay = 0.0f;

        private bool _starting;

        private void Awake()
        {
            // Подгружаем сохранённую сложность (если надо)
            GameSettings.LoadDifficulty();

            // Подписки на кнопки сложности
            easyButton.onClick.AddListener(() => SelectDifficulty(GameDifficulty.Easy));
            normalButton.onClick.AddListener(() => SelectDifficulty(GameDifficulty.Normal));
            hardButton.onClick.AddListener(() => SelectDifficulty(GameDifficulty.Hard));

            // Подписка на Start
            startButton.onClick.AddListener(OnStartClicked);
        }

        private void Start()
        {
            // Применим текущее (включая загруженное из PlayerPrefs)
            ApplyHighlights(GameSettings.Difficulty);

            // На всякий случай выставим спрайт старта в дефолт
            if (startButtonImage != null && startDefaultSprite != null)
                startButtonImage.sprite = startDefaultSprite;
        }

        private void SelectDifficulty(GameDifficulty difficulty)
        {
            if (_starting) return;

            GameSettings.SetDifficulty(difficulty, saveToPrefs: true);
            ApplyHighlights(difficulty);
        }

        private void ApplyHighlights(GameDifficulty difficulty)
        {
            // Активной показываем рамку, остальным скрываем
            if (easyHighlight != null) easyHighlight.enabled = (difficulty == GameDifficulty.Easy);
            if (normalHighlight != null) normalHighlight.enabled = (difficulty == GameDifficulty.Normal);
            if (hardHighlight != null) hardHighlight.enabled = (difficulty == GameDifficulty.Hard);
        }

        private void OnStartClicked()
        {
            if (_starting) return;
            _starting = true;
            // Меняем картинку на кнопке Start
            if (startButtonImage != null && startPressedSprite != null)
                startButtonImage.sprite = startPressedSprite;
            // Блокируем повторные тыки (люди любят тыкать)
            startButton.interactable = false;
            easyButton.interactable = false;
            normalButton.interactable = false;
            hardButton.interactable = false;
            StartCoroutine(LoadSceneAfterDelay());
        }

        private IEnumerator LoadSceneAfterDelay()
        {
            Debug.Log($"[DifficultyMenuController] nextSceneName: {nextSceneName}");
                 yield return new WaitForSeconds(startDelay);
                 SceneTransition.Load(nextSceneName);
                     //  SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        }
    }
}