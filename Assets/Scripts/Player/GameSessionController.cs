using UnityEngine;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
namespace Player
{
    public class CombatSystem : MonoBehaviour
    {
        // Пример 1: Простая отправка урона
        public void DealDamage(int damageAmount)
        {
            if (GameAPIService.Instance != null)
            {
                GameAPIService.Instance.SendDamageEvent(damageAmount, 1);
            }
        }

        // // Пример 2: Отправка урона по боссу
        // public void DealBossDamage(int bossId, int damageAmount)
        // {
        //     if (GameAPIService.Instance != null)
        //     {
        //         GameAPIService.Instance.SendBossDamage(bossId, damageAmount, 1);
        //     }
        // }
        //
        // // Пример 3: Отправка кастомного события
        // public void SendCustomEvent()
        // {
        //     if (GameAPIService.Instance != null)
        //     {
        //         GameAPIService.Instance.SendGameEvent("player_level_up", "Level: 5");
        //     }
        // }
    }

// Управление сессией
    public class GameSessionController : MonoBehaviour
    {
        void Start()
        {
            // Запускаем сессию при старте уровня
            StartCoroutine(StartSession());
            
            
        }

        IEnumerator StartSession()
        {
            yield return new WaitForSeconds(1f); // Ждем инициализации
        
            Debug.Log($"Starting");
            if (PlayerAuthManager.Instance != null && PlayerAuthManager.Instance.IsRegistered)
            {
                Debug.Log($"Starting 1");
                yield return GameAPIService.Instance.StartGameSession();
            }
        }

        // void OnDestroy()
        // {
        //     // Завершаем сессию при выходе
        //     if (GameAPIService.Instance != null && GameAPIService.Instance.HasActiveSession())
        //     {
        //         StartCoroutine(GameAPIService.Instance.StartGameSession("finished"));
        //     }
        // }
    }
}