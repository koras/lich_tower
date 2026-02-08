using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Heroes;
using Input; // PinchToZoomAndPan
using  UnityEngine.EventSystems;
/// <summary>
/// Кнопка способности "Fireball" для Лича.
/// Логика:
/// - Показывает "текущая мана / стоимость"
/// - Fill на кнопке = (currentMana / manaCost) [0..1]
/// - Кнопка активна только если маны хватает
/// - Кнопка полностью скрывается (SetActive) если маны недостаточно
/// - По нажатию включает режим прицеливания (BeginFireballTargeting)
/// ВАЖНО:
/// - Мана НЕ списывается здесь. Она списывается в Animation Event на Личе (как ты и хотел).
/// </summary>
namespace Ui
{
    public class ButtonLichFireballSkill : MonoBehaviour, IPointerDownHandler
    {
        [Header("Refs")]
        [SerializeField] private PinchToZoomAndPan _input;   // кто включает режим прицеливания
        [SerializeField] private HeroesBase _heroBase;       // Лич (отсюда берём ману)

         private int mannaCost = 100; // стоимость манны
         [SerializeField] private CanvasGroup _canvasGroup; // добавь на корень кнопки
         private bool _isAimingLock; // чтобы не спамить нажатиями
         
        [Header("Visibility Settings")]
        [SerializeField] private bool hideWhenNotEnoughMana = true; // скрывать кнопку при недостатке маны
        [SerializeField] private GameObject buttonGameObject; // сам GameObject кнопки (если не задан, берется this.gameObject)

        [Header("Mana UI")]
        [SerializeField] private TMP_Text _mannaText;        // "50 / 20"
        [SerializeField] private Image _mannaEnoughIcon;     // опционально: индикатор (может быть null)

        
        [Header("Button UI")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _cooldownFill;        // Image (Fill)
        [SerializeField] private Image _buttonGraphic;       // основной Image кнопки

        
        [Header("Fill smoothing (optional)")]
        [SerializeField, Min(0f)] private float fillSmoothSpeed = 12f; // 0 = без сглаживания

        private Color _origGraphicColor;
        private ColorBlock _origColors;

        
        
        [Header("Button root (hide/show)")]
         
        
        private float _fillVelocity; // для SmoothDamp, если захочешь
        private float _currentFillShown;
        private bool _isEnoughManaLastFrame = false;
        private bool _isActiveState = true;

        private void Awake()
        {
            if (_input == null) _input = FindObjectOfType<PinchToZoomAndPan>();
            if (buttonGameObject == null) buttonGameObject = gameObject;
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        }

        // private void OnEnable()
        // {
        //     RefreshAllUI(immediate: true);
        //     _isEnoughManaLastFrame = false; // Сбросить состояние при включении
        // }

        private void Update()
        {
            if (_heroBase == null) return;

            bool canShow = _heroBase.HasManna(mannaCost) && !_isAimingLock;

            // вместо SetActive:
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = canShow ? 1f : 0f;
                _canvasGroup.blocksRaycasts = canShow;
                _canvasGroup.interactable = canShow;
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log($"[ButtonLichFireballSkill] PointerDown id={eventData.pointerId} pos={eventData.position}");

            if (_isAimingLock) return;
            if (_heroBase == null) return;
            if (_input == null) return;
            if (!_heroBase.HasManna(mannaCost)) return;

            _isAimingLock = true;

            HideButton(true);

            _input.BeginFireballTargetingFromUIButton(
                eventData.pointerId,
                eventData.position,
                () =>
                {
                    _isAimingLock = false;
                    HideButton(false);
                }
            );
        }
        private void HideButton(bool hide)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = hide ? 0f : 1f;
                _canvasGroup.blocksRaycasts = !hide;
                _canvasGroup.interactable = !hide;
            }
            else
            {
                buttonGameObject.SetActive(!hide);
            }
        }
        
        /// <summary>
        /// Вызывается кнопкой (OnClick).
        /// </summary>
        // public void UseFireball()
        // {
        //     Debug.LogWarning("[ButtonLichFireballSkill] UseFireball");
        //     if (_heroBase == null)
        //     {
        //         Debug.LogWarning("[ButtonLichFireballSkill] Не назначен HeroesBase Лича (_heroBase).");
        //         return;
        //     }
        //
        //     if (_input == null)
        //     {
        //         Debug.LogWarning("[ButtonLichFireballSkill] Не назначен PinchToZoomAndPan (_input).");
        //         return;
        //     }
        //
        //     if (!HasEnoughMana())
        //     {
        //         Debug.Log("Не хватает маны на Fireball.");
        //         return;
        //     }
        //     
        //     _heroBase.SpendManna(mannaCost);
        //     // Включаем режим прицеливания.
        //     // Списание маны делай в Animation Event на анимации Лича.
        //     _input.BeginFireballTargeting();
        //     
        //      
        // }

        private bool HasEnoughMana() => _heroBase != null && _heroBase.HasManna(mannaCost);
 
        /// <summary>
        /// Принудительно показать/скрыть кнопку (например, для внешнего контроля)
        /// </summary>
        public void SetButtonVisible(bool visible)
        {
            buttonGameObject.SetActive(visible);
            _isActiveState = visible;
            
            // Если показываем, обновляем состояние маны
            if (visible)
            {
                _isEnoughManaLastFrame = HasEnoughMana();
            }
        }

        
    }
}