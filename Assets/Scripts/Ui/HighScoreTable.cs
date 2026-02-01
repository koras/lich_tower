using UnityEngine;
using Spine.Unity;
using Level.Loading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using Player;

namespace Ui
{
    public class HighScoreTable : MonoBehaviour
    {
        //   [Header("Animations")] [SerializeField]
        [SerializeField] private Transform _entryContainer;
        [SerializeField] private Transform _entryTemplate;

        private List<HighScoreEntry> highScores = new List<HighScoreEntry>();


        private int _rowsTable = 10;
        private int maxEntries = 10;
        [SerializeField] private float _templateHeight = 40f; 
        // Добавьте Text элементы для отображения данных
        [SerializeField] private Text loadingText;

        private void Start()
        {
            StartCoroutine(LoadHighScores());
        }

        private void Awake()
        {
            // _entryContainer = _entryContainer.Find("highScoreEntryContainer");
            //  _entryTemplate = _entryContainer.Find("highScoreEntryTemplate");

            _entryTemplate.gameObject.SetActive(false);


            for (int i = 0; i < _rowsTable; i++)
            {
             //   Debug.Log($"[Input] targetCamera {i}");
                Transform entryTransform = Instantiate(_entryTemplate, _entryContainer);
                RectTransform entryRectTransform = entryTransform.GetComponent<RectTransform>();
                entryRectTransform.anchoredPosition = new Vector2(0, -_templateHeight * i);
                entryTransform.gameObject.SetActive(true);

                int rank = i + 1;

                entryTransform.Find("posText").GetComponent<Text>().text = rank.ToString();
                entryTransform.Find("posText").GetComponent<Text>().text = rank.ToString();
                entryTransform.Find("posText").GetComponent<Text>().text = rank.ToString();
            }
        }

        private IEnumerator LoadHighScores()
        {
            if (loadingText != null)
                loadingText.text = "Загрузка рекордов...";

            bool isLoading = true;
            bool hasError = false;

            Player.GameAPIService.Instance.GetHighScores(
                onSuccess: (scores) =>
                {
                    highScores = scores;
                    isLoading = false;
                    Debug.Log($"Loaded {scores.Count} high scores");
                },
                onError: (error) =>
                {
                    Debug.LogError($"Error loading high scores: {error}");
                    isLoading = false;
                    hasError = true;
                }
            );

            // Ждем загрузки
            while (isLoading)
            {
                yield return null;
            }

            if (hasError && loadingText != null)
            {
                loadingText.text = "Ошибка загрузки данных";
                yield break;
            }

            // Очищаем контейнер
            foreach (Transform child in _entryContainer)
            {
                if (child != _entryTemplate)
                    Destroy(child.gameObject);
            }

            // Скрываем шаблон
            _entryTemplate.gameObject.SetActive(false);

            // Отображаем данные
            DisplayHighScores();

            if (loadingText != null)
                loadingText.gameObject.SetActive(false);
        }

        private void DisplayHighScores()
        {
            int displayCount = Mathf.Min(highScores.Count, maxEntries);

            for (int i = 0; i < displayCount; i++)
            {
                Transform entryTransform = Instantiate(_entryTemplate, _entryContainer);
                RectTransform entryRectTransform = entryTransform.GetComponent<RectTransform>();
                entryRectTransform.anchoredPosition = new Vector2(0, -_templateHeight * i);
                entryTransform.gameObject.SetActive(true);

                // Получаем данные записи
                var entry = highScores[i];

                // Заполняем UI элементы
                entryTransform.Find("posText").GetComponent<Text>().text = entry.rank.ToString();
                entryTransform.Find("nameText").GetComponent<Text>().text = entry.player_name;
                entryTransform.Find("scoreText").GetComponent<Text>().text = entry.total_score.ToString();
                entryTransform.Find("killsText").GetComponent<Text>().text = entry.total_kills.ToString();
                entryTransform.Find("damageText").GetComponent<Text>().text = entry.total_damage.ToString();
                entryTransform.Find("winsText").GetComponent<Text>().text = entry.win_count.ToString();

                // Если нужно отобразить дату
                if (entryTransform.Find("dateText") != null)
                {
                    System.DateTime date = System.DateTime.Parse(entry.updated_at);
                    entryTransform.Find("dateText").GetComponent<Text>().text = date.ToString("dd.MM.yyyy");
                }

                // Подсветка текущего игрока (опционально)
                if (Player.PlayerAuthManager.Instance.IsRegistered &&
                    entry.userId == Player.PlayerAuthManager.Instance.UserId)
                {
                    var entryImage = entryTransform.GetComponent<Image>();
                    if (entryImage != null)
                    {
                        entryImage.color = new Color(0.2f, 0.4f, 0.8f, 0.3f);
                    }
                }
            }

            // Если записей меньше максимума, заполняем оставшиеся места пустыми
            for (int i = displayCount; i < maxEntries; i++)
            {
                Transform entryTransform = Instantiate(_entryTemplate, _entryContainer);
                RectTransform entryRectTransform = entryTransform.GetComponent<RectTransform>();
                entryRectTransform.anchoredPosition = new Vector2(0, -_templateHeight * i);
                entryTransform.gameObject.SetActive(true);

                entryTransform.Find("posText").GetComponent<Text>().text = (i + 1).ToString();
                entryTransform.Find("nameText").GetComponent<Text>().text = "---";
                entryTransform.Find("scoreText").GetComponent<Text>().text = "0";
                entryTransform.Find("killsText").GetComponent<Text>().text = "0";
                entryTransform.Find("damageText").GetComponent<Text>().text = "0";
                entryTransform.Find("winsText").GetComponent<Text>().text = "0";

                if (entryTransform.Find("dateText") != null)
                {
                    entryTransform.Find("dateText").GetComponent<Text>().text = "--.--.----";
                }
            }
        }
    }
}