using System;
using UnityEngine;

namespace Weapons
{
    public interface IWeapon
    {
       // string Id { get; } // "sword", "bow" и т.п.
       
        float Cooldown { get; }

        string WeaponName  { get; }
        void SpawnBow();
        
        void Attack(); // действие

        void InvokeAttack();

         void SetDamage(int value);
     //   event Action OnAttack; // события по желанию

        void SetEnemyTarget(Transform currentTarget);


    }
}