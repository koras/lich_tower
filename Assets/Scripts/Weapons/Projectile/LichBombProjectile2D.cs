using System;
using UnityEngine;
using Weapons;
using System.Collections.Generic;
using AudioSystem; 
using Unity.Cinemachine;

namespace Weapons.Projectile
{
    public class LichBombProjectile2D : MonoBehaviour
    {
        [Header("Компоненты")]
        [SerializeField] private Rigidbody2D rb;         // назначь в префабе
        [SerializeField] private Collider2D hitbox;      // можно оставить пустым, мы его отключим
                  
        [Header("Звуки")]
        [SerializeField] private bool playAnimalSounds = false;
        [SerializeField] private Vector3 soundOffset = Vector3.zero;

        [Header("Где земля горит после взрыва")]
        [SerializeField] private GameObject _prefabHole; // Префаб для спауна
        
        [Header("AOE Damage")]
        [SerializeField, Min(0.1f)] private float explosionRadius = 1.5f;     // радиус массового урона
        [SerializeField] private LayerMask damageMask;                        // слои юнитов (Heroes/Units)
        [SerializeField] private bool damageOwnerTeamOnlyEnemies = true;      // не бить своих
        
        private Animator _animator;
        
        
        [Header("Параметры полёта/попадания")]
        [SerializeField] private float hitRadius = 0.25f;   // радиус срабатывания возле цели
        [SerializeField] private float verticalOffset = -0.3f; // смещение вниз относительно цели
        
        [Header("Время жизни снаряда")] 
        [SerializeField] private float _lifeTimer = 4f;  
        
        [Header("Скорость снаряда")]
        [SerializeField] private float _speed = 10.5f;
        
        [Header("Дебаг визуализация")]
        [SerializeField] private bool showDebugMarkers = true;
        [SerializeField] private Color targetCircleColor = Color.yellow;
        [SerializeField] private Color hitPointColor = Color.red;
        [SerializeField] private float hitPointSize = 0.1f;
         
        [Header("Частица взрыва")]
        [SerializeField] private ParticleSystem hitFxPrefab; // твой ParticleLichBow
        [SerializeField] private Vector3 hitFxOffset = Vector3.zero; // если нужно чуть выше/ниже цели
        
        private Vector2 _targetPositionWithOffset; // позиция цели со смещением
        
        
        private const string BOOM = "boom";
        private const string FIRE = "fire"; 
        private const string EXIT = "exit";
         
        
        [SerializeField] private CinemachineImpulseSource impulse;
        
        
        // Параметры, что задаёт оружие
        private int _damage;
      //  private bool _homing;

        private Vector2 _target;
        private Transform  _targetHp;   // кеш здоровья цели
        private Heroes.HeroesBase _owner;
 
        private Vector2 _initialDirection; // начальное направление
        
         private bool _showBool = false;
        
        // НОВОЕ: Флаг что снаряд уже достиг цели
        private bool _hasReachedTarget = false;
        // === ИНИЦИАЛИЗАЦИЯ ИЗ ОРУЖИЯ ===
        public void InitFire(Vector2 initialDirection, int damage)
        {
            
            Debug.Log($"InitFire");
            _initialDirection = initialDirection;
             _target    = initialDirection;
            _damage    = damage; 

            if (!rb)     rb     = GetComponent<Rigidbody2D>();
            if (!hitbox) hitbox = GetComponent<Collider2D>();
            
            Debug.Log($"I was born2");
            
 

            // 🔴 Полностью выключаем столкновения — работаем только по дистанции
            if (hitbox) hitbox.enabled = false;

            if (rb)
            {
                rb.isKinematic = false;
                rb.gravityScale = 0f;   // ВЫКЛЮЧИЛ гравитацию - снаряд летит прямо
                // Задаём начальное направление и скорость
                Vector3 direction = new Vector3(_target.x, 0f, _target.y);
                
                
                _initialDirection = (direction - transform.position).normalized;
                rb.linearVelocity = _initialDirection * _speed;
            }
 
              
            // вычисляем позицию цели со смещением
            UpdateTargetPositionWithOffset();
            
        }
        
        private void Awake()
        {
            _animator = GetComponent<Animator>(); 
        }

        public void SetFire()
        {
            Debug.Log("SetFire");
            _animator.SetTrigger(FIRE);
        }

        
         
        
        private void SetBoom()
        {
            if (_showBool) return;
            _showBool = true;
             Debug.Log("SetBoom");
            SetHole();
            _animator.SetBool(BOOM, true ); 
        }

        
        private GameObject _spawnedHole;
        private bool _holeSpawned;
        public GameObject SetHole()
        {
            if (_holeSpawned) return _spawnedHole;
            _holeSpawned = true;

            if (_prefabHole == null)
            {
                Debug.LogError("[LichBombProjectile2D] _prefabHole не задан");
                return null;
            }

            _spawnedHole = Instantiate(_prefabHole, transform.position, Quaternion.identity);
            Debug.Log("SetHole");

            return _spawnedHole;
        }


        public void SetExit()
        {
            Debug.Log("SetExit"); 
            _animator.SetBool(EXIT, true );
            
            Destroy(gameObject);
        }


        private void FixedUpdate()
        {
            
            _lifeTimer -= Time.fixedDeltaTime;
            if (_lifeTimer <= 0f)
            {
                Debug.Log("Снаряд истёк по времени");
                Destroy(gameObject);
                return;
            }
            
            // НОВОЕ: Если уже достигли цели - не обновляем логику полёта
            if (_hasReachedTarget) return;
            
            // Визуализация для отладки
            if (Application.isPlaying && showDebugMarkers)
                DrawDebugGizmos();

 
            // Обновляем позицию цели со смещением
            UpdateTargetPositionWithOffset();
            
            // Проверяем радиус попадания к смещённой позиции
            float sqr = (_targetPositionWithOffset - (Vector2)transform.position).sqrMagnitude;
            if (sqr <= hitRadius * hitRadius)
            {
                DealDamageAndDie();
                return;
            }

            // таймер жизни
            _lifeTimer -= Time.fixedDeltaTime;
            if (_lifeTimer <= 0f) 
            {
                Debug.Log($"Снаряд истёк по времени");
                Destroy(gameObject);
            }
        }

        // вычисляет позицию цели со смещением вниз 
        private void UpdateTargetPositionWithOffset()
        {
           // if (_target != null)
          //  {
                _targetPositionWithOffset = new Vector2(
                    _target.x,  // X остаётся таким же как у цели
                    _target.y + verticalOffset  // Только Y меняется на verticalOffset
                );
         //   }
        }
        
 
        
        private void DealDamageAndDie()
        {
            // НОВОЕ: Устанавливаем флаг что достигли цели
            _hasReachedTarget = true;
            // НОВОЕ: Останавливаем движение
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.isKinematic = true; // Делаем кинематическим чтобы не двигался
            }
            
             
            DealAoeDamage(transform.position);
            // защита от дружеского огня
            // var myAI    = _owner ? _owner.GetComponent<Heroes.WarriorAI>() : null;
            // var otherAI = _targetHp ? _targetHp.GetComponent<Heroes.WarriorAI>() : null;

            // {
            //     Destroy(gameObject);
            //     return;
            // }
            SpawnHitFx();
        }
        
        private void DealAoeDamage(Vector2 center)
        {
            // Кто "мы" по команде
          //  var myAI = _owner ? _owner.GetComponent<Heroes.HeroesBase>() : GetComponent<Heroes.HeroesBase>();

          

            // Ищем всех в радиусе
            var hits = Physics2D.OverlapCircleAll(center, explosionRadius, damageMask);

            // Чтобы не ударить дважды, если несколько коллайдеров у одного юнита
            HashSet<Heroes.HeroesBase> damaged = new HashSet<Heroes.HeroesBase>();

            foreach (var col in hits)
            {
                
                Debug.Log("Перебираем"); 
                if (col == null) continue;

                // Находим здоровье
                Debug.Log("Находим героев"); 
                var hp = col.GetComponent<Heroes.HeroesBase>() ?? col.GetComponentInParent<Heroes.HeroesBase>();
                if (hp == null) continue;

                Debug.Log("Нашли"); 
                // Не бьём владельца (если надо)
                if (_owner != null && hp == _owner) continue;

                // Френдли-файр фильтр
                if (damageOwnerTeamOnlyEnemies)
                {
                    Debug.Log("Френдли-файр фильтр"); 
                    var other = hp.GetComponent<Heroes.HeroesBase>() ?? hp.GetComponentInParent<Heroes.HeroesBase>();
                 
                    if (other != null)
                    {
                        if (1 == other.GetTeam())
                            continue; // свои, пропускаем
                    }
                }

                // Дубликаты отсекаем
                if (!damaged.Add(hp)) 
                    continue;

                Debug.Log("гоу"); 
                // Наносим урон (подстрой под свой API здоровья)
                // Вариант 1: hp.TakeDamage(_damage, _owner);
                // Вариант 2: hp.ApplyDamage(_damage);
                // Вариант 3: hp.Damage(_damage);
              //  Vector2 hitDir = ((Vector2)hp.transform.position - (Vector2)transform.position).normalized;

                hp.TakeDamage(_damage,transform); // <-- ПОДСТАВЬ СВОЙ МЕТОД
            }
        }
        
        
        // Показываем тучку взрыва
        private void SpawnHitFx()
        {
            // теперь здесь анимация
            Debug.Log($"теперь здесь анимация");
             SetBoom();
             
             
             // 2) трясём камеру через Hole (там у тебя BombImpulse + CinemachineImpulseSource)
             var hole = SetHole(); // вернёт уже созданный, повторно не создаст
             var imp = hole != null ? hole.GetComponent<BombImpulse>() : null;

             if (imp == null) Debug.LogWarning("[LichBombProjectile2D] На Hole нет BombImpulse");
             else imp.Shake(0.3f);
        }

        // Визуализация в редакторе
        private void DrawDebugGizmos()
        {
            if (_target != null)
            {
                // Линия к смещённой позиции (красная)
                Debug.DrawLine(transform.position, _targetPositionWithOffset, Color.red);
                
                // Окружность вокруг смещённой позиции (жёлтая)
                DrawCircle(_targetPositionWithOffset, hitRadius, 12, targetCircleColor);
                
                // Линия от цели к смещённой позиции (синяя) - показывает смещение
             //   Debug.DrawLine(_target.position, _targetPositionWithOffset, Color.blue);
                
                // НОВОЕ: Мини-круг в точке попадания
                DrawHitPointMarker(_targetPositionWithOffset, hitPointSize, hitPointColor);
                
                // НОВОЕ: Крестик в точке попадания
                DrawCrossMarker(_targetPositionWithOffset, hitPointSize * 0.5f, hitPointColor);
            }
        }

        // Вспомогательный метод для рисования окружности
        private void DrawCircle(Vector2 center, float radius, int segments, Color color)
        {
            float angle = 0f;
            float angleIncrement = 360f / segments;
            Vector2 lastPoint = center + new Vector2(Mathf.Cos(0), Mathf.Sin(0)) * radius;
            
            for (int i = 1; i <= segments; i++)
            {
                angle += angleIncrement * Mathf.Deg2Rad;
                Vector2 nextPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Debug.DrawLine(lastPoint, nextPoint, color);
                lastPoint = nextPoint;
            }
        }

        // НОВЫЙ МЕТОД: Рисует мини-круг в точке попадания
        private void DrawHitPointMarker(Vector2 center, float size, Color color)
        {
            DrawCircle(center, size, 8, color);
        }

        // НОВЫЙ МЕТОД: Рисует крестик в точке попадания
        private void DrawCrossMarker(Vector2 center, float size, Color color)
        {
            // Горизонтальная линия крестика
            Debug.DrawLine(
                center + new Vector2(-size, 0), 
                center + new Vector2(size, 0), 
                color
            );
            
            // Вертикальная линия крестика
            Debug.DrawLine(
                center + new Vector2(0, -size), 
                center + new Vector2(0, size), 
                color
            );
        }
 
 
        
        public void IgnoreOwnerFor(Heroes.HeroesBase owner, float seconds) { /* не нужен в этой схеме */ }
    } 
}