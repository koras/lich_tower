using UnityEngine; 
using Heroes; 
using Spine.Unity; 
using AudioSystem; 



namespace Weapons.Projectile
{
    
    
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ShamanBombProjectile2D : MonoBehaviour
  {
      
      
      [Header("Звуки")]
      [SerializeField] private bool playAnimalSounds = true;
      [SerializeField] private Vector3 soundOffset = Vector3.zero;

      [Header("Анимация")] 
        
        
     // [SerializeField] public SkeletonAnimation skeletonAnimation;
      //[SerializeField] protected AnimationReferenceAsset idleAnimation;
 
       private Animator _animator;
      
     
      [Header("Огонь")]
       
     [SerializeField] private FX.ShamanFireFirst _shamanFireFirstPrefab; 
      [SerializeField] private Transform firePoint;
     
      [Header("Spine")]
      [SerializeField] private SkeletonAnimation _skeletonAnimation;
     
      [Header("Animation")]
      [SerializeField] private AnimationReferenceAsset _fire;
     
      
      
      [Header("Время жизни снаряда")] 
    //  [SerializeField] private float _lifeTimer = 4f;
      
        [SerializeField] private bool debugStraight = false;

        [Header("Парабола")]
        [SerializeField, Min(0.01f)]
        private float baseArcHeight = 1.5f;

        [SerializeField, Min(0f)]
        private float arcPerUnit = 0.2f;

        [Header("Время полёта")]
        [SerializeField, Min(0.1f)]
        private float flightSpeed = 8f;

        [SerializeField, Min(0.05f)]
        private float minDuration = 0.15f;

        [SerializeField, Min(0.05f)]
        private float maxDuration = 1.2f;

        [Header("Урон")] 
        [field: SerializeField] public int Damage { get; private set; } = 5;

     //   [SerializeField] private float hitRadius = 0.3f;  
      //  [SerializeField] private LayerMask damageMask = -1;
        [SerializeField] private Transform ignoreCollisionsWith; // Чей коллайдер игнорировать

     //   [Header("Визуализация траектории")]
     //   [SerializeField] private bool showTrajectory = true;
     //   [SerializeField] private Color trajectoryColor = Color.red;
     //   [SerializeField] private int trajectoryPoints = 20;

        [Header("Жизненный цикл")]
        [SerializeField] private bool destroyOnArrive = true;
        [SerializeField] private GameObject hitEffectPrefab;

        [Header("Отладка")]
        [SerializeField] private bool enableDebug = false;
        [SerializeField] private GameObject debugMarkerPrefab;
        private GameObject debugMarker;
        protected HeroesBase _targetHealth;

        private Vector2 _start;
        private Vector2 _end;
        private float _arcHeight;
        private float _duration;
        private float _t;
        private bool _launched;
        private bool _hitDetected;
        private Collider2D _arrowCollider;

        
        
       // public void SetAnimation(AnimationReferenceAsset animationCurrent, bool loop, float timeScale)
       // {
         //   skeletonAnimation.AnimationState.SetAnimation(0, animationCurrent, loop).TimeScale = timeScale;
      //  }

        
        
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _arrowCollider = GetComponent<Collider2D>();
            
            // Если есть коллайдер у стрелы - делаем его триггером
            if (_arrowCollider != null)
            {
                _arrowCollider.isTrigger = true;
            }
            
            if (_skeletonAnimation == null)
                _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);

        }


        /// <summary>
        /// Универсальный запуск анимации Spine.
        /// </summary>
        private void Play(AnimationReferenceAsset anim, bool loop, float timeScale)
        {
            if (anim == null || _skeletonAnimation == null) return;

            var entry =  _skeletonAnimation.AnimationState.SetAnimation(0, anim, loop);
            entry.TimeScale = timeScale;
        }
        
        void Start()
        {
            Play(_fire, true,1f);
        }
        

        public void LaunchTowards(Transform target, float yOffset = 0f)
        {
            if (!target) 
            {
            //    Debug.LogError("❌ Стрела: цель не назначена!");
                return;
            }

            Vector2 end = (Vector2)target.position + Vector2.up * yOffset;
            LaunchTowards(end);

         //   if (enableDebug) Debug.Log($"🎯 Стрела запущена в цель: {target.name}, позиция: {end}");

            // Маркер цели
            if (debugMarkerPrefab != null)
            {
                if (debugMarker == null)
                    debugMarker = Instantiate(debugMarkerPrefab);
                debugMarker.transform.position = end;
            }
            
            
         //   if(idleAnimation != null){ 
         //       SetAnimation(idleAnimation, true, 0.7f);
         //   }
          //  else
         //   {
         //       Debug.Log($"🎯 не установлен idleAnimation");
        //    }
        }

        public void LaunchTowards(Vector2 target)
        {
            _start = transform.position;
            _end = target;

            float dist = Vector2.Distance(_start, _end);
            _arcHeight = baseArcHeight + dist * arcPerUnit;
            _duration = Mathf.Clamp(dist / Mathf.Max(0.01f, flightSpeed), minDuration, maxDuration);

            _t = 0f;
            _launched = true;
            _hitDetected = false;

            if (enableDebug) 
            {
               // Debug.Log($"🏹 Стрела запущена: " +
              //           $"старт: {_start}, цель: {_end}, " +
              //           $"дистанция: {dist:F2}, время: {_duration:F2}");
            }
            

        }

        // Установить чьи коллайдеры игнорировать
        public void SetIgnoreCollisions(Transform ignoreTransform)
        {
            ignoreCollisionsWith = ignoreTransform;
        }

        private void Update()
        {
            if (!_launched) return;

            _t += Time.deltaTime / _duration;
            
            if (_t >= 1f)
            {
                ArriveAtDestination();
                return;
            }

            if (debugStraight)
            {
                transform.position = Vector2.Lerp(_start, _end, _t);
                transform.right = (_end - _start).normalized;
            }
            else
            {
                // ПАРАБОЛИЧЕСКАЯ ТРАЕКТОРИЯ
                Vector2 flatPos = Vector2.Lerp(_start, _end, _t);
                float h = 4f * _arcHeight * _t * (1f - _t);
                Vector2 pos = new Vector2(flatPos.x, flatPos.y + h);
                transform.position = pos;

                // Поворот стрелы по направлению движения
                float dt = 0.01f;
                float t2 = Mathf.Clamp01(_t + dt);
                Vector2 flatPos2 = Vector2.Lerp(_start, _end, t2);
                float h2 = 4f * _arcHeight * t2 * (1f - t2);
                Vector2 pos2 = new Vector2(flatPos2.x, flatPos2.y + h2);

                Vector2 dir = pos2 - pos;
                if (dir.sqrMagnitude > 0.0001f)
                    transform.right = dir.normalized;
            }
        }

 

        /// <summary>
        /// Обрабатывает прибытие стрелы в конечную точку
        /// </summary>
        private void ArriveAtDestination()
        {
            transform.position = _end;

            if (enableDebug) Debug.Log($"🏁 Стрела достигла конечной точки: {_end}");

            // Проверяем попадание в конечной точке
            if (!_hitDetected)
            {
                CheckHitAtDestination();
            }

            if (destroyOnArrive)
            {
                if (enableDebug) Debug.Log("🗑️ Уничтожаем стрелу (прибытие)");
                Destroy(gameObject);
            }
        }
        private void PlaySound(SoundId id)
        {
            if (!playAnimalSounds) return;
            if (AudioService.I == null) return;
            AudioService.I.Play(id, transform.position + soundOffset);
        }
        /// <summary>
        /// Проверяет попадание в конечной точке траектории
        /// </summary>
        private void CheckHitAtDestination()
        {

            ApplyDamageTargetHealth();
            return;
            // акомментировано специально, работаем на цель
            
        }

        private void ApplyDamageTargetHealth()
        {

            PlaySound(SoundId.HitAttackFireBall);
            // Наносим урон
            if(_targetHealth != null)
                _targetHealth.TakeDamage(Damage);
            
 
         Transform fp = firePoint != null ? firePoint : transform;
         Vector2 spawnPos = fp.position;

             var arrow = Instantiate(_shamanFireFirstPrefab, spawnPos, Quaternion.identity);
             arrow.transform.position = spawnPos;
             
             Destroy(gameObject);
            
        }
        

        public void SetTargetHealth(HeroesBase  targetHealth)
        {
     //       Debug.Log($"🎯 Установили цель");
     //     if(_targetHealth != null)
                _targetHealth = targetHealth;
        }
        
        
    }
}