using System;
using UnityEngine;
using Spine.Unity;
using Level.Loading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using Player;
using UnityEngine.Networking;
using TMPro;

namespace Ui
{
    public class HighScoreTable : MonoBehaviour
    {
        private List<HighScoreEntry> highScores = new List<HighScoreEntry>();
        private UserStats currentUserStats;


        [System.Serializable]
        public class HighScoreEntry
        {
            public int user_id;
            public string player_name;
            public int total_damage;
            public int total_kills;
            public int win_count;
            public int total_games;
            public int rank;
            public int user_rank;


            public string last_played_at;
        }

        [System.Serializable]
        public class UserStats
        {
            public int user_id;
            public string player_name;
            public int total_damage;
            public int total_kills;
            public int win_count;
            public int total_games;
            public int rank;
            public float avg_damage;
            public float avg_kills;
            public string last_played_at;
        }

        [System.Serializable]
        public class Top10Response
        {
            public bool success;
            public string message;
            public List<HighScoreEntry> data;
            public UserStats user_stats;
            public int user_rank;
            public int current_user_id;
        }


        [Header("Preloader Settings")] [SerializeField]
        private GameObject preloaderPanel;

        [SerializeField] private Image loadingSpinner;
        [SerializeField] private Text preloaderText;
        [SerializeField] private TextMeshProUGUI preloaderTMProText;
        [SerializeField] private float spinnerRotationSpeed = 180f;
        [SerializeField] private float loadingDotsSpeed = 0.5f;


        [System.Serializable]
        public class SimpleTop10Response
        {
            public bool success;
            public string message;
            public List<HighScoreEntry> data;
        }


        //   [Header("Animations")] [SerializeField]
        [SerializeField] private Transform _entryContainer;
        [SerializeField] private Transform _entryTemplate;
        private bool isLoading = false;

        [SerializeField] private Text loadingText;
        [SerializeField] private Text userRankText;
        [SerializeField] private Text userStatsText;


        private int maxEntries = 5;
        private float _templateHeight = 90f;
        private Coroutine loadingDotsCoroutine;

        // Добавьте Text элементы для отображения данных


        private void Start()
        {
            // Инициализация прелоадера
            // InitializePreloader();
            StartCoroutine(LoadHighScores());
        }

        private IEnumerator LoadHighScores()
        {
            if (isLoading) yield break;

            isLoading = true;


            if (loadingText != null)
                loadingText.text = "Загрузка рейтинга...";

            if (userRankText != null)
                userRankText.text = "";

            if (userStatsText != null)
                userStatsText.text = "";

            // Проверяем авторизацию
            //  if (Player.PlayerAuthManager.Instance != null && Player.PlayerAuthManager.Instance.IsRegistered)
            //  {
            // Авторизованный пользователь - загружаем топ-10 с центрированием
            yield return StartCoroutine(LoadTop10Centered());
            //    }
            //    else
            //    {
            //       // Неавторизованный пользователь - загружаем обычный топ-10
            //      yield return StartCoroutine(LoadTop10Public());
            //   }

            isLoading = false;
        }

        private void Update()
        {
            // Анимация спиннера
            if (loadingSpinner != null && loadingSpinner.gameObject.activeSelf)
            {
                loadingSpinner.transform.Rotate(0, 0, -spinnerRotationSpeed * Time.deltaTime);
            }
        }


        private IEnumerator LoadTop10Centered()
        {
            string url = "https://lich.staers.ru/api/game/stats/top10-centered";
            string token = Player.PlayerAuthManager.Instance.AuthToken;

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("Authorization", $"Bearer {token}");
                www.SetRequestHeader("Accept", "application/json");

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Top10Response response = JsonUtility.FromJson<Top10Response>(www.downloadHandler.text);

                    if (response.success)
                    {
                        highScores = response.data;
                        currentUserStats = response.user_stats;

                        Debug.Log($"Загружено {highScores.Count} записей. Позиция пользователя: {response.user_rank}");
                        // Скрываем прелоадер
                        HidePreloader();
                        // Обновляем UI с информацией о пользователе
                        UpdateUserInfoUI();

                        // Отображаем таблицу
                        DisplayHighScores();

                        if (loadingText != null)
                            loadingText.gameObject.SetActive(false);
                    }
                    else
                    {
                        Debug.LogError($"Ошибка сервера: {response.message}");
                        if (loadingText != null)
                            loadingText.text = $"Ошибка: {response.message}";

                        // Пробуем загрузить обычный топ-10
                        yield return StartCoroutine(LoadTop10Public());
                    }
                }
                else
                {
                    Debug.LogError($"Ошибка сети: {www.error}");

                    if (loadingText != null)
                    {
                        if (www.responseCode == 401)
                            loadingText.text = "Требуется авторизация";
                        else
                            loadingText.text = "Ошибка соединения";
                    }

                    // Пробуем загрузить обычный топ-10
                    yield return StartCoroutine(LoadTop10Public());
                }
            }
        }

        private void UpdateUserInfoUI()
        {
            if (currentUserStats == null) return;

            if (userRankText != null)
            {
                userRankText.text = $"Ваша позиция: #{currentUserStats.rank}";
            }

            if (userStatsText != null)
            {
                userStatsText.text = $"Урон: {currentUserStats.total_damage:N0} | " +
                                     $"Убийства: {currentUserStats.total_kills} | " +
                                     $"Победы: {currentUserStats.win_count}";
            }
        }

        private IEnumerator LoadTop10Public()
        {
            string url = "https://lich.staers.ru/api/stats/top10";
             
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("Accept", "application/json");

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        SimpleTop10Response response =
                            JsonUtility.FromJson<SimpleTop10Response>(www.downloadHandler.text);

                        if (response.success)
                        {
                            highScores = response.data;
                            currentUserStats = null;

                            Debug.Log($"Загружен публичный топ-10: {highScores.Count} записей");

                            DisplayHighScores();

                            if (loadingText != null)
                            {
                                loadingText.text = "Публичный рейтинг";
                                // Через 2 секунды скрываем текст
                                StartCoroutine(HideLoadingTextAfterDelay(2f));
                            }
                        }
                        else
                        {
                            Debug.LogError($"Ошибка при загрузке публичного рейтинга: {response.message}");
                            CreateMockData();
                            DisplayHighScores();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Ошибка парсинга публичного рейтинга: {e.Message}");
                        CreateMockData();
                        DisplayHighScores();
                    }
                }
                else
                {
                    Debug.LogError($"Ошибка сети при загрузке публичного рейтинга: {www.error}");
                    CreateMockData();
                    DisplayHighScores();
                }
            }
        }


        private IEnumerator HideLoadingTextAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (loadingText != null)
                loadingText.gameObject.SetActive(false);
        }


        private void CreateMockData()
        {
            highScores = new List<HighScoreEntry>();

            // Добавляем текущего пользователя в центр (если авторизован)
            int userRank = UnityEngine.Random.Range(4, 8);
            string userName = "Player_" + UnityEngine.Random.Range(1000, 9999);

            if (Player.PlayerAuthManager.Instance != null && Player.PlayerAuthManager.Instance.IsRegistered)
            {
                userName = Player.PlayerAuthManager.Instance.PlayerName;
            }

            // Создаем 5 записей выше пользователя
            for (int i = userRank - 5; i < userRank; i++)
            {
                if (i >= 1)
                {
                    highScores.Add(CreateMockEntry(i, $"Player_{UnityEngine.Random.Range(1000, 9999)}"));
                }
            }

            // Добавляем пользователя
            highScores.Add(CreateMockEntry(userRank, userName));

            // Создаем записи ниже пользователя
            for (int i = userRank + 1; i <= userRank + 4 && highScores.Count < 10; i++)
            {
                highScores.Add(CreateMockEntry(i, $"Player_{UnityEngine.Random.Range(1000, 9999)}"));
            }

            Debug.Log("Созданы тестовые данные");
        }

        private HighScoreEntry CreateMockEntry(int rank, string name)
        {
            return new HighScoreEntry
            {
                rank = rank,
                user_id = UnityEngine.Random.Range(1000, 9999),
                player_name = name,
                total_damage = UnityEngine.Random.Range(50000, 200000),
                total_kills = UnityEngine.Random.Range(100, 500),
                win_count = UnityEngine.Random.Range(5, 50),
                total_games = UnityEngine.Random.Range(10, 100),
                last_played_at = DateTime.Now.AddDays(-UnityEngine.Random.Range(0, 30)).ToString("o")
            };
        }

        // Вспомогательный метод для безопасной установки текста
        private void SetTextIfExists(Transform parent, string childName, string text)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                Text textComponent = child.GetComponent<Text>();
                if (textComponent != null)
                {
                    textComponent.text = text;
                }
                else
                {
                    Debug.LogWarning($"No Text component found on {childName}");
                }
            }
            else
            {
                Debug.LogWarning($"Child {childName} not found in entry template");
            }
        }


        private void DisplayHighScores()
        {
            int displayCount = Mathf.Min(highScores.Count, maxEntries);

            Debug.Log($"highScores.Count {highScores.Count} highScores.Counte");
            for (int i = 0; i < highScores.Count; i++)
            {
                Transform entryTransform = Instantiate(_entryTemplate, _entryContainer);
                RectTransform entryRectTransform = entryTransform.GetComponent<RectTransform>();
                entryRectTransform.anchoredPosition = new Vector2(0, -_templateHeight * i);
                entryTransform.gameObject.SetActive(true);

                // Получаем данные записи
                var entry = highScores[i];

                var damageTf = entryTransform.Find("TitleRows/DamageText");
                if (damageTf != null)
                {
                    var tmp = damageTf.GetComponent<TextMeshProUGUI>();
                    tmp.text = entry.total_damage.ToString();

                    if (entry.user_id == Player.PlayerAuthManager.Instance.UserId)
                    {
                        tmp.color = GetColor();
                    }
                }

                var NameText = entryTransform.Find("TitleRows/NameText");
                if (NameText != null)
                {
                    var tmp = NameText.GetComponent<TextMeshProUGUI>();
                    tmp.text = entry.player_name.ToString();

                    if (entry.user_id == Player.PlayerAuthManager.Instance.UserId)
                    {
                        tmp.color = GetColor();
                    }
                }

                var RankText = entryTransform.Find("TitleRows/RankText");
                if (RankText != null)
                {
                    Debug.Log($"entry.rank {entry.rank}");
                    var tmp = RankText.GetComponent<TextMeshProUGUI>();
                    tmp.text = entry.user_rank.ToString();

                    if (entry.user_id == Player.PlayerAuthManager.Instance.UserId)
                    {
                        tmp.color = GetColor();
                    }
                }

                var KillsTotalText = entryTransform.Find("TitleRows/KillsTotalText");
                if (KillsTotalText != null)
                {
                    var tmp = KillsTotalText.GetComponent<TextMeshProUGUI>();
                    tmp.text = entry.total_kills.ToString();

                    if (entry.user_id == Player.PlayerAuthManager.Instance.UserId)
                    {
                        tmp.color = GetColor();
                    }
                }

                // Подсветка текущего игрока (опционально)
                if (Player.PlayerAuthManager.Instance.IsRegistered &&
                    entry.user_id == Player.PlayerAuthManager.Instance.UserId)
                {
                    var entryImage = entryTransform.GetComponent<Image>();
                    if (entryImage != null)
                    {
                        entryImage.color = new Color(0.2f, 0.4f, 0.8f, 0.3f);
                    }
                }
            }
        }

        private Color GetColor()
        {
            return Color.chartreuse;
        }

        private void HidePreloader()
        {
            if (preloaderPanel != null)
                preloaderPanel.SetActive(false);

            if (loadingSpinner != null)
                loadingSpinner.gameObject.SetActive(false);

            if (loadingDotsCoroutine != null)
            {
                StopCoroutine(loadingDotsCoroutine);
                loadingDotsCoroutine = null;
            }
        }
    }
}