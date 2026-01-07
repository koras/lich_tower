using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Heroes;
using Input; // PinchToZoomAndPan

/// <summary>
/// Кнопка способности "Fireball" для Лича.
/// Логика:
/// - Показывает "текущая мана / стоимость"
/// - Fill на кнопке = (currentMana / manaCost) [0..1]
/// - Кнопка активна только если маны хватает
/// - По нажатию включает режим прицеливания (BeginFireballTargeting)
/// ВАЖНО:
/// - Мана НЕ списывается здесь. Она списывается в Animation Event на Личе (как ты и хотел).
/// </summary>
namespace Ui
{
    public class ButtonLichFireballSkill : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private PinchToZoomAndPan _input;   // кто включает режим прицеливания
        [SerializeField] private HeroesBase _heroBase;       // Лич (отсюда берём ману)

        [Header("Mana")]
       // [SerializeField, Min(0)] 
        private int mannaCost = 80; // стоимость манны

        [Header("Mana UI")]
        [SerializeField] private TMP_Text _mannaText;        // "50 / 20"
        [SerializeField] private Image _mannaEnoughIcon;     // опционально: индикатор (может быть null)

        [SerializeField] private Color _enoughColor = Color.white;
        [SerializeField] private Color _notEnoughColor = new Color(1f, 0.4f, 0.4f, 1f);

        [Header("Button UI")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _cooldownFill;        // Image (Fill)
        [SerializeField] private Image _buttonGraphic;       // основной Image кнопки

        [Header("Not ready visuals")]
        [SerializeField] private Color _notReadyColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        [Header("Fill smoothing (optional)")]
        [SerializeField, Min(0f)] private float fillSmoothSpeed = 12f; // 0 = без сглаживания

        private Color _origGraphicColor;
        private ColorBlock _origColors;

        private float _fillVelocity; // для SmoothDamp, если захочешь
        private float _currentFillShown;

        private void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_buttonGraphic == null)
                _buttonGraphic = GetComponent<Image>(); // fallback

            if (_button != null)
                _origColors = _button.colors;

            if (_buttonGraphic != null)
                _origGraphicColor = _buttonGraphic.color;

            if (_cooldownFill != null)
            {
                _cooldownFill.fillAmount = 0f;
                _currentFillShown = _cooldownFill.fillAmount;
            }

            // если забыли назначить input, попробуем найти в сцене
            if (_input == null)
                _input = FindObjectOfType<PinchToZoomAndPan>();
        }

        private void OnEnable()
        {
            RefreshAllUI(immediate: true);
        }

        private void Update()
        {
            if (_heroBase == null) return;

            // Текст + индикаторы
            UpdateManaText();
            UpdateFillAndInteractable();
        }

        /// <summary>
        /// Вызывается кнопкой (OnClick).
        /// </summary>
        public void UseFireball()
        {
            if (_heroBase == null)
            {
                Debug.LogWarning("[ButtonLichFireballSkill] Не назначен HeroesBase Лича (_heroBase).");
                return;
            }

            if (_input == null)
            {
                Debug.LogWarning("[ButtonLichFireballSkill] Не назначен PinchToZoomAndPan (_input).");
                return;
            }

            if (!HasEnoughMana())
            {
                Debug.Log("Не хватает маны на Fireball.");
                return;
            }

            // Включаем режим прицеливания.
            // Списание маны делай в Animation Event на анимации Лича.
            _input.BeginFireballTargeting();
        }

        private bool HasEnoughMana()
        {
            Debug.Log("Проверяем манну");
            return _heroBase != null && _heroBase.HasManna(mannaCost);
        }

        private void UpdateManaText()
        {
            if (_mannaText == null) return;
            _mannaText.text = $"{_heroBase.GetCurrentManna()}/{_heroBase.GetMaxManna()}";

            if (_mannaEnoughIcon != null)
                _mannaEnoughIcon.color = (_heroBase.GetCurrentManna() >= mannaCost) ? _enoughColor : _notEnoughColor;
        }

        private void UpdateFillAndInteractable()
        {
            if (_cooldownFill == null && _button == null && _buttonGraphic == null)
                return;

            int now = _heroBase.CurrentManna;

            // Fill = текущая мана / стоимость (0..1)
            float targetFill = mannaCost <= 0 ? 1f : Mathf.Clamp01((float)now / mannaCost);

            // опционально сглаживаем (чтобы fill не "дёргался")
            if (_cooldownFill != null)
            {
                if (fillSmoothSpeed <= 0f)
                {
                    _currentFillShown = targetFill;
                }
                else
                {
                    _currentFillShown = Mathf.Lerp(_currentFillShown, targetFill, Time.deltaTime * fillSmoothSpeed);
                }

                _cooldownFill.fillAmount = _currentFillShown;
            }

            bool ready = now >= mannaCost;

            // Кнопка активна только если готово
            if (_button != null)
                _button.interactable = ready;

            // Визуал кнопки (серый когда не готово)
            if (_buttonGraphic != null)
                _buttonGraphic.color = ready ? _origGraphicColor : _notReadyColor;

            // Если хочешь ещё и цвета button state приглушать (не обязательно)
            if (_button != null)
            {
                var cb = _button.colors;
                if (ready)
                {
                    _button.colors = _origColors;
                }
                else
                {
                    cb.normalColor = _notReadyColor;
                    cb.highlightedColor = _notReadyColor;
                    cb.pressedColor = _notReadyColor;
                    cb.selectedColor = _notReadyColor;
                    _button.colors = cb;
                }
            }
        }

        private void RefreshAllUI(bool immediate)
        {
            if (_heroBase == null) return;

            UpdateManaText();

            if (_cooldownFill != null)
            {
                int now = _heroBase.CurrentManna;
                float fill = mannaCost <= 0 ? 1f : Mathf.Clamp01((float)now / mannaCost);
                _currentFillShown = fill;
                _cooldownFill.fillAmount = fill;
            }

            UpdateFillAndInteractable();
        }
    }
}
