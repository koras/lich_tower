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
            if (PlayerPrefs.HasKey(PLAYER_NAME_KEY))
            {
                playerName = PlayerPrefs.GetString(PLAYER_NAME_KEY);
                authToken = PlayerPrefs.GetString(AUTH_TOKEN_KEY);
                userId = PlayerPrefs.GetInt(USER_ID_KEY);
                
                bool isRegistered = PlayerPrefs.GetInt(IS_REGISTERED_KEY, 0) == 1;
                
                if (isRegistered && !string.IsNullOrEmpty(authToken))
                {
                    Debug.Log($"Loaded registered user: {playerName}, ID: {userId}");
                }
                else
                {
                    Debug.Log("User exists but not registered, re-registering...");
                    StartCoroutine(RegisterPlayer());
                }
            }
            else
            {
                GenerateSimpleUniqueName();
                StartCoroutine(RegisterPlayer());
            }
        }

        // ИСПРАВЛЕННЫЙ МЕТОД: Генерация более простого имени
        private void GenerateSimpleUniqueName()
        {
            // Простое имя для тестирования
            int randomNum = UnityEngine.Random.Range(1000000, 9999999);
            playerName = $"Player{randomNum}";
            
            Debug.Log($"Generated simple player name: {playerName}");
        }

        // ДОБАВЛЕНО: Метод для отладки запроса
        private void DebugRequest(UnityWebRequest www)
        {
            Debug.Log($"Request URL: {www.url}");
            Debug.Log($"Request Method: {www.method}");
            Debug.Log($"Request Headers: {www.GetRequestHeader("Accept")}");
            Debug.Log($"Request Body (name): {playerName}");
        }

        // УЛУЧШЕННЫЙ МЕТОД: Регистрация с детальной отладкой
        private IEnumerator RegisterPlayer()
        {
            string serverUrl = "http://localhost:8881/api/auth/register";
            
            WWWForm form = new WWWForm();
            form.AddField("name", playerName);

            using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
            {
                www.SetRequestHeader("Accept", "application/json");
                www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                www.chunkedTransfer = false;
                
                // Отладка запроса
                DebugRequest(www);
                
                yield return www.SendWebRequest();

                // Детальная обработка результата
                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Registration successful! Status: {www.responseCode}");
                    Debug.Log($"Response: {www.downloadHandler.text}");
                    
                    try
                    {
                        AuthResponse response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                        
                        if (response.success)
                        {
                            authToken = response.data.token;
                            userId = response.data.user.id;
                            
                            SavePlayerData();
                            
                            Debug.Log($"Token received (first 20 chars): {authToken.Substring(0, Mathf.Min(20, authToken.Length))}...");
                            Debug.Log($"User ID: {userId}");
                            
                            OnRegistrationComplete?.Invoke(true, response.message);
                        }
                        else
                        {
                            Debug.LogError($"Server returned error: {response.message}");
                            HandleRegistrationError(response.message);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"JSON Parse Error: {e.Message}");
                        Debug.LogError($"Raw response: {www.downloadHandler.text}");
                        HandleRegistrationError($"Parse error: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"HTTP Error: {www.error}");
                    Debug.LogError($"Status Code: {www.responseCode}");
                    Debug.LogError($"Response: {www.downloadHandler.text}");
                    
                    // Попробуем более простое имя при ошибке 500
                    if (www.responseCode == 500)
                    {
                        Debug.Log("Trying with simpler name...");
                        GenerateSimpleNameForRetry();
                        yield return new WaitForSeconds(1f);
                        yield return StartCoroutine(RegisterPlayer());
                    }
                    else
                    {
                        OnRegistrationComplete?.Invoke(false, $"HTTP Error: {www.error}");
                    }
                }
            }
        }

        // ДОБАВЛЕНО: Генерация очень простого имени для повторной попытки
        private void GenerateSimpleNameForRetry()
        {
            playerName = $"User{DateTime.UtcNow.Ticks % 1000000}";
            Debug.Log($"Generated retry name: {playerName}");
        }

        // ДОБАВЛЕНО: Обработка ошибок регистрации
        private void HandleRegistrationError(string error)
        {
            if (error.Contains("name") && error.Contains("taken"))
            {
                Debug.Log("Name already taken, generating new one...");
                GenerateSimpleUniqueName();
                StartCoroutine(RegisterPlayer());
            }
            else
            {
                OnRegistrationComplete?.Invoke(false, error);
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

        // МЕТОД ДЛЯ ТЕСТИРОВАНИЯ ВРУЧНУЮ
        public IEnumerator TestRegistrationWithName(string testName)
        {
            playerName = testName;
            Debug.Log($"Testing registration with name: {testName}");
            yield return StartCoroutine(RegisterPlayer());
        }
 

        // ДОБАВЛЕНЫ ПУБЛИЧНЫЕ ГЕТТЕРЫ
        public string PlayerName => playerName;
        public string AuthToken => authToken; // ← ВОТ ЭТО НУЖНО ДОБАВИТЬ
        public int UserId => userId;
        public bool IsRegistered => !string.IsNullOrEmpty(authToken);
        
        public IEnumerator SendAuthorizedRequest(string url, WWWForm form = null, 
            Action<string> onSuccess = null, Action<string> onError = null)
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
            
            www.SetRequestHeader("Authorization", $"Bearer {authToken}");
            www.SetRequestHeader("Accept", "application/json");
            
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Request successful: {www.downloadHandler.text}");
                onSuccess?.Invoke(www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"Request failed: {www.error}");
                Debug.LogError($"Response: {www.downloadHandler.text}");
                onError?.Invoke(www.error);
            }
        }

        public event Action<bool, string> OnRegistrationComplete;
    }
        
    
 
    
    
}