using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class AnimatedSceneTransitionButton : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private float delayBeforeTransition = 0.5f;
    
    [Header("Animation Settings")]
    [SerializeField] private Animator buttonAnimator;
    [SerializeField] private string pressAnimation = "Pressed";
    [SerializeField] private string idleAnimation = "Normal";
    
    [Header("Sound Settings")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioSource audioSource;
    
    private Button button;
    private bool isPressed = false;
    private bool sceneIsValid = false;

    void Start()
    {
        button = GetComponent<Button>();
        
        // Получаем компоненты если они не назначены в инспекторе
        if (buttonAnimator == null)
            buttonAnimator = GetComponent<Animator>();
            
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        // Проверяем существование сцены
        sceneIsValid = CheckIfSceneExists(targetSceneName);
        
        if (!sceneIsValid)
        {
         //  Debug.LogError($"Scene '{targetSceneName}' is not in Build Settings!");
//            button.interactable = false;
        }
        else
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }

    private bool CheckIfSceneExists(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string nameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (nameFromPath == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    public void OnButtonClick()
    {
        if (isPressed || !sceneIsValid) return;
        
        isPressed = true;
        button.interactable = false;
        
        // Воспроизводим звук
        PlayClickSound();
        
        // Запускаем анимацию нажатия
        if (buttonAnimator != null && !string.IsNullOrEmpty(pressAnimation))
        {
            buttonAnimator.SetTrigger("Pressed");
        }
        
        StartCoroutine(TransitionCoroutine());
    }

    private void PlayClickSound()
    {
        if (clickSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clickSound, Camera.main.transform.position);
            }
        }
    }

    private IEnumerator TransitionCoroutine()
    {
        yield return new WaitForSeconds(delayBeforeTransition);
        
        if (sceneIsValid)
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    // Метод для сброса состояния кнопки (можно вызвать из анимации)
    public void ResetButtonState()
    {
        isPressed = false;
        button.interactable = true;
        
        if (buttonAnimator != null && !string.IsNullOrEmpty(idleAnimation))
        {
            buttonAnimator.SetTrigger("Normal");
        }
    }
}