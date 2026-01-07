using System;
using System.Collections;
using UnityEngine;

using UnityEngine.AI;

namespace Animals
{
    /// <summary>
    /// AI для животных.
    /// Управляет состояниями: idle, idle2 (копает), walk, run, death.
    /// НЕ занимается анимациями напрямую — только логикой.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class AnimalsAI : MonoBehaviour
    {
        public enum State
        {
            Idle,
            IdleDig,
            Walk,
            Run,
            Death
        }

        [Header("Текущее состояние (debug)")] [SerializeField]
        private State _state = State.Idle;

        [Header("Параметры поведения")] 
       
        [SerializeField] private float idleTimeMin = 1.5f;
        [SerializeField] private float idleTimeMax = 4f;

        [SerializeField] private float walkTimeMin = 2f;
        [SerializeField] private float walkTimeMax = 5f;

        [SerializeField] private float runChance = 0.25f; // шанс побежать вместо walk
        [SerializeField] private float digChance = 0.3f; // шанс idle2 (копать)

        [Header("Движение")] [SerializeField] private float walkSpeed = 0.5f;
        [SerializeField] private float runSpeed = 1.5f;
        [SerializeField] private float roamRadius = 3f;

        
        [Header("Death fade")]
        [SerializeField, Min(0f)] private float deathDelayBeforeFade = 3f; // ждём перед исчезновением
        [SerializeField, Min(0.05f)] private float fadeDuration = 1.2f;    // за сколько “растворяемся”

        private Rigidbody2D _rb;
        private VisualAnimal _visual;
        
        private Coroutine _deathRoutine;
        
        [SerializeField] private Transform _visualRoot; // сюда закидывай объект со SkeletonAnimation
        private bool _facingLeft;
        
        private float _stateTimer;
        private Vector2 _moveTarget;
        private bool _dead;

        public event Action<State> OnStateChanged;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;

            _visual = GetComponentInChildren<VisualAnimal>();
            
          //  if (_visualRoot == null) _visualRoot = transform;
        }

        private void Start()
        {
            SwitchState(State.Idle);
        }

        private void Update()
        {
            if (_dead) return;

            _stateTimer -= Time.deltaTime;

            switch (_state)
            {
                case State.Idle:
                case State.IdleDig:
                    if (_stateTimer <= 0f)
                        DecideNextAction();
                    break;

                case State.Walk:
                case State.Run:
                    UpdateMovement();
                    break;
            }
        }

        /// <summary>
        /// Выбираем следующее действие после idle.
        /// </summary>
        private void DecideNextAction()
        {
            float r = UnityEngine.Random.value;

            if (r < digChance)
            {
                SwitchState(State.IdleDig);
                return;
            }

            if (r < digChance + runChance)
            {
                StartMove(State.Run);
            }
            else
            {
                StartMove(State.Walk);
            }
        }

        /// <summary>
        /// Запуск движения (walk или run).
        /// </summary>
        private void StartMove(State moveState)
        {
            _moveTarget = (Vector2)transform.position 
                          + UnityEngine.Random.insideUnitCircle * roamRadius;
            SwitchState(moveState);
        }

        /// <summary>
        /// Логика движения к точке.
        /// </summary>
        private void UpdateMovement()
        {
            float speed = _state == State.Run ? runSpeed : walkSpeed;

            Vector2 pos = transform.position;
            Vector2 dir = (_moveTarget - pos);
            float dist = dir.magnitude;

            if (dist < 0.05f)
            {
                _rb.linearVelocity = Vector2.zero;
                SwitchState(State.Idle);
                return;
            }

            dir.Normalize();
            _rb.linearVelocity = dir * speed;

            // флип визуала
           // if (_visual != null)
             //   _visual.SetFacing(dir.x < 0);

            if (_visual != null)
            {
                _visual.SetFacing(dir.x < 0);

              //  bool faceLeft = _ai.turnToFace();
              //  _visualRoot.localRotation = faceLeft ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            }
        }
 
        /// <summary>
        /// Смена состояния.
        /// </summary>
        private void SwitchState(State newState)
        {
            if (_state == newState) return;

            _state = newState;

            switch (_state)
            {
                case State.Idle:
                    _stateTimer = UnityEngine.Random.Range(idleTimeMin, idleTimeMax);
                    _rb.linearVelocity = Vector2.zero;
                    break;

                case State.IdleDig:
                    _stateTimer = UnityEngine.Random.Range(1f, 3f);
                    _rb.linearVelocity = Vector2.zero;
                    break;

                case State.Walk:
                    _stateTimer = UnityEngine.Random.Range(walkTimeMin, walkTimeMax);
                    break;

                case State.Run:
                    _stateTimer = UnityEngine.Random.Range(1.5f, 3f);
                    break;

                case State.Death:
                    _rb.linearVelocity = Vector2.zero;
                    _dead = true;

                    // чтобы труп не ловил клики/коллизии и не мешал
                    DisableColliders();

                    // запускаем исчезновение
                    if (_deathRoutine != null) StopCoroutine(_deathRoutine);
                    _deathRoutine = StartCoroutine(DeathFadeAndDestroyRoutine());
                    break;
            }

            OnStateChanged?.Invoke(_state);
        }
        
        private void DisableColliders()
        {
            var cols = GetComponentsInChildren<Collider2D>(includeInactive: true);
            foreach (var c in cols) c.enabled = false;
        }
        private IEnumerator DeathFadeAndDestroyRoutine()
        {
            // 1) подождать после смерти (пусть анимация “поиграет”)
            if (deathDelayBeforeFade > 0f)
                yield return new WaitForSeconds(deathDelayBeforeFade);

            // 2) плавно уменьшаем прозрачность
            float t = 0f;

            // если VisualAnimal/Spine нет, просто удаляем
            if (_visual == null || !_visual.TrySetAlpha(1f))
            {
                Destroy(gameObject);
                yield break;
            }
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float k = fadeDuration <= 0f ? 1f : Mathf.Clamp01(t / fadeDuration);
                float alpha = Mathf.Lerp(1f, 0f, k); 
                _visual.TrySetAlpha(alpha);
                yield return null;
            }

            // 3) убрать объект совсем
            Destroy(gameObject);
        }
        /// <summary>
        /// Убить животное.
        /// </summary>
        public void Kill()
        {
            if (_dead) return;
            SwitchState(State.Death);
        }

        public State CurrentState => _state;
    }
}