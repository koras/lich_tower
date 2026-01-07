using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionButton : MonoBehaviour
{
    [SerializeField] private string targetSceneName; // Имя сцены для перехода
    [SerializeField] private float delayBeforeTransition = 1f; // Задержка в секундах
    
    private Button button;
    private bool isPressed = false;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);
    }

    public void OnButtonClick()
    {
        if (isPressed) return; // Предотвращаем множественные нажатия
        
        isPressed = true;
        button.interactable = false; // Делаем кнопку неактивной
        
        // Меняем визуальное состояние (опционально)
        StartCoroutine(TransitionCoroutine());
    }

    private IEnumerator TransitionCoroutine()
    {
        // Ждем указанное время
        yield return new WaitForSeconds(delayBeforeTransition);
        
        // Переходим на другую сцену
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("Target scene name is not set!");
        }
    }

    // Для вызова из инспектора (если нужно)
    public void SetTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
    }
}