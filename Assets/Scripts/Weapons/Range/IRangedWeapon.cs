using UnityEngine;

namespace Weapons.Range
{
    public interface IRangedWeapon
    {
        public void SetEnemyTarget(Transform target); // куда/в кого стрелять
    }
}