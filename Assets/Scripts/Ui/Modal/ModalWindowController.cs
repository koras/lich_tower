using UnityEngine; 
using UnityEngine.UI;
using System.Collections;
public class ModalWindowController : MonoBehaviour
{ 
    
    [Header("Просто название")] 
    
    [Header("References")]
    [SerializeField] private CanvasGroup modalCanvasGroup;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup modalPanel;
    
    
  //  [Header("Чем скрываем")]
  //  [SerializeField] private RectTransform backgroundWindow;

    [Header("Animation Settings")]
  //  [SerializeField] private float fadeDuration = 1f;
  //  [SerializeField] private float scaleDuration = 1f;

    private bool isModalOpen = false;
    private Coroutine currentAnimation;

    private void Start()
    {
        // Инициализация
        modalCanvasGroup.alpha = 0f;
        modalCanvasGroup.blocksRaycasts = false;
        modalCanvasGroup.interactable = false;

        // Назначение обработчиков
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleModal);
        closeButton.onClick.AddListener(HideModal);
        
        // Закрытие по клику на фон (опционально)
        if (modalCanvasGroup.TryGetComponent<Button>(out var backgroundButton))
        {
            Debug.Log($"modalCanvasGroup");
            backgroundButton.onClick.AddListener(HideModal);
        }
        else
        {
            Debug.Log($"modalCanvasGroup is null");
        }

        
         
        modalPanel.blocksRaycasts = false;
        modalPanel.interactable = false;
    //    if (backgroundWindow != null)
      //  {
            Debug.Log($"backgroundWindow");
       //    backgroundWindow.gameObject.active = false;
     //   }
     //   else
     //   {
            
     //       Debug.Log($"backgroundWindow is null");
     //   }
    }
    public void ToggleModal()
    {
        if (isModalOpen)
            HideModal();
        else
            ShowModal();
    }
    public void ShowModal()
    {
        Debug.Log("ShowModal called");
        
        if (modalCanvasGroup == null)
        {
            Debug.LogError("ModalCanvasGroup is not assigned!");
            return;
        }

        isModalOpen = true;

        // Активируем объекты
        modalCanvasGroup.gameObject.SetActive(true);
        
     //   if (backgroundWindow != null)
     //       backgroundWindow.gameObject.SetActive(true);

        // Устанавливаем свойства видимости и взаимодействия
        modalCanvasGroup.alpha = 1f;
        modalCanvasGroup.blocksRaycasts = true;
        modalCanvasGroup.interactable = true;

        modalPanel.gameObject.SetActive(true);
        modalPanel.blocksRaycasts = true;
        modalPanel.interactable = true;
        modalPanel.alpha = 1f;

        
        // if (modalPanel != null)
        // {
        //     Debug.Log("modalPanel != null");
        //     modalPanel.localScale = Vector3.one;
        // }
        // else
        // {
        //     Debug.Log("modalPanel");
        // }
        Debug.Log("Modal should be visible now");
    }

    public void HideModal()
    {
        Debug.Log($"HideModal");
        isModalOpen = false;

        modalCanvasGroup.alpha = 0f;
        modalCanvasGroup.blocksRaycasts = false;
        modalCanvasGroup.interactable = false;

        modalCanvasGroup.gameObject.SetActive(false);

        
        modalPanel.gameObject.SetActive(false);
        modalPanel.blocksRaycasts = false;
        modalPanel.interactable = false;
        modalPanel.alpha = 1f;
        modalPanel.gameObject.SetActive(false);
        
        
  //      if (backgroundWindow != null)
  //          backgroundWindow.gameObject.SetActive(false);
    }

 
 
}