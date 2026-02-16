using UnityEngine; 
using Heroes; 
using Damage; 
using Spine.Unity; 
using AudioSystem; 



namespace Weapons.Projectile
{
    
 

    public class LichAttack :  WeaponBase
  {
        private bool _hitAppliedThisSwing; 
        
        [SerializeField] private LichProjectileFire projectilePrefab;
        
        [SerializeField] private Transform firePoint;
        [SerializeField] private int projectileDamage = 30;
        [SerializeField] private float projectileSpeed = 15f;
        [SerializeField] private float projectileMaxDistance = 20f;
        [SerializeField] private LayerMask enemyLayerMask;
 
        
        private void Awake()
        {
            if (_polygonCollider2D == null)
                _polygonCollider2D = GetComponent<PolygonCollider2D>();
                _polygonCollider2D.isTrigger = true;  // <— важно
            AttackColliderTurnOff();
        }
        
  

        public override void LichAttackDirection(Vector2 aimDirection)
        {
            Debug.Log($"[LichAttack] LichAttackDirection");
            if (!canAttack)
            {
                Debug.Log($"[LichAttack] Запрет на атаку");
                Debug.Log($"[LichAttack] LichAttackDirection not access");
                return;
            }

            if (projectilePrefab == null)
            {
                Debug.LogError("[LichAttack] projectilePrefab не назначен!");
                Debug.Log($"[LichAttack] LichAttackDirection not access projectilePrefab");
                return;
            }

            // Получаем команду владельца
            HeroesBase owner = GetComponentInParent<HeroesBase>();
            if (owner == null)
            {
               // Debug.LogError("[LichAttack] Владелец оружия не найден!");
                Debug.Log($"[LichAttack] LichAttackDirection not access projectilePrefab awn");
                return;
            }

            // Проверяем, достаточно ли маны
      

            // Спавним снаряд
            Vector2 spawnPosition = firePoint != null ? firePoint.position : transform.position;
            LichProjectileFire projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            
            // Инициализируем снаряд
            projectile.Initialize(
                aimDirection, 
                projectileDamage, 
                owner.GetTeam(), 
                owner.transform
            );

            // Тратим ману
            //owner.SpendManna(GetMannaCost());

            Debug.Log($"[LichAttack] Выстрел в направлении: {aimDirection}, команда: {owner.GetTeam()}");
        }

        
        /// <summary>
        /// Закрепляем цель и кешируем её здоровье.
        /// БЕЗ кулдауна, это просто выбор цели.
        /// </summary>
        public override void SetEnemyTarget(Transform currentTarget)
        {
            _currentTarget = currentTarget;
        }
        private void AttackColliderTurnOff() => _polygonCollider2D.enabled = false;
    }
}