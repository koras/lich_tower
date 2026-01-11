using UnityEngine;
using System;
using Title;
using Player;
using Level;
using Damage;

namespace Heroes
{
    public class HeroesBase : MonoBehaviour
    {
        
        
        private Vector2 _lastHitDir = Vector2.left; // дефолт, чтобы не было нулей
        public Vector2 LastHitDir => _lastHitDir;
        
        [Header("Команда")]
        [SerializeField] protected int _team = 1;
        [SerializeField] private BaseManager _baseManager; 
        [SerializeField] private bool _isBoss; 
        
        [SerializeField] private Hero _hero = Hero.Lich; 

        
        [Header("Может гулять ?")] [SerializeField]
        public bool canRoaming = false;
        
        [Header("Hint")]
        [SerializeField] private FloatingText floatingText;
        [SerializeField] protected Vector3 offset = new Vector3(0, 0.5f, 0);
        
        [Header("Здоровье")] 
        [SerializeField] protected int maxHp = 100;
        [SerializeField] protected int _currentHp = 100;

        [Header("Манна")] 
        [SerializeField] protected int _maxManna = 100;
        [SerializeField] protected int _currentManna = 100;
        public int CurrentManna => _currentManna;
        
        [Header("Стоимость героя")] 
        [SerializeField] private int _gold = 1; 
       
     //   [SerializeField] private TMP_Text _goldText = null;  // если TextMeshPro
 
        
        public bool IsDead => _currentHp <= 0;

        public HealthbarBehaviour _healthbar;
        public MannaBarBehaviour _mannabar;
        
        [Header("Овал выделения")]
        [SerializeField] private GameObject selectionOval; 
        
        
        private CanvasGroup _healthbarCanvasGroup;
        
        private CanvasGroup _mannabarCanvasGroup;
        
        private BaseVisualCharacter _visual;
        private WarriorAI _ai;
        
        [Header("Модификаторы урона")]
        [SerializeField] [Range(0, 100)] private float missChance = 10f;
        [SerializeField] [Range(0, 100)] private float criticalChance = 15f;
        [SerializeField] private float criticalMultiplier = 2f;

        [Header("Регенерация здоровья")]
        [SerializeField] private bool useRegen = false; 
        [SerializeField] private bool useRegenManna = true;          // включить / выключить реген
       
        
        
        [SerializeField] private float mannaRegenInterval = 0.1f; // Интервал в секундах
        [SerializeField] private float regenPerSecond = 2f;      // сколько HP в секунду
        private float _mannaRegenTimer; // Таймер для регенерации маны
        private float _regenBuffer;                              // накопление дробных значений

        public event Action OnDeath;
        private bool _deathInvoked;


             
        [Header("Враги")]
        // может ли искать босса
        [SerializeField] private bool _findBoss = true;

         
        [Header("Показываем урон лича по героям")]
        [SerializeField] private ShowDamageLichAnimation _showDamageLichAnimation; // Префаб для спауна

         private Transform firePoint;
        
         [Header("При смерти юнита")]
         [SerializeField] private GameObject gibsContainerPrefab;
         
         
        public int GetMaxManna()
        { 
            return _maxManna;
        }
            // Получение текущей команды
        public int GetTeam()
        {
            return _team;
        }
        
        /// <summary>
        /// Может ли юнит искать босса противоположной команды
        /// </summary>
        public bool GetFindBoss()
        {
            return _findBoss;
        }
        public int GetMyTeam()
        {
            if (_baseManager == null) return _team; // или 0/1 по умолчанию
            return _baseManager.GetMyTeam();
        }
        
        private void OnEnable()
        {
            if (GetIsBoss())
            {
                // регистрируем босса 
                Debug.Log($"BossRegistry.RegisterBoss {GetTeam()} {transform}");
                BossRegistry.RegisterBoss(GetTeam(), transform);
            }
        }
        private void OnDisable()
        {
            if (GetIsBoss())
                BossRegistry.UnregisterBoss(GetTeam(), transform);
        }
        
        // ПРоверка команды
        public bool CheckTeam(int team)
        {
            return _team == team;
        }
        
        // ПРоверка команды
        public bool CheckMyTeam()
        {
            return _baseManager.GetMyTeam() == _team;
        }
      
        public bool GetIsBoss()
        {
            return _isBoss; 
        }
         


        private void Awake()
        {
            
            _ai = GetComponent<WarriorAI>();
            ApplyBalanceFromConfig();
            
             
            _visual = GetComponentInChildren<BaseVisualCharacter>(true);

            if (_healthbar == null)
            {
                _healthbar = GetComponentInChildren<HealthbarBehaviour>(true);
                _healthbarCanvasGroup = _healthbar.GetComponent<CanvasGroup>();
            }
 

            HealthBarInActive();
            MannaBarInActive();
            
            
            if(_mannabar != null)
            _mannabarCanvasGroup =  _mannabar.GetComponent<CanvasGroup>();
            
            // если в инспекторе не указали, пробуем найти ребёнка по имени
            if (selectionOval == null)
            {
                var tr = transform.Find("CircleOvalRedShape"); // имя объекта овала в иерархии
                if (tr != null) selectionOval = tr.gameObject;
            }
            
            if (_baseManager == null)
                _baseManager = FindObjectOfType<BaseManager>();
             
        }


        public int GetGold()
        {
            return _gold;
       }

        private void Start()
        {
            if (_healthbar != null)
                _healthbar.SetHealth(_currentHp, maxHp);
    
            if (_healthbarCanvasGroup == null && _healthbar != null)
                _healthbarCanvasGroup = _healthbar.gameObject.AddComponent<CanvasGroup>();



            if (_mannabar != null)
            {
                
                _mannabar.SetManna(_currentManna, _maxManna);
                if (_mannabarCanvasGroup == null && _mannabar != null)
                {
                    _mannabarCanvasGroup = _mannabar.gameObject.AddComponent<CanvasGroup>();
                }
                _mannabarCanvasGroup.alpha = 0.9f;
            }


            if (_healthbarCanvasGroup != null)
            {
                _healthbarCanvasGroup.alpha = 0.9f; 
            }
 
        }

        private void Update()
        {
            HandleRegen();
            if (_mannabar != null)
            {
                 MannaRegen();
            }
        }

        /// <summary>
        /// Пасивная регенерация здоровья
        /// </summary>
        private void MannaRegen()
        {
            if (!useRegenManna) return;
            if (IsDead) return;
            if (_currentManna >= _maxManna) return;

            // Уменьшаем таймер
            _mannaRegenTimer -= Time.deltaTime;
            
            // Если таймер достиг нуля
            if (_mannaRegenTimer <= 0f)
            {
                // Восстанавливаем 1 единицу маны
                AddManna(1);
                
                // Сбрасываем таймер
                _mannaRegenTimer = mannaRegenInterval;
            }
        }

        
        /// <summary>
        /// Пасивная регенерация здоровья
        /// </summary>
        private void HandleRegen()
        {
            if (!useRegen) return;
            if (IsDead) return;
            if (_currentHp >= maxHp) return;

            // regenPerSecond HP/сек → умножаем на Time.deltaTime
            _regenBuffer += regenPerSecond * Time.deltaTime;

            int whole = Mathf.FloorToInt(_regenBuffer);
            if (whole <= 0) return;

            _regenBuffer -= whole;
            Heal(whole);
            // Если хочешь всплывающий текст на каждый тик хила:
            // ShowFloatingText("+" + whole, Color.green);
        }

        public void TakeDamage(int baseDmg, Vector2 hitDir)
        {  
 
        
            // Промах
            // if (CheckForMiss())
            // {
            //     ShowFloatingText("MISS!", Color.yellow);
            //     return;
            // }
            //
            // // Крит
            // bool isCritical = CheckForCritical();
             int finalDmg = baseDmg;
            //
            // if (isCritical)
            // {
            //     finalDmg = Mathf.RoundToInt(baseDmg * criticalMultiplier);
            //     ShowFloatingText("CRIT!", Color.red);
            // }
          
            if (IsDead) return;
            // запоминаем направление удара, если оно валидно
            if (hitDir.sqrMagnitude > 0.0001f)
                _lastHitDir = hitDir.normalized;
            
            if (_visual != null) 
                _visual.FlashHit();
        
            _currentHp = Mathf.Max(0, _currentHp - finalDmg);

            if (_healthbar != null)
                _healthbar.SetHealth(_currentHp, maxHp);


            if (IsDead && !_deathInvoked)
            {
                _deathInvoked = true;
                HealthBarInActive();
                MannaBarInActive();
                _ai?.SetDeath(); 
                
                // ВАЖНО: Уведомляем GameManager о смерти героя
                if (Level.GameManager.Instance != null)
                {
                    Level.GameManager.Instance.OnHeroDeath(_hero);
                }
                OnDeath?.Invoke();
            }
        }
        
        
        private bool CheckForMiss()
        {
            float chance = UnityEngine.Random.Range(0f, 100f);
            return chance < missChance;
        }

        private bool CheckForCritical()
        {
            return UnityEngine.Random.Range(0f, 100f) < criticalChance;
        }
        
        private void ShowFloatingText(string message, Color color)
        {
            if (floatingText != null)
            {
                var ft = Instantiate(floatingText, transform.position + offset, Quaternion.identity);
                ft.Setup(message, color);
            }
           
        }
        
        
        public void Heal(int amount)
        {
            if (IsDead) return;
            if (amount <= 0) return;

            int oldHp = _currentHp;
            _currentHp = Mathf.Min(maxHp, _currentHp + Mathf.Abs(amount));

            if (_healthbar != null)
                _healthbar.SetHealth(_currentHp, maxHp);

            // Если хочешь подсветку лечения:
            // ShowFloatingText("+" + (_currentHp - oldHp), Color.green);
        }
        
        
        public void AddManna(int amount)
        {
            if (IsDead) return;
            if (amount <= 0) return;

            int oldHp = _currentManna;
            _currentManna = Mathf.Min(_maxManna, _currentManna + Mathf.Abs(amount));

            if (_mannabar != null)
                _mannabar.SetManna(_currentManna, _maxManna);

            // Если хочешь подсветку лечения:
            // ShowFloatingText("+" + (_currentHp - oldHp), Color.green);
        }
         

        public void HealthBarInActive()
        {
            if (_healthbar != null)
                _healthbar.gameObject.SetActive(false);
        }       

        public void MannaBarInActive()
        {
            if (_mannabar != null)
                _mannabar.gameObject.SetActive(false);
        }

        public void HealthBarActive()
        {
            if (_healthbar != null)
                _healthbar.gameObject.SetActive(true);
            
            if (_mannabar != null)
                _mannabar.gameObject.SetActive(true);
        }
        
        
        public void SetSelected(bool selected)
        {
            if (selectionOval != null)
                selectionOval.SetActive(selected);
        }

        public void ShowOval()
        {
            SetSelected(true);
        }

        public void HideOval()
        {
            SetSelected(false);
        }
        /// <summary>
        /// Есть ли достаточно маны для способности.
        /// </summary>
        public bool HasManna(int cost)
        {
            return _currentManna >= Mathf.Max(0, cost);
        }

        public int GetCurrentManna()
        {
            return _currentManna;
        }


        /// <summary>
        /// Списать ману. Возвращает true, если списание прошло.
        /// </summary>
        public bool SpendManna(int cost)
        {
            cost = Mathf.Max(0, cost);
            
            Debug.Log($"Пытаемся Списываем манну");
            if (_currentManna < cost) return false;

            Debug.Log($" Списываем манну {_currentManna}");
            _currentManna -= cost;

            if (_mannabar != null)
                _mannabar.SetManna(_currentManna, _maxManna);

            return true;
        }
        
        /// <summary>
        /// Возвращает тип героя
        /// </summary>
        public Hero GetHeroType()
        {
            return _hero;
        }

        
        public void ShowDamageAnimation(Hero hero)
        {      
            Debug.LogWarning($"[HeroesBase] ShowDamageAnimation hero={hero}");
            if (hero == Hero.Lich)
            {
                // ShowDamageLichAnimation
                
              //  _showDamageLichAnimation;
              
              Debug.Log($"Кидаем фаербол1111");
              if (_showDamageLichAnimation == null)
              {
              
                  Debug.LogError($"arrowPrefab _showDamageLichAnimation не установлен");
                  return;
              }

              Transform fp = transform;
              Vector2 spawnPos = fp.position;
              spawnPos.y -= 0.1f;
                 Instantiate(_showDamageLichAnimation, spawnPos, Quaternion.identity);
           //   Vector2 target = new Vector2(_targetPosition.x, _targetPosition.y);
           //   arrow.InitFire(target, 150);
            }
        }

        private void ApplyBalanceFromConfig()
        {
            if (BalanceManager.I == null) return;

            var difficulty = Level.GameSettings.Difficulty;

            if (BalanceManager.I.TryGetHeroBalance(GetHeroType(), difficulty, out var b))
            {
                maxHp = b.MaxHp;
                _maxManna = b.MaxMana;

                // при спавне логично начинать с полного
                _currentHp = maxHp;
                _currentManna = _maxManna;
                
                _ai.Weapon.SetDamage(b.Damage);

    
                

                Debug.LogWarning($"[HeroesBase] Balance applied: {_hero} diff={difficulty} hp={maxHp} mana={_maxManna} mana={b.Damage}");
            }
        }
        
        
        // Возможные герои
        public enum Hero
        {
            Lich,
            Shaman,
            Skeleton,
            SkeletonArcher,
            GobArcher,
            OrcWar,
        }
    }
}
