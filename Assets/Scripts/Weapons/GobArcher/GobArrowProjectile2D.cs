using UnityEngine;
using Heroes;

using AudioSystem; 
namespace Weapons.GobArcher
{
    [DisallowMultipleComponent]
    public class GobArrowProjectile2D : MonoBehaviour, IProjectile
    {
        
        [Header("Звуки")]
        [SerializeField] private bool playAnimalSounds = true;
        [SerializeField] private Vector3 soundOffset = Vector3.zero;


        
        
        [SerializeField] private bool debugStraight = false;

        [Header("Парабола")] [SerializeField, Min(0.01f)]
        private float baseArcHeight = 1.5f;

        [SerializeField, Min(0f)] private float arcPerUnit = 0.2f;

        [Header("Время полёта")] [SerializeField, Min(0.1f)]
        private float flightSpeed = 8f;

        [SerializeField, Min(0.05f)] private float minDuration = 0.15f;
        [SerializeField, Min(0.05f)] private float maxDuration = 1.2f;

        [Header("Урон")]
        [field: SerializeField]
        public int Damage { get; private set; } = 5;

        [Header("Жизненный цикл")] [SerializeField]
        private bool destroyOnArrive = true;

        [SerializeField] private GameObject hitEffectPrefab;

        [Header("Время жизни и исчезновение")] [SerializeField, Min(0.1f)]
        private float maxLifetime = 3f;

        [SerializeField, Min(0.1f)] private float fadeOutDuration = 0.5f;
        [SerializeField] private float groundHitThreshold = 0.5f; // максимальное расстояние для попадания в цель

        // В идеале это вынести из стрелы, но оставим как есть
        protected HeroesBase _targetHealth;

        private Vector2 _start;
        private Vector2 _end;
        private Vector2 _delta; // end - start
        private float _arcHeight;
        private float _duration;
        private float _invDuration; // 1 / duration
        private float _t;
        private bool _launched;
        private bool _hitApplied;
        private bool _stuckInGround;
        private float _currentLifetime;

        private Vector2 _prevPos; // для поворота
        private Collider2D _arrowCollider;
        private SpriteRenderer _spriteRenderer;
        private Rigidbody2D _rb;

        private ArrowPool _pool;
        public void SetPool(ArrowPool pool) => _pool = pool;

        private void Awake()
        {
            _arrowCollider = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();

            if (_arrowCollider != null)
                _arrowCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            // важно для пула: сброс состояния
            _launched = false;
            _hitApplied = false;
            _stuckInGround = false;
            _t = 0f;
            _currentLifetime = 0f;
            _targetHealth = null;

            // Сброс визуальных параметров
            if (_spriteRenderer != null)
            {
                Color c = _spriteRenderer.color;
                c.a = 1f;
                _spriteRenderer.color = c;
            }

            if (_rb != null)
                _rb.simulated = false;
        }

        private void Despawn()
        {
            if (_pool != null) _pool.Release(this);
        }

        public void SetTargetHealth(HeroesBase targetHealth) => _targetHealth = targetHealth;

        public void LaunchTowards(Transform target, float yOffset = 0f)
        {
            if (!target) return;
            LaunchTowards((Vector2)target.position + Vector2.up * yOffset);
        }

        public void LaunchTowards(Vector2 target)
        {
            _start = transform.position;
            _end = target;
            _delta = _end - _start;

            // расчёт длины только 1 раз на запуск
            float dist = _delta.magnitude;

            _arcHeight = baseArcHeight + dist * arcPerUnit;

            _duration = Mathf.Clamp(dist / Mathf.Max(0.01f, flightSpeed), minDuration, maxDuration);
            _invDuration = 1f / _duration;

            _t = 0f;
            _launched = true;
            _hitApplied = false;
            _stuckInGround = false;

            _prevPos = _start;

            if (_rb != null)
                _rb.simulated = true;
        }

        private void Update()
        {
            _currentLifetime += Time.deltaTime;

            if (_currentLifetime >= maxLifetime)
            {
                StartFadeOut();
                return;
            }

            if (_stuckInGround)
            {
                // Стрела воткнута в землю - ждём конца времени жизни
                return;
            }

            if (!_launched) return;

            _t += Time.deltaTime * _invDuration;
            if (_t >= 1f)
            {
                ArriveAtDestination();
                return;
            }

            Vector2 pos;

            if (debugStraight)
            {
                pos = _start + _delta * _t;
            }
            else
            {
                // flat = start + delta * t
                Vector2 flat = _start + _delta * _t;

                // h(t) = 4 * arcHeight * t * (1-t)
                float h = 4f * _arcHeight * _t * (1f - _t);

                pos = new Vector2(flat.x, flat.y + h);
            }

            transform.position = pos;

            // поворот по фактическому движению без второго семпла
            Vector2 dir = pos - _prevPos;
            if (dir.sqrMagnitude > 0.000001f)
                transform.right = dir;

            _prevPos = pos;
        }

        private void ArriveAtDestination()
        {
            transform.position = _end;

            if (!_hitApplied)
            {
                _hitApplied = true;

                // Проверяем, достаточно ли близко к цели для нанесения урона
                bool targetHit = false;
                if (_targetHealth != null && !_targetHealth.IsDead)
                {
                    float distanceToTarget = Vector2.Distance(_end, (Vector2)_targetHealth.transform.position);

                    if (distanceToTarget <= groundHitThreshold)
                    {
                      //  Debug.Log("[GobArcherWeapons] Попадание в цель!");
                        _targetHealth.TakeDamage(Damage);
                        targetHit = true;
                    }
                    else
                    {
                      //  Debug.Log($"[GobArcherWeapons] Цель ушла, расстояние: {distanceToTarget:F2}");
                    }
                }

                // Если не попали в цель, стрела втыкается в землю
                if (!targetHit)
                {
                    StickInGround();
                }
                else if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, _end, Quaternion.identity);
                }

                _launched = false;

                // Не уничтожаем сразу, если стрела воткнулась в землю
                if (destroyOnArrive && !_stuckInGround)
                    Despawn();
            }
        }
        private void PlaySound(SoundId id)
        {
            if (!playAnimalSounds) return;
            if (AudioService.I == null) return;
            AudioService.I.Play(id, transform.position + soundOffset);
        }
        private void StickInGround()
        {
            _stuckInGround = true;

            // Останавливаем физику, если есть
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.simulated = false;
            }

            // Немного "втыкаем" стрелу в землю (немного ниже конечной позиции)
            transform.position = new Vector2(_end.x, _end.y - 0.1f);

            // Можно добавить небольшую случайную ротацию для реализма
            float randomRotation = Random.Range(-10f, 10f);
            transform.Rotate(0, 0, randomRotation);
            PlaySound(SoundId.ArcherOnGrass);
            Debug.Log("[GobArrow] Стрела воткнулась в землю");
        }

        private void StartFadeOut()
        {
            if (!_stuckInGround && _launched)
            {
                // Если стрела всё ещё летит, просто деспавним
                Despawn();
                return;
            }

            StartCoroutine(FadeOutAndDespawn());
        }

        private System.Collections.IEnumerator FadeOutAndDespawn()
        {
            if (_spriteRenderer == null)
            {
                Despawn();
                yield break;
            }

            float fadeTimer = 0f;
            Color startColor = _spriteRenderer.color;

            while (fadeTimer < fadeOutDuration)
            {
                fadeTimer += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeOutDuration);

                Color newColor = startColor;
                newColor.a = alpha;
                _spriteRenderer.color = newColor;

                yield return null;
            }

            Despawn();
        }

        // Опционально: метод для принудительного запуска исчезновения
        public void StartDespawn()
        {
            StartFadeOut();
        }
    }
}