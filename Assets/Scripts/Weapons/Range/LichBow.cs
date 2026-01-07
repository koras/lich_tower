using UnityEngine; 
using Weapons.Projectile;



namespace Weapons.Range
{
    public class LichBow : WeaponBase, IRangedWeapon
    {
        [Header("Настройки выстрела")]
        [SerializeField] private LichBombProjectile2D projectilePrefab; // префаб снаряда
        [SerializeField] private Transform muzzle;              // точка вылета
        
        
        [Header("время жизни снаряда")]    // время жизни снаряда
        [SerializeField] private LayerMask hitMask;             // кого можем ударить
        [SerializeField] private bool homing = false;           // самонаводящийся?
        [SerializeField] private float turnSpeed = 720f;        // град/сек при самонаводке
        
        
        
       
      //  [Header("Цель")] 
      //  [SerializeField]  private Transform _currentTarget;
        
        [Header("Высота над врагом")]
        [SerializeField] private float  _hSpawnPos = 2.5f;           // самонаводящийся?
        // цель нам может передать AI
        private Transform _target;

        public void SetTarget(Transform target) => _target = target;

        // public override void Attack()
        // {
        //     Debug.Log($"Attack");
        // }
        //
        public void SpawnBow(Transform targetFromAnim)
        {
            if (targetFromAnim != null) {
                _currentTarget = targetFromAnim;
                _target = targetFromAnim;
            }
            
        }
        
        public override void Attack()
        {
            
            Debug.Log($" InvokeAttack2 ======================");
 
            // проверка кулдауна в базе

            //     Debug.Log($" Удар  Bow");
            if (projectilePrefab == null)
            {
                Debug.LogError($"[{name}] Нет префаба снаряда у {nameof(LichBow)}");
                return;
            }

            // где носик оружия (если не задан — из центра)
            //  var spawnPos = muzzle != null ? muzzle.position : transform.position;
            
            Vector3 spawnPos;
            if (_currentTarget != null)
            {
                spawnPos = _currentTarget.position + Vector3.up * _hSpawnPos; // ← высота 1 единица
            }
            else
            {
                //  Debug.LogError($"_currentTarget == null");
                spawnPos = muzzle != null ? muzzle.position : transform.position;
            }
            
            var proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            
            var targetForInit = _currentTarget;
            
            Debug.LogWarning($"отключил LichBombProjectile2D222 Projectile2D!");
            // инициализация параметров снаряда
       //  proj.Init(
               // owner: GetComponentInParent<Heroes.HeroesBase>(),
           //     target: targetForInit,
           //     damage: Damage//, 
             //   homing: homing,
            //    turn: turnSpeed
         //   );

        }
    }
}