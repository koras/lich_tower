using UnityEngine;
using Weapons;

using Weapons.Projectile;

namespace Heroes
{
    /// <summary>
    /// Способность фаербол. Вешается ТОЛЬКО на Лича.
    /// Управление прицелом идет из input-контроллера (PinchToZoomAndPan).
    /// Реальный каст делается через Animation Event -> InvokeFireballFromAnimation().
    /// </summary>
    public class LichFireballAbility : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int mannaCost = 20;
        [SerializeField, Min(10)] private int _damage = 50;
         

        [Header("Aim marker (прицел)")]
        [SerializeField] private GameObject aimPrefab;
        private GameObject _aimInstance;
        
        [Header("Настройки выстрела, чем стреляем")]
        [SerializeField] private LichBombProjectile2D _arrowPrefab; 
        [SerializeField] private Transform firePoint;

        [SerializeField] private float heightSpawnOffset = 1.5f;

        
        [Header("Fireball weapon / spawner")]
        [SerializeField] private WeaponBase fireballWeapon;
        // Если у Лича несколько оружий, перетащи сюда конкретное оружие фаербола.

        [Header("Optional: stop AI while casting")]
        [SerializeField] private bool blockAutoAttackWhileCasting = true;

        private HeroesBase _hero;
        private WarriorAI _ai;
        private BaseVisualCharacter _visual;

        private bool _isTargeting;
        private bool _hasTargetPoint;
        private Vector3 _targetWorldPos;

        private void Awake()
        {
            _hero = GetComponent<HeroesBase>();
            _ai = GetComponent<WarriorAI>();
            _visual = GetComponentInChildren<BaseVisualCharacter>(true);

            // fallback: если у Лича вообще одно оружие и это фаербол
            if (fireballWeapon == null)
                fireballWeapon = GetComponentInChildren<WeaponBase>(true);
        }

        /// <summary>
        /// Хватает ли маны на старт прицеливания.
        /// </summary>
        public bool CanStart()
        {
            return _hero != null && _hero.HasManna(mannaCost);
        }

        /// <summary>
        /// Включить режим прицеливания: показать/создать прицел.
        /// ВАЖНО: точка задаётся отдельно через UpdateTarget().
        /// </summary>
        public void StartTargeting()
        {
            if (_isTargeting) return;

            if (!CanStart())
            {
                Debug.Log("Недостаточно маны на Fireball");
                return;
            }

            _isTargeting = true;
            _hasTargetPoint = false;

            if (aimPrefab != null && _aimInstance == null)
                _aimInstance = Instantiate(aimPrefab);

            if (_aimInstance != null)
                _aimInstance.SetActive(true);

            // Если хочешь, чтобы Лич перестал автоатаковать, пока целится:
            // if (blockAutoAttackWhileCasting && _ai != null) _ai.canAttack = false;
        }

        /// <summary>
        /// Двигать прицел по карте (вызывается из input, пока палец/мышь двигается).
        /// </summary>
        public void UpdateTarget(Vector3 worldPos)
        {
            if (!_isTargeting) return;

            worldPos.z = 0f;
            _targetWorldPos = worldPos;
            _hasTargetPoint = true;

            if (_aimInstance != null)
                _aimInstance.transform.position = _targetWorldPos;
        }

        public void Attack()
        {
            Debug.Log($"Attack LichFireballAbility");
            if (_arrowPrefab == null)
            {
                
                Debug.Log($"_arrowPrefab == null");
                return;
            }
 


            if (_aimInstance == null)
            {
                
                Debug.Log($"ель пропала");
            }
 

            Transform fp = firePoint != null ? firePoint : transform;
            Vector2 spawnPos = fp.position;

            Debug.Log($"Attack()");
            // Если есть цель, спавним НАД ЕЁ ГОЛОВОЙ
            if (_targetWorldPos != null)
            {
                
                Debug.Log($"spawn aim");
                Vector2 targetPos = _targetWorldPos;
                spawnPos = new Vector2(targetPos.x, targetPos.y + heightSpawnOffset);
           

            var arrow = Instantiate(_arrowPrefab, spawnPos, Quaternion.identity);
            
             
            arrow.InitFire(_targetWorldPos, _damage);


            Debug.Log($"Списываем манну");
            _hero.SpendManna(mannaCost);
            }

        }

        /// <summary>
        /// Подтверждение цели (палец/мышь отпустили).
        /// Сохраняем финальную точку, прячем прицел и запускаем анимацию каста.
        /// Реальный фаербол будет создан на Animation Event -> InvokeFireballFromAnimation().
        /// </summary>
        public void ConfirmTarget(Vector3 worldPos)
        {
            if (!_isTargeting) return;

            Debug.Log($"ConfirmTarget Запуск фаербола по идее");
 
            // на всякий случай обновим цель финальной точкой
            UpdateTarget(worldPos);

            _isTargeting = false;

            if (_aimInstance != null)
                _aimInstance.SetActive(false);

            // На время каста можно запретить автоатаки (по желанию)
            if (blockAutoAttackWhileCasting && _ai != null)
         
                Debug.Log($"На время каста можно запретить автоатаки (по желанию)");
            _ai.SetCanAttack(false);
            _ai.SetTargetPosition(worldPos);
            // Запускаем анимацию каста магии.
            // ВАЖНО: если у тебя нет PlayCastMagicLich(), то:
            // 1) добавь метод в BaseVisualCharacter
            // или
            // 2) дерни animator.SetTrigger("CastFireball") прямо тут (если есть доступ к Animator).
            _ai.SetIsStoppedAgent();
            _visual?.PlayCastMagicLich();
        }

        /// <summary>
        /// Это вызывается ИМЕННО из Animation Event на нужном кадре каста.
        /// Тут списываем ману и вызываем реальный фаербол.
        /// </summary>
        public void InvokeFireballFromAnimation()
        {
            // 0) Проверяем что есть цель (иначе странно)
            if (!_hasTargetPoint)
            {
                Debug.LogWarning("InvokeFireballFromAnimation вызван, но цель не задана.");
                FinishCastCleanup();
                return;
            }

            // 1) Списываем ману (еще раз проверяем, на случай если мана могла измениться)
            if (_hero== null || !_hero.SpendManna(mannaCost))
            {
                Debug.LogWarning("Каст фаербола сорван: маны нет (или уже списали).");
                FinishCastCleanup();
                return;
            }

            // 2) Кастим фаербол
            if (fireballWeapon == null)
            {
                Debug.LogWarning("Не назначено оружие/спавнер фаербола (fireballWeapon == null).");
                FinishCastCleanup();
                return;
            }

            // ВАЖНО: тут нужно вызвать конкретный метод твоего оружия/фаербола.
            // Я НЕ могу угадать его сигнатуру, поэтому есть 2 нормальных варианта:

            // ВАРИАНТ A (лучший): сделай интерфейс/метод у оружия:
            // public void CastToPoint(Vector3 point)
            // и вызывай его:
            // ((FireballWeapon)fireballWeapon).CastToPoint(_targetWorldPos);

            // ВАРИАНТ B (универсально): сделай у WeaponBase виртуальный метод:
            // public virtual void CastToPoint(Vector3 point) {}
            // и переопредели в оружии фаербола.
            // Тогда тут можно так:
            // fireballWeapon.CastToPoint(_targetWorldPos);

            Debug.Log($"🔥 Fireball cast to: {_targetWorldPos}");

            FinishCastCleanup();
        }

        /// <summary>
        /// Отмена прицеливания (например, если отпустили палец не на земле).
        /// </summary>
        public void CancelTargeting()
        {
            _isTargeting = false;
            _hasTargetPoint = false;

            if (_aimInstance != null)
                _aimInstance.SetActive(false);

            // если ты блокировал автоатаку на время прицеливания/каста
            if (_ai != null) 
                _ai.SetCanAttack(true);
        }

        private void FinishCastCleanup()
        {
            // возвращаем автоатаку после каста
            if (_ai != null)
                _ai.SetCanAttack(true);
        }

        public bool IsTargeting => _isTargeting;
        public int ManaCost => mannaCost;
    }
}
