using UnityEngine;
using System;
using Heroes;

namespace Weapons
{
    public class WeaponBase : MonoBehaviour, IWeapon
    {
    //    [field: SerializeField] public string Id { get; private set; }
        
        [SerializeField]  protected PolygonCollider2D _polygonCollider2D;
        protected Transform _currentTarget;

        protected HeroesBase _targetHealth;
    
        
        [Header("Может атаковать")] 
        [SerializeField] protected bool canAttack = true;
        
        
        [Header("Урон")] 
        protected int Damage = 5;
        
        
        [field: SerializeField] public float Cooldown { get; private set; } = .3f;
    
        [Header("Название оружия для логов")] 
        [field: SerializeField] public string WeaponName { get; } = "";
         
       
        protected float _lastUseTime;
        
        // 🔹 ВЛАДЕЛЕЦ ОРУЖИЯ — теперь защищённая переменная
      //  protected WeaponOwner owner;
        
   //     public event System.Action OnAttack;

    
        [Header("Название оружия (для логов)")]
        [SerializeField] private string weaponName = "Weapon";
        
        /// <summary>
        /// Этот метод вызывается из Animation Event, в момент удара.
        /// Потомки переопределяют его для конкретного оружия.
        /// </summary>
        public virtual void HitAttack()
        {
            Debug.Log($"[{weaponName}] Base HitAttack() — переопредели в наследнике!");
        }
        
        public virtual void Attack()
        {
            //   Debug.Log($"base:Attack");
        }

        public void SetDamage(int value)
        {
            Damage = value;
        }

        public virtual void ClearTarget()
        {
        _currentTarget = null;

          _targetHealth = null;
        }

        /**
         * Вызов снаряда или магии который сам по себе отдельная роль
         */
        public virtual void InvokeAttack()
        {
            
            Debug.Log($"InvokeAttack");
            
        }
        
        public virtual void SpawnBow()
        {
        //    OnAttack?.Invoke();
     //       Debug.Log($"SpawnBoll base");
        }
        /// <summary>
        /// Если оружие стреляет снарядом (лук, магия и т.п.)
        /// </summary>
        public virtual void SpawnProjectile()
        {
            // реализуется в потомках
        }
        
        public virtual void SetTargetHealth(HeroesBase targetHealth)
        {
            _targetHealth = targetHealth;
        }
        public virtual void SetEnemyTarget(Transform currentTarget)
        {
       //    Debug.Log($"[{currentTarget.name}] SetEnemyTarget");
            if (Time.time - _lastUseTime < Cooldown) return;
            _lastUseTime = Time.time;
            _currentTarget = currentTarget;   // <- важно: запомнили цель
        }

    }
}