using UnityEngine;
using UnityEngine.UI;
using Level;
using System.Collections;
using Player;
using Config;
using TMPro; // если используете TextMeshPro

public class ButtonHeroesSkill : MonoBehaviour
{
    [Header("Платформа для спауна")]
    [SerializeField] private SpawnInHero _spawnPlatform;

    [Header("Кого спаунить")]
    [SerializeField] private SpawnInHero.State spawnType = SpawnInHero.State.Skeleton;

    
    [Header("Банк учёта золота")]
    [SerializeField] private GoldBank _goldBank; 
    
    
     
    [SerializeField] private TMP_Text _costText;
    [Header("Сама кнопка")]
    [SerializeField] private Button _button;
    [Header("Image c Fill")]
    [SerializeField] private Image _cooldownFill; // Image c Fill Method
    [Header("основной визуал кнопки")]
    [SerializeField] private Image _buttonGraphic; // основной визуал кнопки (Image на самой кнопке)
    
    [Header("Кулдаун")]
    [SerializeField, Min(0.1f)] private float _cooldownTime = 3f;

    [Header("Анимация кулдауна")]
    [SerializeField, Range(0f, 0.5f)] private float _pulseScale = 0.08f;  // амплитуда пульса по масштабу
    [SerializeField, Min(0.1f)] private float _pulseSpeed = 6f;           // скорость пульса
    [SerializeField, Range(0f, 1f)] private float _blinkStrength = 0.25f; // насколько «тускнеть» в пике
    [SerializeField] private Color _cooldownColor = new Color(0.7f, 0.7f, 0.7f, 1f); // оттенок на кулдауне
    [SerializeField] private AnimationCurve _fillEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool _isCooldown;
    private Vector3 _originalScale;
    private Color _origGraphicColor;
    private ColorBlock _origColors;

    
    private void Start()
    {
        UpdateCostUI();
    }
    
    
    private void Awake()
    {
        if (_spawnPlatform == null)
            _spawnPlatform = FindObjectOfType<SpawnInHero>();
        if (_spawnPlatform == null)
            Debug.LogError($"[{name}] Не найдена SpawnPlatform в сцене!");

        if (_button == null)
            _button = GetComponent<Button>();

        if (_buttonGraphic == null)
            _buttonGraphic = GetComponent<Image>(); // fallback

        _originalScale = transform.localScale;
        _origColors = _button.colors;

        if (_buttonGraphic != null)
            _origGraphicColor = _buttonGraphic.color;

        if (_cooldownFill != null)
            _cooldownFill.fillAmount = 0f;
    }
    
 
    private void OnValidate()
    {
        if (!Application.isPlaying)
            UpdateCostUI();
    }
    private void UpdateCostUI()
    {
        if (_costText == null)
            return;

        int cost = HeroCostStorage.GetCost(spawnType);
        _costText.text = cost.ToString();
    }
    
    
    public int getCost()
    {
        return _spawnPlatform.getCost(spawnType);
    }


    public void SpawnHeroes()
    {
        
        if (_isCooldown)
            return;

        if (_spawnPlatform == null)
        {
            Debug.LogError($"[{name}] Не назначен SpawnPlatform для кнопки");
            return;
        }

        if (_goldBank == null)
        {
            Debug.LogError("HeroSpawner: не задан GoldBank");
            return;
        }

        int heroCost = _spawnPlatform.getCost(spawnType);
        Debug.Log($"Герой {spawnType} стоит {heroCost}");

        if (!_goldBank.TrySpend(heroCost))
        {
            Debug.Log("Не хватает золота на героя");
            return;
        }

        var hero = _spawnPlatform.InvokeSpawn(spawnType);
        if (hero != null)
        {
            StartCoroutine(CooldownRoutine());
        }
    }

    private IEnumerator CooldownRoutine()
    {
        _isCooldown = true;
        _button.interactable = false;

        // Подготовка визуала
        if (_cooldownFill != null) _cooldownFill.fillAmount = 1f;

        // Сразу притушим цвета ховера/нажатия, чтобы не мигало
        var cb = _button.colors;
        cb.normalColor = _cooldownColor;
        cb.highlightedColor = _cooldownColor;
        cb.pressedColor = _cooldownColor;
        cb.selectedColor = _cooldownColor;
        _button.colors = cb;

        // основной цвет графики
        if (_buttonGraphic != null)
            _buttonGraphic.color = _cooldownColor;

        // Запускаем пульсацию
        var pulse = StartCoroutine(PulseRoutine());

        float elapsed = 0f;
        while (elapsed < _cooldownTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _cooldownTime);
            float eased = _fillEase.Evaluate(t);

            if (_cooldownFill != null)
                _cooldownFill.fillAmount = 1f - eased;

            yield return null;
        }

        // Останавливаем пульс и возвращаем всё как было
        if (pulse != null) StopCoroutine(pulse);
        transform.localScale = _originalScale;

        _button.colors = _origColors;
        if (_buttonGraphic != null)
            _buttonGraphic.color = _origGraphicColor;

        if (_cooldownFill != null)
            _cooldownFill.fillAmount = 0f;

        _button.interactable = true;
        _isCooldown = false;
    }

    private IEnumerator PulseRoutine()
    {
        // Пульс масштаба + лёгкое «дыхание» прозрачности/яркости
        float time = 0f;

        // Если нет _buttonGraphic, попробуем приглушать через CanvasGroup
        CanvasGroup cg = GetComponent<CanvasGroup>();
        bool createdCg = false;
        if (cg == null)
        {
            cg = gameObject.AddComponent<CanvasGroup>();
            createdCg = true;
        }

        while (_isCooldown)
        {
            time += Time.deltaTime * _pulseSpeed;

            // Масштаб: синус вокруг 1.0
            float s = 1f + Mathf.Sin(time) * _pulseScale;
            transform.localScale = _originalScale * s;

            // Притухание: от 1.0 до (1 - blinkStrength)
            float fade = 1f - (Mathf.Abs(Mathf.Sin(time)) * _blinkStrength);
            cg.alpha = fade;

            yield return null;
        }

        // Чистим следы
        if (cg != null)
        {
            cg.alpha = 1f;
            if (createdCg) Destroy(cg);
        }
    }
}
