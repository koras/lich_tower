using UnityEngine; 
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Level
{
    public class ChangeLevel : MonoBehaviour
    {
        [Header("Переход на сцены")] 
        [SerializeField] private string sceneName = "PinchLevel"; 
        
        [Header("Кнопка")] [SerializeField] private Button _goSceneButton;
        private void Awake()
        {
            Debug.Log($"[ChangeLevel] Awake");
            _goSceneButton.onClick.AddListener(GoScene);
        }
        /// <summary>
        /// Вызывается при смерти героя
        /// </summary>
        public void GoScene()
        {
            Debug.Log($"[ChangeLevel] GoScene ");
            SceneManager.LoadScene(sceneName,  LoadSceneMode.Single);
        }
    }
}