using System;
using UnityEngine;

namespace Weapons
{
    public interface IWeapon
    {
       // string Id { get; } // "sword", "bow" и т.п.
        int Damage { get; }
        float Cooldown { get; }

        string WeaponName  { get; }
        void SpawnBow();
        
        void Attack(); // действие

        void InvokeAttack();
        
     //   event Action OnAttack; // события по желанию

        void SetEnemyTarget(Transform currentTarget);


    }
}