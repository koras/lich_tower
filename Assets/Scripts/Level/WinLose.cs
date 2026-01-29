using System;
using System.Collections;
using UnityEngine;
using AudioSystem;

namespace Animals
{
    /// <summary>
    /// AI для птицы.
    /// Всего два состояния:
    /// - Fly  : летит в случайную точку
    /// - Death: умирает, затем исчезает
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BirdAI : MonoBehaviour
    {
        public enum State
        {
            Fly,
            Death
        }
        [Header("Звуки")]
    
        [SerializeField] private bool playAnimalSounds = true;
        [SerializeField] private Vector3 soundOffset = Vector3.zero;

        [Header("Текущее состояние (debug)")]
        [SerializeField] private State _state = State.Fly;

        [Header("Параметры полёта")]
        [SerializeField] private float flySpeed = 2f;
        [SerializeField] private float roamRadius = 5f;
        [SerializeField] private float changeDirectionTimeMin = 2f;
        [SerializeField] private float changeDirectionTimeMax = 4f;

        [Header("Death fade")]
        [SerializeField] private float deathDelayBeforeFade = 2f;
        [SerializeField] private float fadeDuration = 1f;

        private Rigidbody2D _rb;
        private BirdVisualAnimal _visual;

        private Vector2 _flyTarget;
        private float _stateTimer;
        private bool _dead;

        private Coroutine _deathRoutine;

        public event Action<State> OnStateChanged;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;

            _visual = GetComponentInChildren<BirdVisualAnimal>();
        }

        private void Start()
        {
            PickNewFlyTarget();

       //    PlaySound(SoundId.AnimalBirdCry);
            
            SwitchState(State.Fly);
        }

        private void Update()
        {
            if (_dead) return;

            switch (_state)
            {
                case State.Fly:
                    UpdateFly();
                    break;
            }
        }

        /// <summary>
        /// Логика полёта.
        /// </summary>
        private void UpdateFly()
        {
            _stateTimer -= Time.deltaTime;

            Vector2 pos = transform.position;
            Vector2 dir = _flyTarget - pos;
            float dist = dir.magnitude;

            if (dist < 0.1f || _stateTimer <= 0f)
            {
           //     PlaySound(SoundId.AnimalBirdFly);
                PickNewFlyTarget();
                return;
            }

            dir.Normalize();
            _rb.linearVelocity = dir * flySpeed;

            // разворот визуала по направлению полёта
            if (_visual != null)
                _visual.SetFacing(dir.x < 0f);
        }

        /// <summary>
        /// Выбираем новую точку полёта.
        /// </summary>
        private void PickNewFlyTarget()
        {
            _flyTarget = (Vector2)transform.position
                         + UnityEngine.Random.insideUnitCircle * roamRadius;

            _stateTimer = UnityEngine.Random.Range(
                changeDirectionTimeMin,
                changeDirectionTimeMax
            );
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
                case State.Fly:
                    PickNewFlyTarget();
                    break;

                case State.Death:
                    _dead = true;
                    _rb.linearVelocity = Vector2.zero;
                    DisableColliders();

                    if (_deathRoutine != null)
                        StopCoroutine(_deathRoutine);

                    PlaySound(SoundId.AnimalTap);
                    _deathRoutine = StartCoroutine(DeathFadeAndDestroy());
                    break;
            }

            OnStateChanged?.Invoke(_state);
        }

        private void DisableColliders()
        {
            var cols = GetComponentsInChildren<Collider2D>(includeInactive: true);
            foreach (var c in cols)
                c.enabled = false;
        }

        private IEnumerator DeathFadeAndDestroy()
        {
            // даём анимации смерти поиграть
            if (deathDelayBeforeFade > 0f)
                yield return new WaitForSeconds(deathDelayBeforeFade);

            // если нет визуала — просто исчезаем
            if (_visual == null || !_visual.TrySetAlpha(1f))
            {
                Destroy(gameObject);
                yield break;
            }

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fadeDuration);
                _visual.TrySetAlpha(Mathf.Lerp(1f, 0f, k));
                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Убить птицу (клик, урон, судьба).
        /// </summary>
        public void Kill()
        {
            Debug.Log($"Птичку жалко");
            if (_dead) return;
            SwitchState(State.Death);
        }
        private void PlaySound(SoundId id)
        {
            if (!playAnimalSounds) return;
            if (AudioService.I == null) return;
            AudioService.I.Play(id, transform.position + soundOffset);
        }
        public State CurrentState => _state;
    }
}
