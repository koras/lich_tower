using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System; 

namespace Player
{
    [System.Serializable]
    public class SessionResponse
    {
        public bool success;
        public string message;
        public SessionData data;
    }

    [System.Serializable]
    public class SessionData
    {
        public string user_id;
        public string game;
        public string started_at;
        public string status;
        public string updated_at;
        public string created_at;
        public int id;
    }

    public class GameAPIService : MonoBehaviour
    {
        public static GameAPIService Instance { get; private set; }

        private Queue<APITask> pendingTasks = new Queue<APITask>();
        private bool isProcessing = false;
        
        // Текущая активная сессия
        private int currentSessionId = -1;
        private const string SESSION_ID_KEY = "CurrentSessionId";
        private const string SESSION_START_TIME_KEY = "SessionStartTime";
        
        // Событие при создании сессии
        public event System.Action<int> OnSessionStarted;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Загружаем сохраненную сессию, если есть
                if (PlayerPrefs.HasKey(SESSION_ID_KEY))
                {
                    currentSessionId = PlayerPrefs.GetInt(SESSION_ID_KEY);
                    Debug.Log($"Loaded saved session: {currentSessionId}");
                    
                    
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private struct APITask
        {
            public string url;
            public WWWForm form;
            public System.Action<string> onSuccess;
            public System.Action<string> onError;
        }

        // === СЕССИОННЫЕ МЕТОДЫ ===
        
        /// <summary>
        /// Начать новую игровую сессию
        /// </summary>
        public IEnumerator StartGameSession( string status = "started",string gameName = "demo")
        {
            // Проверяем авторизацию
            if (!PlayerAuthManager.Instance.IsRegistered)
            {
                Debug.LogError("Cannot start session: player not registered");
                yield break;
            }

            string url = "http://localhost:8881/api/sessions/start";
            string token = PlayerAuthManager.Instance.AuthToken;
            string userId = PlayerAuthManager.Instance.UserId.ToString();

            WWWForm form = new WWWForm();
            form.AddField("user_id", userId);
            form.AddField("game", gameName);
            form.AddField("status", status);

            using (UnityWebRequest www = UnityWebRequest.Post(url, form))
            {
                www.SetRequestHeader("Authorization", $"Bearer {token}");
                www.SetRequestHeader("Accept", "application/json");

                Debug.Log($"Starting game session for user {userId}...");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        SessionResponse response = JsonUtility.FromJson<SessionResponse>(www.downloadHandler.text);
                        
                        if (response.success)
                        {
                            // Сохраняем ID сессии
                            currentSessionId = response.data.id;
                            
                            Debug.Log($"Starting game session currentSessionId {currentSessionId}");
                            // Сохраняем локально
                            PlayerPrefs.SetInt(SESSION_ID_KEY, currentSessionId);
                            PlayerPrefs.SetString(SESSION_START_TIME_KEY, response.data.started_at);
                            PlayerPrefs.Save();
                            
                            Debug.Log($"Game session started! Session ID: {currentSessionId}");
                            
                            // Вызываем событие
                            OnSessionStarted?.Invoke(currentSessionId);
                        }
                        else
                        {
                            Debug.LogError($"Failed to start session: {response.message}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Session parse error: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"Session start failed: {www.error}");
                }
            }
        }
        public IEnumerator SendFinalStats(Player.StatsPayload payload)
        {
            if (!PlayerAuthManager.Instance.IsRegistered)
                yield break;

           // var url = "http://localhost:8881/api/stats/final";
            var url = "http://localhost:8881/api/game/stats/save";
            
            

            var token = PlayerAuthManager.Instance.AuthToken;

            WWWForm form = new WWWForm();
            form.AddField("session_id", payload.session_id.ToString());
            form.AddField("session_id", payload.session_id.ToString());
            form.AddField("team", payload.team.ToString());
            form.AddField("is_win", payload.is_win ? "1" : "0");
            form.AddField("total_damage", payload.total_damage.ToString());
            form.AddField("total_kills", payload.total_kills.ToString());
            form.AddField("timestamp_utc", payload.timestamp_utc);
            form.AddField("hash", payload.hash);

            // сериализуем списки как json-строку
            form.AddField("damage_by_hero_json", JsonUtility.ToJson(new WrapList<Player.KeyInt>(payload.damage_by_hero)));
            form.AddField("kills_by_hero_json", JsonUtility.ToJson(new WrapList<Player.KeyInt>(payload.kills_by_hero)));

            using (var www = UnityWebRequest.Post(url, form))
            {
                www.SetRequestHeader("Authorization", $"Bearer {token}");
                www.SetRequestHeader("Accept", "application/json");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                    Debug.Log("Final stats sent: " + www.downloadHandler.text);
                else
                    Debug.LogError("Final stats send failed: " + www.error);
            }
        }

        [Serializable]
        public class WrapList<T>
        {
            public List<T> items;
            public WrapList(List<T> items) { this.items = items; }
        }
        
        
        
        /// <summary>
        /// Завершить текущую сессию
        /// </summary>
        public IEnumerator EndGameSession(string status = "completed")
        {
            if (currentSessionId <= 0)
            {
                Debug.LogWarning("No active session to end");
                yield break;
            }

            string url = $"http://localhost:8881/api/game/{currentSessionId}/end";
            string token = PlayerAuthManager.Instance.AuthToken;

            WWWForm form = new WWWForm();
            form.AddField("status", status);

            using (UnityWebRequest www = UnityWebRequest.Post(url, form))
            {
                www.SetRequestHeader("Authorization", $"Bearer {token}");
                www.SetRequestHeader("Accept", "application/json");

                Debug.Log($"Ending session {currentSessionId}...");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Session {currentSessionId} ended successfully");
                    
                    // Очищаем данные сессии
                    ClearSessionData();
                }
                else
                {
                    Debug.LogError($"Failed to end session: {www.error}");
                }
            }
        }

        /// <summary>
        /// Очистить данные сессии (при перезапуске или ошибке)
        /// </summary>
        public void ClearSessionData()
        {
            currentSessionId = -1;
            PlayerPrefs.DeleteKey(SESSION_ID_KEY);
            PlayerPrefs.DeleteKey(SESSION_START_TIME_KEY);
            PlayerPrefs.Save();
            Debug.Log("Session data cleared");
        }

        // === ОБНОВЛЕННЫЕ МЕТОДЫ ДЛЯ ОТПРАВКИ СЕССИИ ===
        
        /// <summary>
        /// Отправка урона с автоматическим добавлением session_id
        /// </summary>
        public void SendDamageEvent(int bossId, int damage, int damageType = 1)
        {
            if (currentSessionId <= 0)
            {
                Debug.LogWarning("Cannot send damage: no active session. Starting new session...");
                StartCoroutine(StartSessionAndSendDamage(bossId, damage, damageType));
                return;
            }

            WWWForm form = CreateDamageForm(bossId, damage, damageType);
            
            SendRequest("damage", form,
                response => Debug.Log($"Damage sent for session {currentSessionId}: {response}"),
                error => Debug.LogError($"Damage send failed: {error}")
            );
        }

        private IEnumerator StartSessionAndSendDamage(int bossId, int damage, int damageType)
        {
            // Сначала запускаем сессию
            yield return StartGameSession();
            
            // Затем отправляем урон, если сессия создана
            if (currentSessionId > 0)
            {
                WWWForm form = CreateDamageForm(bossId, damage, damageType);
                yield return SendAuthorizedRequestCoroutine("damage", form, null, null);
            }
        }

        private WWWForm CreateDamageForm(int bossId, int damage, int damageType)
        {
            WWWForm form = new WWWForm();
            form.AddField("type", damageType.ToString());
            form.AddField("power", damage.ToString());
            form.AddField("user_id", PlayerAuthManager.Instance.UserId.ToString());
            form.AddField("session_id", currentSessionId.ToString());
            form.AddField("timestamp", System.DateTime.UtcNow.ToString("o"));
            
            // Опционально: добавляем boss_id, если требуется
            // form.AddField("boss_id", bossId.ToString());
            
            return form;
        }
        // Обновленный основной метод отправки запроса с сессией
        public void SendRequest(string endpoint, WWWForm form,
            System.Action<string> onSuccess = null,
            System.Action<string> onError = null)
        {
            // Всегда добавляем session_id, если есть активная сессия
            if (currentSessionId > 0)
            {
                // Создаем новую форму, чтобы добавить session_id
                WWWForm newForm = new WWWForm();
                
                // Копируем существующие поля из старой формы
                // (В Unity нет прямого способа получить поля из WWWForm,
                // поэтому мы должны хранить поля отдельно или использовать другой подход)
                
                // Для простоты, предполагаем что форма уже содержит все нужные поля
                // И добавляем session_id вручную при создании формы
                // (см. методы CreateDamageForm выше)
            }

            if (!PlayerAuthManager.Instance.IsRegistered)
            {
                pendingTasks.Enqueue(new APITask
                {
                    url = endpoint,
                    form = form,
                    onSuccess = onSuccess,
                    onError = onError
                });
                Debug.Log($"Task queued (not registered): {endpoint}");
                return;
            }

            StartCoroutine(SendAuthorizedRequestCoroutine(
                endpoint, form, onSuccess, onError
            ));
        }

        private IEnumerator SendAuthorizedRequestCoroutine(string endpoint, WWWForm form,
            System.Action<string> onSuccess, System.Action<string> onError)
        {
            string url = $"http://localhost:8881/api/{endpoint}";
            string token = PlayerAuthManager.Instance.AuthToken;

            using (UnityWebRequest www = UnityWebRequest.Post(url, form))
            {
                www.SetRequestHeader("Authorization", $"Bearer {token}");
                www.SetRequestHeader("Accept", "application/json");

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(www.downloadHandler.text);
                }
                else
                {
                    onError?.Invoke(www.error);
                    Debug.LogError($"API Error ({endpoint}): {www.error}");
                }
            }
        }

        // Специализированный метод для отправки убийств с сессией
        public void SendKillEvent(string enemyType, int enemyId, Vector3 position)
        {
            WWWForm form = new WWWForm();
            form.AddField("enemy_type", enemyType);
            form.AddField("enemy_id", enemyId.ToString());
            form.AddField("position_x", position.x.ToString("F2"));
            form.AddField("position_y", position.y.ToString("F2"));
            form.AddField("position_z", position.z.ToString("F2"));
            form.AddField("timestamp", System.DateTime.UtcNow.ToString("o"));

            SendRequest("kill", form,
                response => Debug.Log($"Kill recorded: {response}"),
                error => Debug.LogError($"Kill record failed: {error}")
            );
        }

        // Получить текущий ID сессии
        public int GetCurrentSessionId() => currentSessionId;
        
        // Проверить есть ли активная сессия
        public bool HasActiveSession() => currentSessionId > 0;
    }
}