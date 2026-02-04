using UnityEngine;
using UnityEngine.AI;
using Weapons;
using System;
using Weapons.Range;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Heroes
{
    public class WarriorAI : MonoBehaviour
    {
        // ===== ПАРАМЕТРЫ ПОВЕДЕНИЯ =====
        [Header("Маркировка цели")]
        [SerializeField] private bool showTargetDebug = true;
        [SerializeField] private Color targetColor = Color.red;
        [SerializeField] private GameObject targetMarkerPrefab;
        [SerializeField] private bool _controlledHero = false;
        
        private GameObject targetMarker;
        private float _senseTimer;
        private float _senseTimerBoss;

        [Header("Может атаковать")]
        [SerializeField] public bool canAttack = true;
        [SerializeField] private string namePNS = "NoName";
        
        public NavMeshAgent Agent => _agent;
        
        [Header("Роуминг")]
        [SerializeField] private float roamWaitTime = 2f;
        [SerializeField] private float roamStoppingDistance = 0.05f;
        
        private Vector3 _roamTarget;
        private float _roamWaitTimer;
        private bool _hasRoamPoint;

        [Header("Скорость")]
        [SerializeField] private float _moveSpeed = 1f;
        [SerializeField] private float _roamSpeed = 0.15f;

        private bool _deathHandled;

        [Header("Идентификация")]
        [SerializeField] private LayerMask unitMask;

        [Header("Параметры дистанций зрения")]
        [SerializeField] private float sightRadius = 4f;
        
        [Header("Дистанция атаки")]
        [SerializeField] private float _attackingDistance = 1f;
        
        [Header("Множители дистанции")]
        [SerializeField] private float attackEnterMul = 0.8f;
        [SerializeField] private float attackExitMul = 1.2f;
        
        private float AttackEnterDistance => _attackingDistance * attackEnterMul;
        private float AttackExitDistance => _attackingDistance * attackExitMul;

        [Header("Частота обновления пути")]
        [SerializeField] private float repathRate = 0.25f;
        
        [SerializeField] private float flipThreshold = 0.02f;
        
        [Header("Атака")]
        [SerializeField] private float attackRate = 1.2f;
        
        [SerializeField] private float debugInterval = 0.2f;

        [Header("Оружие")]
        [SerializeField] private WeaponBase weapon;
        public WeaponBase Weapon => weapon;

        [Header("Оружие Лича")]
        [SerializeField] private LichWeapon _weaponLichFireball;
        
        private Vector3 _targetPosition;
        private BaseVisualCharacter _character;

        // ===== КОМПОНЕНТЫ И ПЕРЕМЕННЫЕ =====
        private NavMeshAgent _agent;
        private float _attackCd; 
        private Transform _boss;
        private Transform _currentTarget;
        public Transform CurrentTarget => _currentTarget;
        
        private float _dbgTimer;
        private int _lookDir = +1;
        
        private HeroesBase _heroesBase;
        private float _repathCd;
        
        [Header("Текущее состояние")]
        [SerializeField] private State _state = State.Appear;
        
        [Header("Логирование")]
        [SerializeField] private bool debugAI;
        
        private LichFireballAbility _lichFireball;
        public LichFireballAbility LichFireball => _lichFireball;
        private HeroesBase _targetHealth;
        
        public bool IsSelected { get; private set; }
        public bool IsManualControl { get; private set; }
        private Vector3 _manualDestination;
        
        private readonly Collider2D[] _senseHits = new Collider2D[16];
        [SerializeField] private float senseInterval = 0.3f;

        /// <summary>
        /// Вызывается каждый раз при смене состояния ИИ
        /// </summary>
        public event Action<State> StateChanged;

        // ===== ИНИЦИАЛИЗАЦИЯ =====
        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_agent != null)
            {
                _agent.updateRotation = false;
                _agent.updateUpAxis = false;
                _agent.angularSpeed = 0f;
            }

            _heroesBase = GetComponent<HeroesBase>();
            if (_heroesBase != null)
            {
                _heroesBase.OnDeath += HandleDeath;
                _heroesBase.OnCanMoveChanged += OnCanMoveChanged;
                
                if (!_heroesBase.CanMove)
                    HardStopAgent();
            }

            _lichFireball = GetComponent<LichFireballAbility>();
            _character = GetComponentInChildren<BaseVisualCharacter>(true);
            
            if (weapon == null)
                weapon = GetComponentInChildren<WeaponBase>(true);
        }

        private void Start()
        {
            _character?.PlayAppear();
        }

        private void OnEnable()
        {
            if (_heroesBase != null && _heroesBase.GetIsBoss())
                BossRegistry.RegisterBoss(_heroesBase.GetTeam(), transform);
        }

        private void OnDisable()
        {
            if (_heroesBase != null && _heroesBase.GetIsBoss())
                BossRegistry.UnregisterBoss(_heroesBase.GetTeam(), transform);
        }

        private void OnDestroy()
        {
            if (_heroesBase != null)
            {
                _heroesBase.OnCanMoveChanged -= OnCanMoveChanged;
                _heroesBase.OnDeath -= HandleDeath;
            }
        }

        // ===== ОСНОВНОЙ ЦИКЛ =====
        private void Update()
        {
            if (_state == State.Appear || _state == State.Death)
                return;
                
            TickState();
            UpdateTargetVisualization();
        }

        private void TickState()
        {
            bool canUseAgent = _agent != null && _agent.enabled && _agent.isOnNavMesh;

            switch (_state)
            {
                case State.Idle:
                    if (canUseAgent)
                        _agent.isStopped = true;
                        
                    bool hasEnemy = SenseForEnemies();
                    if (hasEnemy)
                        break;

                    if (_heroesBase != null && _heroesBase.canRoaming)
                    {
                        if (!_hasRoamPoint)
                            StartRoaming();
                        break;
                    }

                    if (_heroesBase != null && _heroesBase.GetFindBoss() && !_heroesBase.GetIsBoss())
                    {
                        EnsureBoss();
                        if (_boss != null) 
                            GoToBoss();
                    }
                    break;

                case State.MovingToBoss:
                    SenseForEnemies();
                    UpdateMoveToBoss();
                    break;

                case State.Chasing:
                    UpdateChasing();
                    break;

                case State.Attacking:
                    UpdateAttacking();
                    break;

                case State.Roaming:
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

        // ===== ПУБЛИЧНЫЕ МЕТОДЫ =====
        public void SetSelected(bool value)
        {
            IsSelected = value;
            _heroesBase?.ShowOval();
        }

        public void MoveToPointManual(Vector3 worldPos)
        {
            if (!CanMoveNow()) 
                return;
                
            IsManualControl = true;
            ClearTarget();
            _manualDestination = worldPos;

            if (_agent != null)
            {
                _agent.stoppingDistance = 0.05f;
                _agent.isStopped = false;
                _agent.SetDestination(_manualDestination);
            }

            SwitchState(State.ManualMove);
        }

        public void SetTargetPosition(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
        }

        public void ClearTargetPosition()
        {
            _targetPosition = Vector3.zero;
        }

        public void SetIsStoppedAgent()
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                _agent.isStopped = true;
        }

        public void SetCanAttack(bool canAttackCharacter)
        {
            canAttack = canAttackCharacter;
            DLog($"WarriorAI меняем состояние атаки canAttack {namePNS}");
        }

        public void SetDeath()
        {
            _heroesBase?.HideOval();
            SwitchState(State.Death);
        }

        public bool turnToFace()
        {
            if (_agent != null)
            {
                var v = _agent.desiredVelocity;
                if (v.sqrMagnitude < flipThreshold * flipThreshold)
                {
                    if (_currentTarget != null)
                    {
                        var dx = _currentTarget.position.x - transform.position.x;
                        if (Mathf.Abs(dx) > flipThreshold)
                            _lookDir = dx > 0f ? +1 : -1;
                    }
                }
                else
                {
                    if (Mathf.Abs(v.x) > flipThreshold)
                        _lookDir = v.x > 0f ? +1 : -1;
                }
            }
            
            return _lookDir < 0;
        }

        public void InvokeAppearFromAnimation()
        {
            SwitchState(State.Idle);
            _heroesBase?.HealthBarActive();
            
            if (_controlledHero)
                return;

            if (_heroesBase != null && _heroesBase.GetFindBoss() && !_heroesBase.GetIsBoss()) 
            {
                _boss = null;
                EnsureBoss();
        
                if (_boss != null)
                {
                    GoToBoss();
                    return;
                }
            }
            
            if (_heroesBase != null && _heroesBase.canRoaming)
                SwitchState(State.Roaming);
        }

        public void InvokeAttackFromAnimation()
        {
            if (!canAttack || _state == State.Appear || _state == State.Death)
                return;

            if (!HasValidTarget())
            {
                ClearTarget();
                GoToBoss();
                return;
            }

            weapon?.SetEnemyTarget(_currentTarget);
            weapon?.SetTargetHealth(_targetHealth);
            
            if (_heroesBase != null)
            {
                HeroesBase.Hero hero = _heroesBase.GetHeroType();
                
                if (hero == HeroesBase.Hero.Lich && _targetHealth != null)
                {
                    _heroesBase.ShowDamageAnimationAt(_targetHealth.transform.position);
                }
                else
                {
                    _targetHealth?.ShowDamageAnimation(hero);
                }
            }

            if (_currentTarget != null)
                weapon?.Attack();
            else
                SwitchState(State.Idle);
        }

        public void InvokeAttackLichFireballFromAnimation()
        {
            if (_state == State.Appear || _state == State.Death)
                return;

            if (_weaponLichFireball != null)
            {
                _currentTarget = null;
                _targetHealth = null;
                _weaponLichFireball.SetTargetPoint(_targetPosition);
                
                if (_heroesBase != null)
                    _heroesBase.SpendManna(_weaponLichFireball.GetMannaLichCost());
                    
                _weaponLichFireball.Attack();
            }
        }

        // ===== МЕТОДЫ СОСТОЯНИЙ =====
        private void UpdateManualMove()
        {
            if (_agent == null)
                return;

            _repathCd -= Time.deltaTime;
            if (_repathCd <= 0f)
            {
                _agent.SetDestination(_manualDestination);
                _repathCd = repathRate;
            }

            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.05f)
            {
                IsManualControl = false;
                SwitchState(State.Idle);
            }
        }

        private void UpdateMoveToBoss()
        {
            if (_agent == null || _boss == null)
            {
                SwitchState(State.Idle);
                return;
            }

            if (_heroesBase != null && !_heroesBase.GetFindBoss())
            {
                SwitchState(State.Idle);
                return;
            }

            _repathCd -= Time.deltaTime;
            if (_repathCd <= 0f)
            {
                _agent.SetDestination(_boss.position);
                _repathCd = repathRate;
            }

            float dist = Vector3.Distance(transform.position, _boss.position);
            if (dist <= _attackingDistance + 0.1f)
                SwitchState(State.Idle);
        }

        private void UpdateChasing()
        {
            if (!HasValidTarget())
            {
                ClearTarget();
                return;
            }

            float dist = Vector3.Distance(transform.position, _currentTarget.position);
            if (dist <= AttackEnterDistance)
            {
                SwitchState(State.Attacking);
                return;
            }
            
            if (!CanMoveNow())
            {
                HardStopAgent();
                if (dist <= AttackEnterDistance)
                    SwitchState(State.Attacking);
                else
                    SwitchState(State.Chasing);
                return;
            }
            
            _repathCd -= Time.deltaTime;
            if (_repathCd <= 0f && _agent != null)
            {
                _agent.SetDestination(_currentTarget.position);
                _repathCd = repathRate;
            }
        }

        private void UpdateAttacking()
        {
            if (!canAttack)
                return;

            if (_currentTarget == null || _targetHealth == null || _targetHealth.IsDead)
            {
                ExitAttack_NoTarget();
                return;
            }

            float distSqr = (_currentTarget.position - transform.position).sqrMagnitude;
            float exitSqr = AttackExitDistance * AttackExitDistance;

            if (distSqr > exitSqr)
            {
                if (CanMoveNow())
                    SwitchState(State.Chasing);
                else
                    SwitchState(State.Chasing);
                return;
            }

            _attackCd -= Time.deltaTime;
            if (_attackCd > 0f) 
                return;

            _attackCd = 1f / Mathf.Max(0.01f, attackRate);

            if (_targetHealth.IsDead)
            {
                ExitAttack_NoTarget();
                return;
            }

            StartAttack();
        }

        private void UpdateRoaming()
        {
            if (SenseForEnemies())
                return;

            if (!_hasRoamPoint)
            {
                StartRoaming();
                return;
            }

            if (_agent == null)
                return;

            float deltaSqr = (_roamTarget - transform.position).sqrMagnitude;
            float stopDist = _agent.stoppingDistance + 0.05f;
            
            if (deltaSqr <= stopDist * stopDist)
            {
                _agent.isStopped = true;
                _roamWaitTimer = 0f;
                SwitchState(State.RoamingWait);
                return;
            }

            if (_agent.isStopped)
                _agent.isStopped = false;

            if (!_agent.pathPending && _agent.remainingDistance < 0.1f)
                _agent.SetDestination(_roamTarget);
        }

        private void UpdateRoamingWait()
        {
            if (SenseForEnemies())
                return;

            _roamWaitTimer += Time.deltaTime;

            if (_roamWaitTimer >= roamWaitTime)
            {
                _hasRoamPoint = false;
                StartRoaming();
            }
        }

        private void StartRoaming()
        {
            if (_heroesBase == null || !_heroesBase.canRoaming || _state == State.Death || _state == State.Appear)
                return;

            if (_agent == null || !_agent.enabled)
                return;

            if (!TryGetRandomPointAround(transform.position, sightRadius, out _roamTarget))
            {
                SwitchState(State.Idle);
                _hasRoamPoint = false;
                return;
            }

            _hasRoamPoint = true;
            _roamWaitTimer = 0f;

            _agent.stoppingDistance = roamStoppingDistance;
            _agent.isStopped = false;
            _agent.SetDestination(_roamTarget);

            SwitchState(State.Roaming);
        }

        // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====
        private bool SenseForEnemies()
        {
            _senseTimer -= Time.deltaTime;
            if (_senseTimer > 0f)
                return HasValidTarget();

            _senseTimer = senseInterval;

            if (IsManualControl)
                return false;

            if (HasValidTarget())
                return true;

            _currentTarget = null;
            _targetHealth = null;

            if (_heroesBase == null)
                return false;

            int myTeam = _heroesBase.GetTeam();
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, sightRadius, _senseHits, unitMask);
            
            Transform best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = _senseHits[i];
                if (col.transform == transform)
                    continue;

                var unit = col.GetComponentInParent<UnitLink>();
                if (!unit || unit.Hp == null) 
                    continue;

                var hp = unit.Hp;
                if (hp == null || hp.IsDead || hp.GetTeam() == myTeam)
                    continue;

                if (_heroesBase.GetHeroType() == HeroesBase.Hero.Skeleton && 
                    unit.GetComponent<HeroesBase>()?.GetOnTheTower() == true)
                    continue;

                float sqr = (col.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = hp.transform;
                }
            }

            if (best != null)
            {
                SetTarget(best);
                return true;
            }

            if (_currentTarget != null)
                ClearTarget();

            return false;
        }

        private void SetTarget(Transform t)
        {
            if (t == null) 
                return;

            var hp = t.GetComponent<HeroesBase>() ?? t.GetComponentInParent<HeroesBase>();
            if (hp == null)
                return;

            _currentTarget = hp.transform;
            _targetHealth = hp;

            if (!CanMoveNow())
            {
                HardStopAgent();
                float dist = Vector3.Distance(transform.position, _currentTarget.position);
                if (dist <= AttackEnterDistance) 
                    SwitchState(State.Attacking);
                else 
                    SwitchState(State.Chasing);
                return;
            }
            
            if (_agent != null)
            {
                _agent.stoppingDistance = AttackEnterDistance;
                _agent.isStopped = false;
                _agent.SetDestination(_currentTarget.position);
            }
            
            SwitchState(State.Chasing);
        }

        private void ClearTarget()
        {
            if (_state == State.Death) 
                return;

            _currentTarget = null;
            _targetHealth = null;
            weapon?.ClearTarget();
            
            if (_agent != null)
            {
                _agent.stoppingDistance = _attackingDistance;
                _agent.isStopped = true;
            }

            SwitchState(State.Idle);
        }

        private bool HasValidTarget()
        {
            return _currentTarget != null && 
                   _targetHealth != null && 
                   !_targetHealth.IsDead && 
                   _currentTarget.gameObject.activeInHierarchy;
        }

        private void EnsureBoss()
        {
            if (_boss != null || _heroesBase == null) 
                return;

            int myTeam = _heroesBase.GetTeam();
            _boss = BossRegistry.GetEnemyBoss(myTeam);
        }

        private void GoToBoss()
        {
            if (!CanMoveNow() || _controlledHero || _state == State.Appear || _state == State.Death)
                return;

            if (_heroesBase == null || !_heroesBase.GetFindBoss() || _heroesBase.GetIsBoss())
                return;

            if (_boss == null)
            {
                SwitchState(State.Idle);
                return;
            }

            if (_agent != null)
            {
                _agent.stoppingDistance = _attackingDistance;
                _agent.SetDestination(_boss.position);
            }
            
            SwitchState(State.MovingToBoss);
        }

        private void ExitAttack_NoTarget()
        {
            _currentTarget = null;
            _targetHealth = null;
            weapon?.ClearTarget();

            if (_agent != null)
            {
                _agent.isStopped = true;
                _agent.stoppingDistance = _attackingDistance;
            }

            SwitchState(State.Idle);
        }

        private void StartAttack()
        {
            if (!canAttack)
                return;

            weapon?.SetEnemyTarget(_currentTarget);
            weapon?.SetTargetHealth(_targetHealth);
        }

        private bool CanMoveNow()
        {
            return _heroesBase != null && _heroesBase.CanMove && _agent != null && _agent.enabled;
        }

        private void HardStopAgent()
        {
            if (_agent == null || !_agent.enabled) 
                return;
                
            _agent.isStopped = true; 
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
        }

        private void OnCanMoveChanged(bool canMove)
        {
            if (_agent == null) 
                return;

            if (canMove)
            {
                _agent.enabled = true;
                _agent.isStopped = false;
            }
            else
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.enabled = false;
            }

            if (!canMove)
            {
                IsManualControl = false;
                ClearTargetPosition();
                SwitchState(State.Idle);
            }
        }

        private void HandleDeath()
        {
            OnDeath();
        }

        private void OnDeath()
        {
            if (_deathHandled) 
                return;
                
            _deathHandled = true;
            SwitchState(State.Death);

            if (_agent != null)
            {
                _agent.isStopped = true;
                _agent.enabled = false;
            }

            var rb2D = GetComponent<Rigidbody2D>();
            if (rb2D) 
                rb2D.simulated = false;

            var cols = GetComponentsInChildren<Collider2D>();
            foreach (var c in cols) 
                c.enabled = false;

            if (weapon != null)
                weapon.gameObject.SetActive(false);

            if (_heroesBase != null && _heroesBase.GetHeroType() is HeroesBase.Hero.Skeleton or HeroesBase.Hero.SkeletonArcher)
            {
                if (_heroesBase.GibsPrefab != null)
                {
                    var go = Instantiate(_heroesBase.GibsPrefab, transform.position, Quaternion.identity);
                    var gibs = go.GetComponent<Heroes.BodyParts.Skeleton.GibsContainer2D>();
                    if (gibs != null)
                    {
                        Vector2 pushDir = _heroesBase.LastHitDir;
                        gibs.Scatter(Vector2.zero, pushDir);
                    }
                }
                Destroy(gameObject, 0f);
                return;
            }

            Destroy(gameObject, 7f);
        }

        private void SwitchState(State s)
        {
            if (s == State.Appear || (_state == State.Death && s != State.Death) || _state == s)
                return;

            if (!CanMoveNow())
            {
                HardStopAgent();
                _state = s;
                ChangeAnimation();
                StateChanged?.Invoke(_state);
                return;
            }

            _state = s;
            StateChanged?.Invoke(_state);

            if (_agent == null)
                return;

            switch (s)
            {
                case State.Roaming:
                case State.RoamingWait:
                    _agent.speed = _roamSpeed;
                    _agent.isStopped = false;
                    break;

                case State.MovingToBoss:
                case State.Chasing:
                    _agent.speed = _moveSpeed;
                    _agent.isStopped = false;
                    break;

                case State.Attacking:
                    _agent.isStopped = false;
                    _agent.speed = _moveSpeed;
                    break;

                case State.Idle:
                    _agent.isStopped = true;
                    _agent.speed = _moveSpeed;
                    break;

                case State.Death:
                    _agent.isStopped = true;
                    _agent.speed = 0f;
                    break;
            }

            ChangeAnimation();
        }

        private void ChangeAnimation()
        {
            if (_character == null)
                return;

            switch (_state)
            {
                case State.Idle:
                case State.RoamingWait:
                    _character.PlayIdle();
                    break;

                case State.MovingToBoss:
                    _character.PlayWalk();
                    break;

                case State.Chasing:
                    if (!CanMoveNow())
                        _character.PlayIdle();
                    else
                        _character.PlayWalk();
                    break;

                case State.Attacking:
                    _character.PlayAttack();
                    break;

                case State.Roaming:
                    _character.PlayRoaming();
                    break;

                case State.Death:
                    _character.PlayDeath();
                    break;

                case State.Appear:
                    _character.PlayAppear();
                    break;
            }
        }

        private bool TryGetRandomPointAround(Vector3 origin, float radius, out Vector3 result)
        {
            for (int i = 0; i < 10; i++)
            {
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

        private void UpdateTargetVisualization()
        {
            if (!showTargetDebug)
                return;

            if (targetMarkerPrefab != null && targetMarker == null)
            {
                targetMarker = Instantiate(targetMarkerPrefab);
                targetMarker.name = $"{gameObject.name}_TargetMarker";
            }

            if (targetMarker != null)
            {
                if (_currentTarget != null)
                {
                    targetMarker.transform.position = _currentTarget.position;
                    targetMarker.SetActive(true);
                }
                else
                {
                    targetMarker.SetActive(false);
                }
            }
        }

        // ===== ВИЗУАЛИЗАЦИЯ =====
        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(transform.position, Vector3.forward, sightRadius);

            Handles.color = Color.red;
            Handles.DrawWireDisc(transform.position, Vector3.forward, _attackingDistance);

            Handles.color = Color.cyan;
            Handles.DrawWireDisc(transform.position, Vector3.forward, _attackingDistance);
#endif
        }

        private void OnDrawGizmos()
        {
            if (!showTargetDebug || _currentTarget == null) 
                return;

            Gizmos.color = targetColor;
            Gizmos.DrawLine(transform.position, _currentTarget.position);
            Gizmos.DrawWireSphere(_currentTarget.position, 0.3f);

#if UNITY_EDITOR
            float distance = Vector3.Distance(transform.position, _currentTarget.position);
            Vector3 targetPos = _currentTarget.position;

            Handles.Label(_currentTarget.position + Vector3.up * 0.5f, 
                $"Цель: {_currentTarget.name}\n" +
                $"Позиция: ({targetPos.x:F1}, {targetPos.y:F1})");

            Handles.Label(transform.position + Vector3.up * 1f, 
                $"Дистанция: {distance:F2}");
#endif
        }

        private void DLog(string msg)
        {
            if (debugAI) 
                Debug.Log(msg);
        }

        // ===== ENUMS =====
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
    }
}