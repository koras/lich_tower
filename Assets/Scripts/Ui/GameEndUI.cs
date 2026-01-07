using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Level;

namespace Ui
{
    public class GameEndUI : MonoBehaviour
    {
        [Header("Панели")] [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;

        [Header("Тексты")] [SerializeField] private TMP_Text winText;
        [SerializeField] private TMP_Text loseText;

        [Header("Кнопки")] [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button nextLevelButton;

        private void Start()
        {
            // Скрываем панели при старте
            winPanel.SetActive(false);
            losePanel.SetActive(false);

            // Подписываемся на события GameManager
            if (Level.GameManager.Instance != null)
            {
                Level.GameManager.Instance.onGameWin.AddListener(ShowWinScreen);
                Level.GameManager.Instance.onGameLose.AddListener(ShowLoseScreen);
            }

            // Настраиваем кнопки
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);

            if (menuButton != null)
                menuButton.onClick.AddListener(GoToMenu);

            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(LoadNextLevel);
        }

        private void ShowWinScreen()
        {
            winPanel.SetActive(true);
            Time.timeScale = 0f; // Пауза игры
        }

        private void ShowLoseScreen()
        {
            losePanel.SetActive(true);
            Time.timeScale = 0f; // Пауза игры
        }

        private void RestartGame()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }

        private void GoToMenu()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        private void LoadNextLevel()
        {
            Time.timeScale = 1f;
            int nextSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneIndex);
            }
        }
    }
}