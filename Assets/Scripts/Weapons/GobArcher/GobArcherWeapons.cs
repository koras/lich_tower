using UnityEngine;

using Weapons.GobArcher;

namespace Weapons.Range
{
      public class GobArcherWeapons : WeaponBase, IRangedWeapon
    {
        [Header("Настройки выстрела")]
        
        [SerializeField] private Transform firePoint;

        [Header("Наведение")]
        [SerializeField] private float aimYOffset = 0.35f;
        [SerializeField] private float noTargetForward = 6f;

        [Header("Визуализация прицела")]
        [SerializeField] private bool showAimPrediction = true;
        [SerializeField] private Color aimColor = Color.yellow;
        [SerializeField] private int aimPoints = 10;

        [Header("Настройки")]
        [SerializeField] private LayerMask hitMask;
        private Transform _target;
        
       private ArrowPool _pool;
       private ArrowPool Pool
       {
           get
           {
               if (_pool == null) _pool = ArrowPool.Instance;
               return _pool;
           }
       }
       
       
        public void SetTarget(Transform target) => _target = target;

        // private void Awake()
        // {
        //     _pool = ArrowPool.Instance;
        //
        //     if (_pool == null)
        //         Debug.LogError("ArrowPool not found in scene");
        // }
        
        public override void SetEnemyTarget(Transform target)
        {
            base.SetEnemyTarget(target);   // если в базе что-то делает, не ломаем
            _target = target;
        }
        
        
 

        public override void Attack()
        {
            var pool = Pool;
            if (pool == null)
            {
                Debug.LogError("[GobArcherWeapons] ArrowPool.Instance is null");
                return;
            }

            Transform fp = firePoint != null ? firePoint : transform;
            Vector2 spawnPos = fp.position;

            var arrow = pool.Get(spawnPos, Quaternion.identity);
            if (arrow == null) return;

            if (_target != null)
            {
                 
                arrow.SetTargetHealth(_targetHealth);
                arrow.LaunchTowards(_target, aimYOffset);
                
            }
            else
            {
                arrow.SetTargetHealth(null);
                Vector2 fallback = spawnPos + (Vector2)fp.right * noTargetForward;
                arrow.LaunchTowards(fallback);
            }
        }

        
    }
}