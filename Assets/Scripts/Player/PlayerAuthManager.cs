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
        private static PlayerAuthManager _instance;
        private static object _lock = new object();
        
        public static PlayerAuthManager Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindObjectOfType<PlayerAuthManager>();
                            
                            if (_instance == null)
                            {
                                GameObject go = new GameObject("PlayerAuthManager");
                                _instance = go.AddComponent<PlayerAuthManager>();
                                DontDestroyOnLoad(go);
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private string playerName;
        [SerializeField] private string authToken;
        [SerializeField] private int userId;
        [SerializeField] private string _host;
         
        private const string PLAYER_NAME_KEY = "PlayerName";
        private const string AUTH_TOKEN_KEY = "AuthToken";
        private const string USER_ID_KEY = "UserId";
        private const string DEVICE_ID_KEY = "DeviceId";
        private const string IS_REGISTERED_KEY = "IsRegistered";
        private const string LAST_REGISTER_ATTEMPT_KEY = "LastRegisterAttempt";
        
        private bool isInitialized = false;
        private string deviceId;

        public string PlayerName => playerName;
        public string AuthToken => authToken;
        public int UserId => userId;
        public bool IsRegistered => !string.IsNullOrEmpty(authToken);
        
        public event Action<bool, string> OnRegistrationComplete;

        void Awake()
        {
            
            
              _host = new GameAPIService().getHost();
 
            
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (!isInitialized)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            isInitialized = true;
            
            // Генерируем или получаем уникальный Device ID
            deviceId = GetOrCreateDeviceId();
            Debug.Log($"Device ID: {deviceId}");
            
            LoadSavedData();
        }

        private string GetOrCreateDeviceId()
        {
            if (PlayerPrefs.HasKey(DEVICE_ID_KEY))
            {
                return PlayerPrefs.GetString(DEVICE_ID_KEY);
            }
            else
            {
                string newDeviceId = SystemInfo.deviceUniqueIdentifier;
                if (string.IsNullOrEmpty(newDeviceId))
                {
                    // Fallback для редактора
                    newDeviceId = "editor_" + DateTime.Now.Ticks.ToString();
                }
                
                PlayerPrefs.SetString(DEVICE_ID_KEY, newDeviceId);
                PlayerPrefs.Save();
                return newDeviceId;
            }
        }

        private void LoadSavedData()
        {
            bool hasSavedData = PlayerPrefs.HasKey(PLAYER_NAME_KEY) && 
                              PlayerPrefs.HasKey(AUTH_TOKEN_KEY) &&
                              PlayerPrefs.HasKey(USER_ID_KEY) &&
                              PlayerPrefs.HasKey(IS_REGISTERED_KEY);
            
            if (hasSavedData)
            {
                playerName = PlayerPrefs.GetString(PLAYER_NAME_KEY);
                authToken = PlayerPrefs.GetString(AUTH_TOKEN_KEY);
                userId = PlayerPrefs.GetInt(USER_ID_KEY);
                
                int isRegistered = PlayerPrefs.GetInt(IS_REGISTERED_KEY, 0);
                
                if (isRegistered == 1 && !string.IsNullOrEmpty(authToken))
                {
                    Debug.Log($"Loaded existing player: {playerName}, ID: {userId}");
                    
                    // Проверяем токен на валидность
                    StartCoroutine(ValidateAndProceed());
                }
                else
                {
                    Debug.Log("Saved player found but not properly registered. Re-registering...");
                    GeneratePersistentPlayerName();
                    StartCoroutine(RegisterPlayer());
                }
            }
            else
            {
                Debug.Log("No saved player data found. Creating new player...");
                GeneratePersistentPlayerName();
                StartCoroutine(RegisterPlayer());
            }
        }

        private void GeneratePersistentPlayerName()
        {
            // Используем Device ID как основу для генерации имени
            // Это гарантирует одинаковое имя на одном устройстве
            if (!string.IsNullOrEmpty(playerName) && PlayerPrefs.HasKey(PLAYER_NAME_KEY))
            {
                // Уже есть имя в PlayerPrefs, используем его
                playerName = PlayerPrefs.GetString(PLAYER_NAME_KEY);
                Debug.Log($"Using existing player name: {playerName}");
            }
            else
            {
                // Генерируем новое имя на основе Device ID
                int hash = deviceId.GetHashCode();
                int positiveHash = Mathf.Abs(hash) % 1000000;
                playerName = $"Player_{positiveHash}";
                
                Debug.Log($"Generated persistent player name: {playerName}");
                
                // Сохраняем сразу, чтобы использовать при следующем запуске
                PlayerPrefs.SetString(PLAYER_NAME_KEY, playerName);
                PlayerPrefs.Save();
            }
        }
private IEnumerator ValidateAndProceed444()
{
    string validateUrl = $"{_host}/api/profile";
    
    Debug.Log($"=== TOKEN VALIDATION START ===");
    Debug.Log($"URL: {validateUrl}");
    Debug.Log($"Auth Token (first 20 chars): {(authToken.Length > 20 ? authToken.Substring(0, 20) + "..." : authToken)}");
    Debug.Log($"Auth Token length: {authToken.Length}");
    Debug.Log($"Player ID: {userId}");
    
    using (UnityWebRequest www = UnityWebRequest.Get(validateUrl))
    {
        www.SetRequestHeader("Authorization", $"Bearer {authToken}");
        www.SetRequestHeader("Accept", "application/json");
        
        // Добавляем User-Agent для отладки
        www.SetRequestHeader("User-Agent", $"Unity/{Application.unityVersion} PlayerAuthManager");
        
        // Логируем все заголовки
        Debug.Log($"Request Headers:");
        Debug.Log($"- Authorization: Bearer [TOKEN]");
        Debug.Log($"- Accept: application/json");
        Debug.Log($"- User-Agent: {www.GetRequestHeader("User-Agent")}");
        
        Debug.Log($"Sending validation request...");
        float startTime = Time.time;
        
        yield return www.SendWebRequest();
        
        float responseTime = Time.time - startTime;
        
        Debug.Log($"=== VALIDATION RESPONSE ===");
        Debug.Log($"Response time: {responseTime:F2}s");
        Debug.Log($"Result: {www.result}");
        Debug.Log($"Response Code: {www.responseCode}");
        Debug.Log($"Response URL: {www.url}"); // Показывает финальный URL (после редиректов)
        
        // Логируем все заголовки ответа
        if (!string.IsNullOrEmpty(www.GetResponseHeader("Server")))
            Debug.Log($"Server: {www.GetResponseHeader("Server")}");
        
        if (!string.IsNullOrEmpty(www.GetResponseHeader("Content-Type")))
            Debug.Log($"Content-Type: {www.GetResponseHeader("Content-Type")}");
        
        if (!string.IsNullOrEmpty(www.GetResponseHeader("WWW-Authenticate")))
            Debug.Log($"WWW-Authenticate: {www.GetResponseHeader("WWW-Authenticate")}");
        
        // Логируем тело ответа (если есть)
        if (!string.IsNullOrEmpty(www.downloadHandler.text))
        {
            Debug.Log($"Response Body: {www.downloadHandler.text}");
            
            // Пытаемся разобрать JSON для более читаемого вывода
            try
            {
             //   .   var json = SimpleJSON.JSON.Parse(www.downloadHandler.text);
            //    Debug.Log($"Parsed JSON:\n{json.ToString(2)}");
            }
            catch
            {
                Debug.Log("Response is not valid JSON or not using SimpleJSON");
            }
        }
        else
        {
            Debug.Log("Response Body: EMPTY");
        }
        
        // Логируем полный URL с параметрами (если есть)
        Debug.Log($"Full Request Details:");
        Debug.Log($"- Method: GET");
        Debug.Log($"- URL: {validateUrl}");
        
        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"✅ Token validation SUCCESS!");
            Debug.Log($"Token is valid. Player ready!");
            OnRegistrationComplete?.Invoke(true, "Token validated");
        }
        else
        {
            Debug.LogError($"❌ Token validation FAILED!");
            Debug.LogError($"Error: {www.error}");
            
            // Более детальный анализ ошибки
            if (www.responseCode == 401)
            {
                Debug.LogError($"❌ 401 Unauthorized - Token is invalid or expired");
                Debug.LogError($"Possible reasons:");
                Debug.LogError($"1. Token has expired");
                Debug.LogError($"2. Token was revoked");
                Debug.LogError($"3. Invalid token format");
                Debug.LogError($"4. Server authentication middleware rejected token");
            }
            else if (www.responseCode == 404)
            {
                Debug.LogError($"❌ 404 Not Found - Endpoint doesn't exist");
                Debug.LogError($"Check if the URL is correct: {validateUrl}");
            }
            else if (www.responseCode == 500)
            {
                Debug.LogError($"❌ 500 Internal Server Error");
                Debug.LogError($"Server-side error. Check server logs.");
            }
            
            Debug.LogWarning($"Token validation failed: {www.error}. Re-authenticating...");
            
            // Сохраняем старые данные для отладки
            string oldToken = authToken;
            string oldName = playerName;
            int oldUserId = userId;
            
            Debug.Log($"Old user data for debugging:");
            Debug.Log($"- Name: {oldName}");
            Debug.Log($"- User ID: {oldUserId}");
            Debug.Log($"- Token: {(oldToken.Length > 30 ? oldToken.Substring(0, 30) + "..." : oldToken)}");
            
            // Попробуем перерегистрироваться с тем же именем
            if (string.IsNullOrEmpty(playerName))
            {
                GeneratePersistentPlayerName();
            }
            else
            {
                Debug.Log($"Will try to re-register with existing name: {playerName}");
            }
            
            // Добавляем небольшую задержку перед повторной регистрацией
            yield return new WaitForSeconds(1f);
            
            yield return StartCoroutine(RegisterPlayer());
        }
        
        Debug.Log($"=== TOKEN VALIDATION END ===");
    }
}
        private IEnumerator ValidateAndProceed()
        {

            string validateUrl = $"{_host}/api/profile";
            
            using (UnityWebRequest www = UnityWebRequest.Get(validateUrl))
            {
                www.SetRequestHeader("Authorization", $"Bearer {authToken}");
                www.SetRequestHeader("Accept", "application/json");
                
                yield return www.SendWebRequest();

                Debug.Log($"Token is valid. { www.result.ToString()}"  );
                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Token is valid. Player ready!");
                    OnRegistrationComplete?.Invoke(true, "Token validated");
                }
                else
                {
                    Debug.LogWarning($"Token validation failed: {www.error}. Re-authenticating...");
                    
                    // Попробуем перерегистрироваться с тем же именем
                    if (string.IsNullOrEmpty(playerName))
                    {
                        GeneratePersistentPlayerName();
                    }
                    
                    yield return StartCoroutine(RegisterPlayer());
                }
            }
        }

        private IEnumerator RegisterPlayer()
        {
            // Защита от слишком частых попыток регистрации
            if (ShouldDelayRegistration())
            {
                Debug.Log("Registration delayed to prevent spam");
                yield return new WaitForSeconds(5f);
            }
            
            string serverUrl = $"{_host}/api/auth/register";
            WWWForm form = new WWWForm();
            form.AddField("name", playerName);
            
            // Добавляем device_id для отслеживания
            form.AddField("device_id", deviceId);
            
            using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
            {
                www.SetRequestHeader("Accept", "application/json");
                www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                
                Debug.Log($"Registering player: {playerName}");
                
                yield return www.SendWebRequest();
                
                // Сохраняем время попытки
                PlayerPrefs.SetString(LAST_REGISTER_ATTEMPT_KEY, DateTime.UtcNow.ToString("o"));
                PlayerPrefs.Save();
                
                if (www.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        AuthResponse response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                        
                        if (response.success)
                        {
                            authToken = response.data.token;
                            userId = response.data.user.id;
                            
                            SavePlayerData();
                            
                            Debug.Log($"Registration successful! User ID: {userId}");
                            OnRegistrationComplete?.Invoke(true, "Registration successful");
                        }
                        else
                        {
                            Debug.LogError($"Registration failed: {response.message}");
                            HandleRegistrationError(response.message);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"JSON Parse Error: {e.Message}");
                        OnRegistrationComplete?.Invoke(false, $"Parse error: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"HTTP Error: {www.error}");
                    
                    // При ошибке 500 пробуем другое имя
                    if (www.responseCode == 500)
                    {
                        GenerateAlternativePlayerName();
                        yield return new WaitForSeconds(2f);
                        yield return StartCoroutine(RegisterPlayer());
                    }
                    else
                    {
                        OnRegistrationComplete?.Invoke(false, $"HTTP Error: {www.error}");
                    }
                }
            }
        }

        private bool ShouldDelayRegistration()
        {
            if (PlayerPrefs.HasKey(LAST_REGISTER_ATTEMPT_KEY))
            {
                string lastAttemptStr = PlayerPrefs.GetString(LAST_REGISTER_ATTEMPT_KEY);
                if (DateTime.TryParse(lastAttemptStr, out DateTime lastAttempt))
                {
                    TimeSpan timeSinceLastAttempt = DateTime.UtcNow - lastAttempt;
                    return timeSinceLastAttempt.TotalSeconds < 5; // Задержка 5 секунд
                }
            }
            return false;
        }

        private void GenerateAlternativePlayerName()
        {
            // Генерируем альтернативное имя с timestamp
            long timestamp = DateTime.UtcNow.Ticks;
            playerName = $"User_{deviceId.GetHashCode() % 10000}_{timestamp % 100000}";
            Debug.Log($"Generated alternative name: {playerName}");
        }

        private void HandleRegistrationError(string error)
        {
            if (error.Contains("name") && error.Contains("taken"))
            {
                Debug.Log("Name already taken, generating new one...");
                GenerateAlternativePlayerName();
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
            
            Debug.Log("Player data saved successfully");
        }

        public void ClearLocalData()
        {
            PlayerPrefs.DeleteKey(PLAYER_NAME_KEY);
            PlayerPrefs.DeleteKey(AUTH_TOKEN_KEY);
            PlayerPrefs.DeleteKey(USER_ID_KEY);
            PlayerPrefs.DeleteKey(IS_REGISTERED_KEY);
            PlayerPrefs.Save();
            
            playerName = "";
            authToken = "";
            userId = 0;
            
            Debug.Log("Local player data cleared!");
            
            // Генерируем новое имя и регистрируемся
            GeneratePersistentPlayerName();
            StartCoroutine(RegisterPlayer());
        }

        public IEnumerator SendAuthorizedRequest(string url, WWWForm form = null, 
            Action<string> onSuccess = null, Action<string> onError = null)
        {
            if (!IsRegistered)
            {
                Debug.LogError("Cannot send request: player not registered");
                onError?.Invoke("Player not registered");
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
                onSuccess?.Invoke(www.downloadHandler.text);
            }
            else
            {
                // Если токен истек, пытаемся перерегистрироваться
                if (www.responseCode == 401)
                {
                    Debug.Log("Token expired, re-registering...");
                    StartCoroutine(RegisterPlayer());
                }
                onError?.Invoke(www.error);
            }
        }
    }
}