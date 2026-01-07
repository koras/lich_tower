using UnityEngine;
using UnityEngine.EventSystems;  
public class SwipeController : MonoBehaviour, IEndDragHandler
{
    [Header("Страницы")] 
    [SerializeField] private int maxPage;
    private int currentPage;
    private Vector3 targetPos;
    
    [SerializeField] private Vector3 pageStep;
    [SerializeField] private RectTransform levelPagesRect;
    [SerializeField] private float tweenTime;
  //  [SerializeField] private LeanTweenType tweenType;

    public float dragThreshold;
    private void Awake()
    {
        currentPage = 1;
        targetPos = levelPagesRect.localPosition;
        dragThreshold = Screen.width / 15;
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        if (Mathf.Abs(eventData.position.x - eventData.pressPosition.x) > dragThreshold)
        {
            if(eventData.position.x > eventData.pressPosition.x) Previous();
            else Next();
        }
        else
        {
            MovePage();
        }
    }

    public void Next()
    {
        if (currentPage < maxPage)
        {
            currentPage++;
            targetPos += pageStep;
            MovePage();
        }
    }

    public void Previous()
    {
        if (currentPage > 1)
        {
            currentPage--;
            targetPos -= pageStep;
            MovePage();
        }
    }

    private void MovePage()
    {
        // Останавливаем все текущие анимации перед запуском новой
     //   LeanTween.cancel(levelPagesRect.gameObject);
        
     //   levelPagesRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType);
    }

    public int GetCurrentPage()
    {
        return currentPage;
    }

    public bool IsOnFirstPage()
    {
        return currentPage == 1;
    }

    public bool IsOnLastPage()
    {
        return currentPage == maxPage;
    }
}