using UnityEngine; 
using Weapons.Projectile;



namespace Weapons.Range
{
    public class ShamanBowWeapon : WeaponBase, IRangedWeapon
    {
        [Header("Настройки выстрела, чем стреляем")]
        [SerializeField] private ShamanBombProjectile2D arrowPrefab; // префаб снаряда
        [SerializeField] private Transform firePoint;

        
        
        [Header("время жизни снаряда")]    // время жизни снаряда
        [SerializeField] private LayerMask hitMask;             // кого можем ударить
    //    [SerializeField] private bool homing = false;           // самонаводящийся?
   //     [SerializeField] private float turnSpeed = 720f;        // град/сек при самонаводке
        
        [Header("Наведение")]
        [SerializeField] private float aimYOffset = 0.35f;
        [SerializeField] private float noTargetForward = 6f;

        
        
     //   [Header("Высота над врагом")]
      //  [SerializeField] private float  _hSpawnPos = 1.5f;           // самонаводящийся?
 


        public override void Attack()
        {
            
            if (arrowPrefab == null)
            {
            //    Debug.LogError("[GobArcherWeapons] Не назначен префаб стрелы!");
                return;
            }

            Transform fp = firePoint != null ? firePoint : transform;
            Vector2 spawnPos = fp.position;

            var arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
            // Указываем стреле игнорировать коллайдер лучника
            arrow.SetIgnoreCollisions(transform); // Или transform.parent в зависимости от иерархии

            if (_currentTarget != null)
            {
                // здесь определённая цель
                arrow.LaunchTowards(_currentTarget, aimYOffset);
                arrow.SetTargetHealth(_targetHealth);

            //          Debug.Log($"Стрела запущена в цель");
            }
            else
            {
                Vector2 fallback = spawnPos + (Vector2)fp.right * noTargetForward;
                arrow.LaunchTowards(fallback);
               //    Debug.Log($"Стрела запущена вперёд: {fallback} в жопу");
            }



        }
    }
}