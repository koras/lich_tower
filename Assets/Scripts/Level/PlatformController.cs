using UnityEngine;
using UnityEngine.UI;

public class PlatformController : MonoBehaviour
{
    [SerializeField] private RectTransform pcButtons;
    [SerializeField] private RectTransform mobileButtons;
    [SerializeField] private GameObject joystick;

    void Start()
    {
        #if UNITY_EDITOR
        // Для тестирования в редакторе можно симулировать разные платформы
          Debug.Log("Running in Editor");
        #endif

        CheckPlatform();
    }

    void CheckPlatform()
    {
        bool isMobile = false;
        
        // Проверка платформы
        switch (Application.platform)
        {
            case RuntimePlatform.Android:
            case RuntimePlatform.IPhonePlayer:
                isMobile = true;
                break;
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.LinuxPlayer:
                isMobile = false;
                break;
            case RuntimePlatform.WebGLPlayer:
                // Для WebGL можно проверить через UserAgent или сделать настройки
                isMobile = CheckIfMobileWeb();
                break;
        }

        SetupUI(isMobile);
    }

    void SetupUI(bool isMobile)
    {
        // Активируем/деактивируем элементы UI в зависимости от платформы
        if (pcButtons != null)
            pcButtons.gameObject.SetActive(!isMobile);
        
        if (mobileButtons != null)
            mobileButtons.gameObject.SetActive(isMobile);
        
        if (joystick != null)
            joystick.SetActive(isMobile);

        // Также можно менять положение элементов
        if (isMobile)
        {
            Debug.Log("Running isMobile");
            // Настройка для мобильных устройств
            // Например, перемещение кнопок
        }
        else
        {
            Debug.Log("Running PC");
            // Настройка для PC
        }
    }

    bool CheckIfMobileWeb()
    {
        // Для WebGL можно проверить UserAgent или размер экрана
        #if UNITY_WEBGL && !UNITY_EDITOR
        // Проверка через JavaScript
        return IsMobileUserAgent();
        #else
        return false;
        #endif
    }
}