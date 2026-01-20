using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using UnityEngine;
using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

namespace Player
{  
    [System.Serializable]
    public class AuthResponse
    {
        public bool success;
        public string message;
        public AuthData data;
    }

    [System.Serializable]
    public class AuthData
    {
        public UserData user;
        public string token;
        public string token_type;
    }

    [System.Serializable]
    public class UserData
    {
        public string name;
        public string updated_at;
        public string created_at;
        public int id;
    }

    public class PlayerAuthManager : MonoBehaviour
    {
        public static PlayerAuthManager Instance { get; private set; }

        [SerializeField] private string playerName;
        [SerializeField] private string authToken;
        [SerializeField] private int userId;
        
        private const string PLAYER_NAME_KEY = "PlayerName";
        private const string AUTH_TOKEN_KEY = "AuthToken";
        private const string USER_ID_KEY = "UserId";
        private const string IS_REGISTERED_KEY = "IsRegistered";

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSavedData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadSavedData()
        {
            // Загружаем сохраненные данные
            if (PlayerPrefs.HasKey(PLAYER_NAME_KEY))
            {
                playerName = PlayerPrefs.GetString(PLAYER_NAME_KEY);
                authToken = PlayerPrefs.GetString(AUTH_TOKEN_KEY);
                userId = PlayerPrefs.GetInt(USER_ID_KEY);
                
                bool isRegistered = PlayerPrefs.GetInt(IS_REGISTERED_KEY, 0) == 1;
                
                if (isRegistered && !string.IsNullOrEmpty(authToken))
                {
                    Debug.Log($"Loaded registered user: {playerName}, ID: {userId}");
                    // Пользователь уже зарегистрирован и есть токен
                }
                else
                {
                    // Нужна регистрация
                    StartCoroutine(RegisterPlayer());
                }
            }
            else
            {
                // Первый запуск - генерируем уникальное имя и регистрируемся
                GenerateUniqueName();
                StartCoroutine(RegisterPlayer());
            }
        }

        private void GenerateUniqueName()
        {
            // Генерируем уникальное имя для регистрации
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            
            if (string.IsNullOrEmpty(deviceId) || deviceId == "unknown")
            {
                deviceId = Guid.NewGuid().ToString();
            }
            
            long timestamp = DateTime.UtcNow.Ticks;
            int randomNum = UnityEngine.Random.Range(10000, 99999);
            
            playerName = $"Player_{deviceId.GetHashCode():X6}_{timestamp % 1000000}_{randomNum}";
            playerName = playerName.Replace("-", "").Substring(0, Mathf.Min(playerName.Length, 50));
            
            Debug.Log($"Generated player name: {playerName}");
        }

        // Метод для регистрации на сервере и получения токена
        private IEnumerator RegisterPlayer()
        {
            string serverUrl = "http://localhost:8881/api/auth/register";
            
            WWWForm form = new WWWForm();
            form.AddField("name", playerName);

            using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
            {
                www.SetRequestHeader("Accept", "application/json");
                www.chunkedTransfer = false;
                
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Registration successful!");
                    
                    // Парсим ответ сервера
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                    
                    if (response.success)
                    {
                        // Сохраняем полученные данные
                        authToken = response.data.token;
                        userId = response.data.user.id;
                        
                        // Сохраняем данные локально
                        SavePlayerData();
                        
                        Debug.Log($"Token received: {authToken.Substring(0, 20)}...");
                        Debug.Log($"User ID: {userId}");
                        
                        // Можно вызвать событие успешной регистрации
                        OnRegistrationComplete?.Invoke(true, response.message);
                    }
                    else
                    {
                        Debug.LogError($"Registration failed: {response.message}");
                        OnRegistrationComplete?.Invoke(false, response.message);
                    }
                }
                else
                {
                    Debug.LogError($"Registration error: {www.error}");
                    OnRegistrationComplete?.Invoke(false, www.error);
                }
            }
        }

        private void SavePlayerData()
        {
            PlayerPrefs.SetString(PLAYER_NAME_KEY, playerName);
            PlayerPrefs.SetString(AUTH_TOKEN_KEY, authToken);
            PlayerPrefs.SetInt(USER_ID_KEY, userId);
            PlayerPrefs.SetInt(IS_REGISTERED_KEY, 1);
            PlayerPrefs.Save();
            
            Debug.Log("Player data saved locally");
        }

        // Общий метод для отправки запросов с авторизацией
        public IEnumerator SendAuthorizedRequest(string url, WWWForm form = null, Action<string> onSuccess = null, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(authToken))
            {
                Debug.LogError("No auth token available");
                onError?.Invoke("No auth token");
                yield break;
            }

            UnityWebRequest www;
            
            if (form != null)
            {
                www = UnityWebRequest.Post(url, form);
            }
            else
            {
                www = UnityWebRequest.Get(url);
            }
            
            // Добавляем токен авторизации
            www.SetRequestHeader("Authorization", $"Bearer {authToken}");
            www.SetRequestHeader("Accept", "application/json");
            
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Request to {url} successful");
                onSuccess?.Invoke(www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"Request to {url} failed: {www.error}");
                onError?.Invoke(www.error);
            }
        }

        // Пример метода для отправки статистики босса
        public IEnumerator SendBossDamage(int bossId, float damageDealt)
        {
            string url = "http://localhost:8881/api/game/boss-damage";
            
            WWWForm form = new WWWForm();
            form.AddField("boss_id", bossId.ToString());
            form.AddField("damage", damageDealt.ToString());
            form.AddField("timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"));

            yield return SendAuthorizedRequest(
                url, 
                form,
                response => {
                    Debug.Log($"Boss damage recorded: {response}");
                    // Обработка успешного ответа
                },
                error => {
                    Debug.LogError($"Failed to record boss damage: {error}");
                    // Обработка ошибки
                }
            );
        }

        // Пример метода для отправки убийств героев
        public IEnumerator SendHeroKill(int heroId, string heroType, int waveNumber)
        {
            string url = "http://localhost:8881/api/game/hero-kill";
            
            WWWForm form = new WWWForm();
            form.AddField("hero_id", heroId.ToString());
            form.AddField("hero_type", heroType);
            form.AddField("wave", waveNumber.ToString());
            form.AddField("timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"));

            yield return SendAuthorizedRequest(
                url, 
                form,
                response => {
                    Debug.Log($"Hero kill recorded: {response}");
                    // Обработка успешного ответа
                },
                error => {
                    Debug.LogError($"Failed to record hero kill: {error}");
                    // Обработка ошибки
                }
            );
        }

        // Метод для получения статистики
        public IEnumerator GetPlayerStats(Action<string> onSuccess)
        {
            string url = $"http://localhost:8881/api/game/stats/{userId}";
            
            yield return SendAuthorizedRequest(
                url,
                null,
                onSuccess,
                error => Debug.LogError($"Failed to get stats: {error}")
            );
        }

        // Метод для сброса данных (для тестирования)
        public void ResetPlayerData()
        {
            PlayerPrefs.DeleteKey(PLAYER_NAME_KEY);
            PlayerPrefs.DeleteKey(AUTH_TOKEN_KEY);
            PlayerPrefs.DeleteKey(USER_ID_KEY);
            PlayerPrefs.DeleteKey(IS_REGISTERED_KEY);
            PlayerPrefs.Save();
            
            playerName = "";
            authToken = "";
            userId = 0;
            
            Debug.Log("Player data reset");
        }

        // Свойства для доступа к данным
        public string PlayerName => playerName;
        public string AuthToken => authToken;
        public int UserId => userId;
        public bool IsRegistered => !string.IsNullOrEmpty(authToken);

        // Событие для уведомления о завершении регистрации
        public event Action<bool, string> OnRegistrationComplete;
    }
}