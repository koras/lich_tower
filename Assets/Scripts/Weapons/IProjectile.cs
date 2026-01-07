using UnityEngine;

namespace Weapons
{
    public interface IProjectile
    {
        // снаряд
        int Damage { get; }
        // атакуем
      //  void Attack(); // действие
        
        // вызываем атаку из анимации
      //  void InvokeAttack();
            // устанавливаем цель
        public void LaunchTowards(Vector2 target);
    }
}