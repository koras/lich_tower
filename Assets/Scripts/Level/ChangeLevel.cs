using UnityEngine;
using Heroes; 
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Level
{
    public class ChangeLevel : MonoBehaviour
    {
 
        [Header("Переход на сцены")] 
        [SerializeField] private string sceneName = "PinchLevel"; 
        /// <summary>
        /// Вызывается при смерти героя
        /// </summary>
        public void GoScene()
        {
         //   UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            SceneManager.LoadScene(sceneName);
        }
    }
}