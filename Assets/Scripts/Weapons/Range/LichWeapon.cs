using UnityEngine; 
using Weapons.Projectile;



namespace Weapons.Range
{
    public class LichWeapon : WeaponBase, IRangedWeapon
    {
        [Header("Настройки выстрела, чем стреляем")]
        [SerializeField] private LichBombProjectile2D _arrowPrefabLich; // префаб снаряда
        [SerializeField] private Transform firePoint;

        private Vector3 _targetPosition;
         
        [SerializeField] private float heightSpawnOffset = 1.5f;
        
        
        
        [Header("Сколько манны стоит снаряд")]    // время жизни снаряда
      //  [SerializeField] 
        private int _mannaCost = 35;   // стоимость манны


        [Header("время жизни снаряда")]    // время жизни снаряда
        [SerializeField] private LayerMask hitMask;             // кого можем ударить

        
        [Header("Наведение")]
        [SerializeField] private float aimYOffset = 0.35f;
        [SerializeField] private float noTargetForward = 6f;

        
        
     //   [Header("Высота над врагом")]
      //  [SerializeField] private float  _hSpawnPos = 1.5f;           // самонаводящийся?

      public void SetTargetPoint(Vector3 target)
      {
          _targetPosition = target;
      }
      
      public int GetMannaLichCost()
      {
          return _mannaCost;
      }

        

      public override void Attack()
      {
          Debug.Log($"Кидаем фаербол1111");
          if (_arrowPrefabLich == null)
          {
              
              Debug.LogError($"arrowPrefab не установлен");
              return;
          }

          Transform fp = firePoint != null ? firePoint : transform;
          Vector2 spawnPos = new Vector2(_targetPosition.x, _targetPosition.y + heightSpawnOffset);

          Debug.Log($"Выпускаем фаербол ======");
          Debug.LogWarning($"Выпускаем фаербол ======");
          var arrow = Instantiate(_arrowPrefabLich, spawnPos, Quaternion.identity);
              Vector2 target = new Vector2(_targetPosition.x, _targetPosition.y);
              arrow.InitFire(target, 150);
      }
    }
}