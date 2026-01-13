using UnityEngine;
using UnityEngine.AI;
using Weapons;
using System; // ← добавь для Action
using Weapons.Range;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Heroes
{
    public class WarriorAI : MonoBehaviour
    {
        // ===== ПАРАМЕТРЫ ПОВЕДЕНИЯ =====
        [Header("Маркировка цели")] [SerializeField]
        private bool showTargetDebug = true;

        [SerializeField] private Color targetColor = Color.red;
        [SerializeField] private GameObject targetMarkerPrefab;

        [SerializeField] private bool _controlledHero = false;

    

        
        private GameObject targetMarker;


        float _senseTimer;

        float _senseTimerBoss;
        //    [Header("Логика выбора цели")]
        //     [SerializeField] private bool retargetInSight = false; // по умолчанию ведем себя по-старому

        [Header("Может атаковать")] [SerializeField]
        public bool canAttack = true;

        [SerializeField] private string namePNS = "NoName";
        public NavMeshAgent Agent => _agent;

        //  [SerializeField] private float roamingDistanceMax = 7f; // максимальная дистанция для блуждания

        [SerializeField] private float roamWaitTime = 2f; // сколько стоим на точке
        [SerializeField] private float roamStoppingDistance = 0.05f; // на сколько близко подходим к точке
        private Vector3 _roamTarget;
        private float _roamWaitTimer;
        private bool _hasRoamPoint;


        [Header("Скорость")] [SerializeField] private float _moveSpeed = 1f; // для боя / движения к боссу
        [SerializeField] private float _roamSpeed = 0.15f; // для роуминга (можно = moveSpeed)


        private bool _deathHandled; // <- чтобы не выполнить OnDeath дважды

        [Header("Идентификация")] [SerializeField]
        private LayerMask unitMask; // слой, где находятся другие юниты

        [Header("Параметры дистанций зрения")] [SerializeField]
        private float sightRadius = 4f;

        [Header("дистанция, на которой юнит начинает атаковать")] [SerializeField]
        private float _attackingDistance = 1f; // Радиус атаки

        // множители дистанции для входа/выхода из атаки
        [SerializeField] private float attackEnterMul = 0.8f; // начинаем атаку ближе
        [SerializeField] private float attackExitMul = 1.2f; // выходим из атаки, если цель сильно отошла

        private float AttackEnterDistance => _attackingDistance * attackEnterMul;
        private float AttackExitDistance => _attackingDistance * attackExitMul;


        [Header("частота обновления пути к цели")] [SerializeField]
        private float repathRate = 0.25f; // частота обновления пути к цели


        [SerializeField] private float flipThreshold = 0.02f; // мёртвая зона по скорости

        [Header("Атака")] [SerializeField] private float attackRate = 1.2f; // количество атак в секунду


        [SerializeField] private float debugInterval = 0.2f;

        [Header("Оружие общее")] 
        
        [SerializeField] private WeaponBase weapon; // у каждого героя своё оружие

        public WeaponBase Weapon => weapon; // ← публичный геттер


        [Header("Оружие Лича")] [SerializeField]
        private LichWeapon _weaponLichFireball;
 
        
        // Позиция куда будет акатковать Лич
        private Vector3 _targetPosition;


        
        private BaseVisualCharacter _character;

        // ===== КОМПОНЕНТЫ И ПЕРЕМЕННЫЕ =====
        private NavMeshAgent _agent; // агент навигации Unity
        private float _attackCd; // кулдаун атаки
        private float _baseSpeed; // базовая скорость агента

        private Transform _boss; // цель (босс), к которому идём
        private Transform _currentTarget; // текущая цель атаки


        // Внутри WarriorAI
        public Transform CurrentTarget => _currentTarget;


        private float _dbgTimer;
        private int _lookDir = +1;

    
        
        private HeroesBase _heroesBase;
        private float _repathCd; // кулдаун пересчёта пути

        [Header("текущее состояние")] [SerializeField]
        private State _state = State.Appear; // текущее состояние


        [Header("Логирование")] [SerializeField]
        private bool debugAI;

        // относится только к личу
        private LichFireballAbility _lichFireball;

        public LichFireballAbility LichFireball => _lichFireball;


        /// <summary>
        /// Вызывается каждый раз при смене состояния ИИ (Idle/MovingToBoss/Chasing/Attacking/Death).
        /// На это событие может подписаться визуал, чтобы реагировать на смену (вкл/выкл бега и т.п.)
        /// </summary>
        public event Action<State> StateChanged;


        //  [Header("компонент здоровья текущей цели")] 
        private HeroesBase _targetHealth; // компонент здоровья текущей цели


        public bool IsSelected { get; private set; }
        public bool IsManualControl { get; private set; }

        private Vector3 _manualDestination;


        // Вверху класса
        private readonly Collider2D[] _senseHits = new Collider2D[16]; // подбери размер под свой максимум

        [SerializeField] private float senseInterval = 0.3f;

        // вызвать, когда кликнули по герою
        public void SetSelected(bool value)
        {
            IsSelected = value;
            DLog($"[{namePNS}] Показываем овал");
            _heroesBase.ShowOval();
      
        }


        private void OnEnable()
        {
            if (_heroesBase == null) _heroesBase = GetComponent<HeroesBase>();

            // ВАЖНО: регистрируем только настоящего босса
            if (_heroesBase != null && _heroesBase.GetIsBoss())
                BossRegistry.RegisterBoss(_heroesBase.GetTeam(), transform);
        }

        private void OnDisable()
        {
            if (_heroesBase != null && _heroesBase.GetIsBoss())
                BossRegistry.UnregisterBoss(_heroesBase.GetTeam(), transform);
        }


        // вызвать, когда кликнули по карте
        public void MoveToPointManual(Vector3 worldPos)
        {
            IsManualControl = true;
            ClearTarget(); // забываем врагов
            _manualDestination = worldPos;

            _agent.stoppingDistance = 0.05f;
            _agent.isStopped = false;
            _agent.SetDestination(_manualDestination);

            SwitchState(State.ManualMove); // добавим новое состояние
        }

        // ===== ИНИЦИАЛИЗАЦИЯ =====
        private void Awake()
        {
            _lichFireball = GetComponent<LichFireballAbility>();


            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false; // отключаем авто-поворот
            _agent.updateUpAxis = false; // отключаем выравнивание по оси Y (важно для 2D)
            _agent.angularSpeed = 0f; // чтобы не вращался
            _baseSpeed = _agent.speed;

            _character = GetComponentInChildren<BaseVisualCharacter>(true); // ищем у своих детей


            _heroesBase = GetComponent<HeroesBase>();
            if (_heroesBase != null)
                _heroesBase.OnDeath += HandleDeath;

            if (weapon == null)
                weapon = GetComponentInChildren<WeaponBase>(true); // ищем у своих детей
        }

        private void Start()
        {
            _character?.PlayAppear();
        }

        public void SetTargetPosition(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
        }

        public void ClearTargetPosition()
        {
            _targetPosition = Vector3.zero;
        }


        // ===== ОСНОВНОЙ ЦИКЛ =====
        private void Update()
        {
            if (_state == State.Appear) return;
            TickState();
        }

        private void TickState()
        {
            if (_state == State.Appear) return;

            // Поведение в зависимости от состояния
            switch (_state)
            {
                case State.Idle:
                {
                    _agent.isStopped = true;
                    // сначала ищем врагов
                    bool hasEnemy = SenseForEnemies();
                    if (hasEnemy)
                        break;

                    if (_heroesBase.canRoaming)
                    {
                        if (!_hasRoamPoint)
                        {
                            StartRoaming();
                        }

                        break;
                    }

                    if (_heroesBase.GetFindBoss() && !_heroesBase.GetIsBoss())
                    {
                        DLog($"ищет босса {namePNS}");

                        EnsureBoss();
                        if (_boss != null) GoToBoss();
                    }
                    else
                    {
                        if (namePNS != "Lich")
                        {
                            DLog($"[{namePNS}] Этот юнит не ищет босса (findBoss=false или это босс) {namePNS}");
                        }
                    }

                    break;
                }

                case State.MovingToBoss:
                {
                    DLog($"[{namePNS}] логика MovingToBoss");
                    bool hasEnemy = SenseForEnemies();
                    if (hasEnemy)
                        break;
                    //
                    // // если это враг, не двигаемся к боссу, а роумим
                    // if (!_heroesBase.CheckMyTeam())
                    // {
                    //     SwitchState(State.Idle); // в Idle нас отправят в роуминг
                    //     break;
                    // }

                    UpdateMoveToBoss();
                    break;
                }

                case State.Chasing:
              //      DLog($"[{namePNS}] логика преследования врага");
                    UpdateChasing(); // логика преследования врага
                    break;

                case State.Attacking:
                //    DLog($"[{namePNS}] логика атаки врага");
                    UpdateAttacking(); // логика атаки врага
                    break;

                case State.Appear:
                 //   DLog($"[{namePNS}] логика Appear");
                    //     UpdateAttacking(); // логика атаки врага
                    break;

                case State.Death:
                //    DLog($"[{namePNS}] смерть юнита");
              
                    break;
                case State.Roaming:
              //      DLog($"[{namePNS}] Roaming");
                    UpdateRoaming();
                    break;
                case State.RoamingWait:
                    UpdateRoamingWait();
                    break;
                case State.ManualMove:
                    UpdateManualMove();
                    break;
            }
        }

        /**
         * Останавливаем игрока
         */
        public void SetIsStoppedAgent()
        {
            _agent.isStopped = true;
        }
 
            
            
            
        private void UpdateManualMove()
        {
            if (_state == State.Death || _state == State.Appear)
                return;

            // В ручном режиме полностью игнорируем врагов
            // if (SenseForEnemies()) return;  // ← специально НЕ вызываем

            // периодически обновляем путь, если надо
            _repathCd -= Time.deltaTime;
            if (_repathCd <= 0f)
            {
                _agent.SetDestination(_manualDestination);
                _repathCd = repathRate;
            }

            // когда дошли до точки — выключаем ручной режим
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.05f)
            {
                IsManualControl = false;
                SwitchState(State.Idle);
            }
        }
        
        // ===== ВИЗУАЛИЗАЦИЯ РАДИУСОВ =====
        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR

            // Линии
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(transform.position, Vector3.forward, sightRadius);

            Handles.color = Color.red;
            Handles.DrawWireDisc(transform.position, Vector3.forward, _attackingDistance);

            Handles.color = Color.cyan;
            Handles.DrawWireDisc(transform.position, Vector3.forward, _attackingDistance);

            // радиус зрения
            Handles.Label(transform.position + Vector3.up * sightRadius, $"Sight: {sightRadius}");
            // радиус атаки
            Handles.Label(transform.position + Vector3.up * _attackingDistance, $"Attack: {_attackingDistance}");
            #endif
        }
        
        private void HandleDeath()
        {
            OnDeath();
            DLog($"[{namePNS}] ⚰️ уничтожен");
        }

        // ===== СМЕНА СОСТОЯНИЙ =====
        private void SwitchState(State s)
        {
            if (s == State.Appear)
            {
                DLog($" не меняем состояние {_state} == State.Appear");
                return;
            }

            // если уже умер, разрешаем только повторный Death (идемпотентно)
            if (_state == State.Death && s != State.Death) return;

            if (_state == s)
            {
                DLog($"уже в этом состоянии — ничего не делаем {_state}");
                return;
            } // если уже в этом состоянии — ничего не делаем

            _state = s;
            DLog($"Меняем состояние на {_state}");
            // Событие — только при смене!
            StateChanged?.Invoke(_state);
            //  _agent.speed = _baseSpeed;


            switch (s)
            {
                case State.Roaming:
                case State.RoamingWait:
                    DLog($"switch Roaming");
                    _agent.speed = _roamSpeed;
                    _agent.isStopped = false;
                    break;

                case State.MovingToBoss:
                case State.Chasing:
                    DLog($"switch Chasing");
                    _agent.speed = _moveSpeed;
                    _agent.isStopped = false;
                    break;
                //   case State.Appear:
                case State.Attacking:
                    DLog($"switch State.Attacking");
                    _agent.isStopped = false; // позволяем подправлять позицию
                    _agent.speed = _moveSpeed;
                    break;
                case State.Idle:
                    DLog($"switch State.Idle");
                    _agent.isStopped = true;
                    _agent.speed = _moveSpeed;
                    break;
                case State.Death:
                    DLog($"switch State.Death");
                    _agent.isStopped = true;
                    _agent.speed = 0f;
                    break;
            }
            ChangeAnimation();
        }

        // олько при смене состояния
        private void ChangeAnimation()
        {
        //    DLog($" Меняем анимацию [{namePNS}] {_state}");

            switch (_state)
            {
                case State.Idle:
                    _character?.PlayIdle();
                    break;

                case State.MovingToBoss:
                    _character?.PlayWalk();
                    break;

                case State.Chasing:
                    _character?.PlayWalk(); // логика преследования врага
                    break;

                case State.Attacking:
                    _character?.PlayAttack();
                    break;

                case State.Roaming:
                    _character?.PlayRoaming();
                    break;

                case State.Death:
                    _character?.PlayDeath();
                    break;
                case State.Appear:
                    _character?.PlayAppear();
                    break;
                case State.RoamingWait:
                    _character?.PlayIdle();
                    break;
                default:
                    _character?.PlayIdle();
                    break;
            }
        }

        public void SetDeath()
        {
            _heroesBase.HideOval();
            SwitchState(State.Death);
        }

        // ===== ДВИЖЕНИЕ К БОССУ =====
        private void GoToBoss()
        {
            if (_controlledHero)
            {
                // управлемый герой игроком
                return;
            }

            if (_state == State.Appear) return;
            if (_state == State.Death) return;
            
            // ПРОВЕРКА: может ли этот юнит искать босса?
            if (!_heroesBase.GetFindBoss())
            {
                DLog($"[{namePNS}] У этого юнита отключен поиск босса");
                return;
            }
            
            
            // Любой герой (и враг, и союзник) должен искать босса противоположной команды
            // Единственное исключение - сам босс не должен искать другого босса
          //  if (_heroesBase.CheckMyTeam())
         //   {
                DLog($"ищет босса");
                if (_heroesBase.GetIsBoss()) return;
                if (_boss == null)
                {
                    _state = State.Idle;
                    return;
                }

                _agent.stoppingDistance = _attackingDistance;
                _agent.SetDestination(_boss.position);
                SwitchState(State.MovingToBoss);
                DLog($"[{namePNS}] Иду к боссу противоположной команды");
          //  }
        }

        private void UpdateMoveToBoss()
        {
            if (_controlledHero)
            {
                // управлемый герой игроком
                return;
            }

            if (_state == State.Appear) return;
            
            // ПРОВЕРКА: все еще может искать босса?
            if (!_heroesBase.GetFindBoss())
            {
                DLog($"[{namePNS}] Поиск босса отключен, перехожу в Idle");
                SwitchState(State.Idle);
                return;
            }
            
            
            
            if (_boss == null)
            {
                DLog($" UpdateMoveToBoss State.Idle");
                SwitchState(State.Idle);
                return;
            }

            DLog($" UpdateMoveToBoss");
            // периодически обновляем путь
            _repathCd -= Time.deltaTime;
            if (_repathCd <= 0f)
            {
                _agent.SetDestination(_boss.position);
                _repathCd = repathRate;
            }

            // если дошли до босса — останавливаемся
            var dist = Vector3.Distance(transform.position, _boss.position);
            if (dist <= _attackingDistance + 0.1f) SwitchState(State.Idle);
        }

        // ===== ПРЕСЛЕДОВАНИЕ ВРАГА =====
        private void UpdateChasing()
        {
            if (_state == State.Appear) return;
            if (_state == State.Death) return;

            if (!HasValidTarget())
            {
                ClearTarget();
                return;
            }

            var dist = Vector3.Distance(transform.position, _currentTarget.position);

            // если враг в радиусе атаки — начинаем бить
            if (dist <= AttackEnterDistance)
            {
                SwitchState(State.Attacking);
                return;
            }

            if (_dbgTimer <= 0f)
            {
                _dbgTimer = debugInterval;
            }

            // периодически пересчитываем путь
            _repathCd -= Time.deltaTime;
            if (_repathCd <= 0f)
            {
                _agent.SetDestination(_currentTarget.position);
                _repathCd = repathRate;
            }
        }

        public void SetCanAttack(bool canAttackCharacter)
        {
            DLog($"WarriorAI меняем состояние атаки canAttack {namePNS}");
            canAttack = canAttackCharacter;
        }


        private void UpdateAttacking()
        {
            if (_state == State.Appear || _state == State.Death) return;
            if (!canAttack) return;

            // 1) Быстро выходим, если цели нет
            if (_currentTarget == null || _targetHealth == null || _targetHealth.IsDead ||
                !_currentTarget.gameObject.activeInHierarchy)
            {
                ExitAttack_NoTarget();
                return;
            }

            // 2) Дистанция (используем sqrMagnitude, чтобы вообще без sqrt)
            var delta = _currentTarget.position - transform.position;
            float distSqr = delta.sqrMagnitude;
            float exitSqr = AttackExitDistance * AttackExitDistance;

            if (distSqr > exitSqr)
            {
                _agent.isStopped = false;
                SwitchState(State.Chasing);
                return;
            }

            // 3) Кулдаун
            _attackCd -= Time.deltaTime;
            if (_attackCd > 0f) return;

            _attackCd = 1f / Mathf.Max(0.01f, attackRate);

            // В момент удара проверяем цель ещё раз (но это редкое событие)
            if (_targetHealth.IsDead || !_currentTarget.gameObject.activeInHierarchy)
            {
                ExitAttack_NoTarget();
                return;
            }

            StartAttack();
        }

        private void ExitAttack_NoTarget()
        {
            // Важно: не делай тут StartRoaming и прочую хрень каждый кадр
            // Просто сбрось и уйди в Idle, а Idle сам решит что делать.
            _currentTarget = null;
            _targetHealth = null;
            weapon?.ClearTarget();

            _agent.isStopped = true;
            _agent.stoppingDistance = _attackingDistance;

            SwitchState(State.Idle); // Idle сам вызовет EnsureBoss/GoToBoss или Roaming
        }

        public void InvokeAppearFromAnimation()
        {
            SwitchState(State.Idle);
            _heroesBase.HealthBarActive();
            
            if (_controlledHero)
            { 
                return;
            }
 
            if (_heroesBase.GetFindBoss() && !_heroesBase.GetIsBoss()) 
            {
                DLog($"InvokeAppearFromAnimation() - ищу босса противоположной команды");
                _boss = null;
                EnsureBoss();
        
                // Если нашли босса - идем к нему
                if (_boss != null)
                {
                    GoToBoss();
                    return; // ← ВАЖНО: выходим, не переходим в роуминг
                }
            } else
            {
                DLog($"[{namePNS}] Не ищу босса (findBoss={_heroesBase.GetFindBoss()}, isBoss={_heroesBase.GetIsBoss()})");
            }
            
            // ТОЛЬКО если не ищем босса ИЛИ босс не найден
            if (_heroesBase.canRoaming)
            {
                DLog($"[{namePNS}] Начинаю роуминг");
                SwitchState(State.Roaming);
            }

            return;
            if (_heroesBase.CheckMyTeam())
            {
                DLog($"InvokeAppearFromAnimation()");


                _boss = null;
                EnsureBoss();
            }
            else
            {
                if (!_heroesBase.GetIsBoss())
                {
                    DLog($"{name}: Меняем состояние на Roaming");
                    SwitchState(State.Roaming);
                }
            }
        }

        public void InvokeAttackFromAnimation()
        {
            if (!canAttack)
            {
                DLog($"WarriorAI запрет на атаку canAttack {namePNS}");
                return;
            }

            if (_state == State.Appear) return;
            if (_state == State.Death) return; // мертвые не бьют
            // Ещё раз проверяем, что цель валидна


            // Ещё раз проверяем, что цель валидна
            if (!HasValidTarget())
            {
                ClearTarget();
                GoToBoss();
                return;
            }

            // ПЕРЕД ВЫСТРЕЛОМ ОБНОВЛЯЕМ ЦЕЛЬ В ОРУЖИИ
            weapon?.SetEnemyTarget(_currentTarget);
            weapon?.SetTargetHealth(_targetHealth);
            
            HeroesBase.Hero _hero = _heroesBase.GetHeroType();
            _targetHealth?.ShowDamageAnimation(_hero);
            
         
            if (_currentTarget != null)
            {
                weapon?.Attack();
            }
            else
            {
                _state = State.Idle;
                ChangeAnimation();
                DLog($"цель  ☠️ ☠️ ☠️ ☠️ ☠️ ☠️ ☠️ ☠️");
            }
        }


        public void InvokeAttackLichFireballFromAnimation()
        {
            DLog($"АТАКА АТАКА АТАКА АТАКА АТАКА АТАКА АТАКА АТАКААТАКА АТАКА АТАКА АТАКА АТАКА АТАКА АТАКА АТАКА ");
            if (_state == State.Appear) return;
            if (_state == State.Death) return; // мертвые не бьют

            if (_weaponLichFireball != null)
            {
                _currentTarget = null;
                _targetHealth = null;
                _weaponLichFireball.SetTargetPoint(_targetPosition);
                DLog($"АТАКА АТАКА АТАКА АТАКА АТАКА АТАКА АТАКА АТАКА ");
                
                // списываем манну
                _heroesBase.SpendManna(_weaponLichFireball.GetMannaLichCost());
                _weaponLichFireball.Attack();
            }
            else
            {
                DLog($"_weaponLichFireball != null");
            }
        }

        // установка цели
        private void StartAttack()
        {
            if (!canAttack)
            {
                return;
            }

            // для координат
            weapon?.SetEnemyTarget(_currentTarget);
            // устанавливаем основного героя в виде цели
            weapon?.SetTargetHealth(_targetHealth);


            //   weapon?.Attack();
            //   weapon?.InvokeAttack();
            // Просто проигрываем анимацию атаки — урон пойдёт через Animation Event
        }

        /**
         * логика атаки
         */
        private void PerformHit()
        {
            // 1) Дистанция
            var dist = Vector2.Distance(transform.position, _currentTarget.position);
            if (dist > _attackingDistance * 1.1f)
            {
                DLog($"[{namePNS}] Удар отменён: далеко ({dist:F2} > {_attackingDistance * 1.1f:F2})");
                _state = State.Chasing;
                return;
            }

            // 2) Бьём оружием (если есть)
            if (weapon == null)
            {
                DLog($"Отсутствует  оружие [{namePNS}] weapon == NULL — у героя не назначено оружие");
                ClearTarget();
                GoToBoss();
                return;
            }


            // 3) Наносим урон напрямую (если нужно) и/или проверяем смерть
            //    ВАЖНО: цель обязана иметь HeroesBase, иначе выходим
            if (_targetHealth == null)
            {
                //     Debug.LogWarning($"[{namePNS}] У цели нет HeroesBase — сбрасываю цель");
                ClearTarget();
                GoToBoss();
                return;
            }

            // Если урон наносит само оружие — ниже можно убрать.


            if (_targetHealth.IsDead)
            {
                DLog($"[{namePNS}] ❌ Цель уничтожена: {_currentTarget.name}");
                ClearTarget();
                //     GoToBoss();
            }
        }


        // ===== ОБНАРУЖЕНИЕ ВРАГОВ =====
        private bool SenseForEnemies()
        {
            _senseTimer -= Time.deltaTime;
            if (_senseTimer <= 0f)
            {
                _senseTimer = senseInterval;

                int myTeam = _heroesBase.GetTeam();
                // если сейчас ручное управление — никого не ищем
                if (IsManualControl)
                {
                    DLog($"сейчас ручное управление — никого не ищем,{namePNS}");
                    return false;
                }
                if (HasValidTarget())
                {
                    return true;
                }
                // 2. Если цель была, но уже невалидна — обнуляем, чтобы можно было взять новую
                _currentTarget = null;
                _targetHealth = null;


                int count = Physics2D.OverlapCircleNonAlloc(transform.position, sightRadius, _senseHits, unitMask);
                Transform best = null;
                var bestSqr = float.MaxValue;


                for (int i = 0; i < count; i++)
                {
                    var col = _senseHits[i];

                    if (col.transform == transform)
                    {
                        continue;
                    }

                    // var unit = col.GetComponent<UnitLink>();
                    var unit = col.GetComponentInParent<UnitLink>();
                    if (!unit || unit.Hp == null) continue;
                    var hp = unit.Hp;


                    //  var hp = col.GetComponentInParent<HeroesBase>();
                    // ИСПРАВЛЕНИЕ: атаковать врагов, у которых команда НЕ совпадает с нашей
                    if (!hp || hp.IsDead) continue;


                    if (hp.GetTeam() == myTeam)
                    {
                        continue;
                    }


                    var sqr = (col.transform.position - transform.position).sqrMagnitude;
                    if (sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        best = hp.transform;
                    }
                }


                if (best != null)
                {
                    if (_currentTarget != best)
                    {
                        SetTarget(best);
                    }

                    return true;
                }

                if (_currentTarget != null || _targetHealth != null)
                {
                    ClearTarget();
                }
            }

            return false;
        }

        // ===== УПРАВЛЕНИЕ ЦЕЛЬЮ =====
        private void SetTarget(Transform t)
        {
            if (t == null) return;

            // Ищем здоровье на объекте или у его родителя
            // (подстрой под свою иерархию)
            var hp = t.GetComponent<HeroesBase>() ?? t.GetComponentInParent<HeroesBase>();
            if (hp == null)
            {
                //     Debug.LogWarning($"[{namePNS}] Попытка выбрать цель без HeroesBase: {t.name}");
                return;
            }


            _currentTarget = hp.transform; // фиксируемся на том Transform, где есть здоровье

            _targetHealth = hp;

            _agent.stoppingDistance = AttackEnterDistance;

            _agent.isStopped = false; // ← ВАЖНО
            _agent.SetDestination(_currentTarget.position);


            var ai = _currentTarget.GetComponent<WarriorAI>();
            var targetPns = ai != null ? ai.namePNS : _currentTarget.name;


            SwitchState(State.Chasing);
        }

        private void ClearTarget()
        {
            if (_state == State.Death) return;
            if (_currentTarget)
            {
                var ai = _currentTarget.GetComponent<WarriorAI>();
                var targetPns = ai ? ai.namePNS : _currentTarget.name;
                DLog($"[{namePNS}] 🔁 Сброс цели: {targetPns}");
            }

            _currentTarget = null;
            _targetHealth = null;
            if (weapon != null)
                weapon.ClearTarget();
            _agent.stoppingDistance = _attackingDistance;
            _agent.isStopped = true;

            SwitchState(State.Idle);


            DLog($"[{namePNS}] 🔁 Включаем роуминг 1");
        }

        private bool HasValidTarget()
        {
            // Цель должна существовать И иметь HeroesBase
            if (_currentTarget == null) return false;
            if (_targetHealth == null) return false;
            if (_targetHealth.IsDead) return false;
            // _deathHandled
            return _currentTarget.gameObject.activeInHierarchy;
        }

        private void EnsureBoss()
        {
            if (_boss != null) return;
    
            int myTeam = _heroesBase.GetTeam();
            DLog($"[{namePNS}] Моя команда: {myTeam}. Ищу босса противоположной команды...");
    
            // Получаем босса противоположной команды
            _boss = BossRegistry.GetEnemyBoss(myTeam);
    
            if (_boss != null)
            {
                DLog($"[{namePNS}] ✓ Нашел босса противоположной команды: {_boss.name} (команда босса: {_boss.GetComponent<HeroesBase>()?.GetTeam()})");
        
                // Отладка: проверяем команду босса
                var bossHero = _boss.GetComponent<HeroesBase>();
                if (bossHero != null)
                {
                    DLog($"[{namePNS}] Команда босса: {bossHero.GetTeam()}, моя команда: {myTeam}");
                }
            }
            else
            {
                DLog($"[{namePNS}] ✗ Не удалось найти босса противоположной команды");
        
                // Отладка: что в реестре?
                DLog($"[{namePNS}] Реестр боссов: {BossRegistry.DebugInfo()}");
            }
        }

        private bool TryGetRandomPointAround(Vector3 origin, float radius, out Vector3 result)
        {
            for (int i = 0; i < 10; i++)
            {
                // случайная точка в круге
                Vector2 random2D = UnityEngine.Random.insideUnitCircle * radius;
                var randomPos = origin + new Vector3(random2D.x, random2D.y, 0f);

                if (NavMesh.SamplePosition(randomPos, out var hit, 1f, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }

            result = origin;
            return false;
        }

        // Меняем цель БЕЗ смены состояния (остаёмся в Attacking)
        private void AssignTargetForAttack(Transform t, HeroesBase hp)
        {
            _currentTarget = t;
            _targetHealth = hp;

            _agent.stoppingDistance = AttackEnterDistance;
            _agent.isStopped = false;
            _agent.SetDestination(_currentTarget.position);
        }

        private void UpdateRoaming()
        {
            if (_state == State.Death || _state == State.Appear)
                return;

            // 1. Сначала проверяем, не появился ли враг
            if (SenseForEnemies())
            {
                return;
            }

            if (!_hasRoamPoint)
            {
                StartRoaming();
                return;
            }

            var delta = _roamTarget - transform.position;
            if (delta.sqrMagnitude <= (_agent.stoppingDistance + 0.05f) * (_agent.stoppingDistance + 0.05f))
            {
                _agent.isStopped = true;
                _roamWaitTimer = 0f;
                SwitchState(State.RoamingWait);
                DLog($"[{namePNS}] Roaming: reached point, wait");
                return;
            }

            // 3. Ещё идём к точке
            if (_agent.isStopped)
                _agent.isStopped = false;

            if (!_agent.pathPending && _agent.remainingDistance < 0.1f)
            {
                _agent.SetDestination(_roamTarget);
            }
        }

        private void UpdateRoamingWait()
        {
            if (_state == State.Death || _state == State.Appear)
                return;

            if (SenseForEnemies())
            {
                // SetTarget переведёт в Chasing
                return;
            }

            _roamWaitTimer += Time.deltaTime;

            if (_roamWaitTimer >= roamWaitTime)
            {
                _hasRoamPoint = false;
                StartRoaming(); // снова пойдём, SwitchState переведёт в Roaming
            }
        }

        private void StartRoaming()
        {
            if (!_heroesBase.canRoaming)
                return;

            if (_state == State.Death || _state == State.Appear)
            {
                DLog($"[{namePNS}] 🔁 Только живые или то кто появились");
                return;
            }

            if (_agent == null || !_agent.enabled)
            {
                DLog($"[{namePNS}] 🔁 не можем роумить нет агента");
                return;
            }

            if (!TryGetRandomPointAround(transform.position, sightRadius, out _roamTarget))
            {
                DLog($"[{namePNS}] 🔁 не можем роумить Idle ищем точку");
                _state = State.Idle;
                _hasRoamPoint = false;
                return;
            }

            _hasRoamPoint = true;
            _roamWaitTimer = 0f;

            _agent.stoppingDistance = roamStoppingDistance;
            _agent.isStopped = false;
            _agent.SetDestination(_roamTarget);

            DLog($"[{namePNS}] 🔁 меняем состояние на Roaming");

            SwitchState(State.Roaming);
        }


        /**
         * Мы поворачиваемся лицом к врагу.
         * Метод публичный потому что вызываем ото всюду
         */
        public bool turnToFace()
        {
            // 1) Пытаемся смотреть по желаемой скорости агента (плавнее и без рывков пути)
            var v = _agent.desiredVelocity; // для NavMeshAgent в 2D это X/Y плоскость (Y = up)
            // 2) Если стоим или скорость очень маленькая — в атаке/преследовании смотрим на цель
            if (v.sqrMagnitude < flipThreshold * flipThreshold)
            {
                if (_currentTarget != null)
                {
                    var dx = _currentTarget.position.x - transform.position.x;
                    if (Mathf.Abs(dx) > flipThreshold)
                        _lookDir = dx > 0f ? +1 : -1;
                }
                // иначе просто сохраняем предыдущий _lookDir
            }
            else
            {
                // есть движение — смотрим по направлению X
                if (Mathf.Abs(v.x) > flipThreshold)
                    _lookDir = v.x > 0f ? +1 : -1;
            }

            // 3) Применяем флип
            return _lookDir < 0;
        }


        private void OnDeath()
        {
            if (_deathHandled) return; // защита от повторов
            _deathHandled = true;
            SwitchState(State.Death);

            if (_agent)
            {
                _agent.isStopped = true;
                _agent.enabled = false;
            }

            // 3. Вырубаем физику
            var rb2D = GetComponent<Rigidbody2D>();
            if (rb2D) rb2D.simulated = false;

            var cols = GetComponentsInChildren<Collider2D>(includeInactive: true);
            foreach (var c in cols) c.enabled = false;

            DLog($"[{namePNS}] 💀 погиб — уничтожаю объект через 2 сек.");
            GetComponent<Collider2D>().enabled = false; // отключаем столкновения
            // Можно добавить задержку, чтобы успела проиграться анимация
            if (weapon)
            {
                weapon.gameObject.SetActive(false);
            }


            StopAllCoroutines();
            if (_heroesBase != null)
            {
                _heroesBase.OnDeath -= HandleDeath;
            }

            if (_heroesBase.GetHeroType() == HeroesBase.Hero.Skeleton || _heroesBase.GetHeroType() == HeroesBase.Hero.SkeletonArcher )
            {
                if (_heroesBase != null && _heroesBase.GibsPrefab != null)
                {
                    var go = Instantiate(_heroesBase.GibsPrefab, transform.position, Quaternion.identity);

                    var gibs = go.GetComponent<Heroes.BodyParts.Skeleton.GibsContainer2D>();
                    if (gibs != null)
                    {
                        // Вот это ключ: куда разлетается
                        // Если hitDir = "от атакующего к жертве", то куски обычно летят "по этому же" направлению.
                        Vector2 pushDir = _heroesBase.LastHitDir;

                        // Если хочешь наоборот (удар справа -> влево), просто инвертируй:
                        // Vector2 pushDir = -_heroesBase.LastHitDir;

                        Debug.LogWarning($"[HeroesBase] {pushDir} pushDir: {pushDir}");
                        
                        
                        gibs.Scatter(Vector2.zero, pushDir);
                    }
                }

                Destroy(gameObject, 0f);
                return;
            }

            Destroy(gameObject, 7f);
        }

        // Возможные состояния ИИ
        public enum State
        {
            Start,
            Idle,
            MovingToBoss,
            Chasing,
            Attacking,
            Death,
            Appear,
            Roaming,
            RoamingWait,
            ManualMove,
        }
        
        
        private void UpdateTargetVisualization()
        {
            // Создаем маркер если нужно
            if (showTargetDebug && targetMarkerPrefab != null && targetMarker == null)
            {
                targetMarker = Instantiate(targetMarkerPrefab);
                targetMarker.name = $"{gameObject.name}_TargetMarker";
            }

            // Обновляем позицию маркера
            if (targetMarker != null)
            {
                if (_currentTarget != null)
                {
                    // УБРАТЬ Vector3.up * 2f - использовать реальную позицию цели
                    targetMarker.transform.position = _currentTarget.position;
                    targetMarker.SetActive(true);
            
                    // Логируем информацию о цели
                    float distance = Vector3.Distance(transform.position, _currentTarget.position);
                    Vector3 targetPos = _currentTarget.position;

                }
                else
                {
                    targetMarker.SetActive(false);
                }
            }
        }

        
        
        // Визуализация в Scene View
// Визуализация в Scene View
        private void OnDrawGizmos()
        {
            if (!showTargetDebug || _currentTarget == null) return;

            Gizmos.color = targetColor;
    
            // Линия от центра к центру (без смещения)
            Gizmos.DrawLine(transform.position, _currentTarget.position);
    
            // Маркер на реальной позиции цели
            Gizmos.DrawWireSphere(_currentTarget.position, 0.3f);
    
            // Подпись с дистанцией
#if UNITY_EDITOR
            float distance = Vector3.Distance(transform.position, _currentTarget.position);
            Vector3 targetPos = _currentTarget.position;
    
            // Подпись над целью
            UnityEditor.Handles.Label(_currentTarget.position + Vector3.up * 0.5f, 
                $"Цель: {_currentTarget.name}\n" +
                $"Позиция: ({targetPos.x:F1}, {targetPos.y:F1})");
    
            // Подпись над юнитом
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, 
                $"Дистанция: {distance:F2}");
#endif
        }
        private void DLog(string msg)
        {
            if (debugAI) Debug.Log(msg);
        }
    }
}