using UnityEngine;
using System;
using Heroes;

namespace Weapons
{
    public class WeaponBase : MonoBehaviour, IWeapon
    {
        //    [field: SerializeField] public string Id { get; private set; }

        [SerializeField] protected PolygonCollider2D _polygonCollider2D;
        protected Transform _currentTarget;

        [SerializeField]  protected HeroesBase _targetHealth;


        [SerializeField] public WeaponType weaponType;
        
       // public WeaponType weaponType => _weaponType;
        
        
        [Header("Может атаковать")] [SerializeField]
        protected bool canAttack = true;


        [Header("Урон")] protected int Damage = 5;


        [field: SerializeField] public float Cooldown { get; private set; } = .3f;

        [Header("Название оружия для логов")]
        [field: SerializeField]
        public string WeaponName { get; } = "";


        protected float _lastUseTime;

        // 🔹 ВЛАДЕЛЕЦ ОРУЖИЯ — теперь защищённая переменная
        //  protected WeaponOwner owner;

        //     public event System.Action OnAttack;


        [Header("Название оружия (для логов)")] [SerializeField]
        private string weaponName = "Weapon";

        /// <summary>
        /// Этот метод вызывается из Animation Event, в момент удара.
        /// Потомки переопределяют его для конкретного оружия.
        /// </summary>
        public virtual void HitAttack()
        {
            Debug.Log($"[{weaponName}] Base HitAttack() — переопредели в наследнике!");
        }


        public virtual  void LichAttackDirection(Vector2 aimDirection)
        {
            
            
        }

        public virtual void Attack()
        {
            //   Debug.Log($"base:Attack");
        }

        public void SetDamage(int value)
        {
            //j   _targetHealth.ShowDamageAnimation(Hero hero);
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
            _currentTarget = currentTarget; // <- важно: запомнили цель
        }


        // Возможные герои
        public enum WeaponType
        {
            // Ближний бой
            Melee, // Обычное оружие ближнего боя (меч, топор, дубина)
            MeleeCleave, // Оружие с областью поражения (секира, двуручный меч)
            MeleePierce, // Колющее оружие (копье, рапира)

            // Дальний бой с физическими снарядами
            RangedBow, // Лук (стрелы по прямой траектории)
            FireBow, // Оружие бьющее по направлению
            RangedCrossbow, // Арбалет (прямолинейные снаряды с высокой пробивной силой)
            RangedThrowing, // Метательное (нож, топорик, бумеранг)

            // Магическое/элементальное оружие
            MagicProjectile, // Магические снаряды (фаербол, ледяная стрела)
            MagicBeam, // Непрерывный луч (лазер, луч энергии)
            MagicArea, // Область действия (магия земли, ядовитое облако)

            // Особые типы
            Explosive, // Взрывное (бомбы, гранаты)
            Trap, // Ловушки (мины, капканы)

            // Поддержка/утилити
            Heal, // Целительное оружие
            Buff, // Усиливающее оружие (баффы союзников)
            Debuff, // Ослабляющее оружие (дебаффы врагов)

            // Уникальные механики
            Chain, // Цепное (молния, цепной кинжал)
            Bouncing, // Рикошетящее (снаряд отскакивает от врагов/стен)
            Homing, // Самонаводящееся (снаряд преследует цель)

            // Оружие по умолчанию/резервное
            Unarmed, // Без оружия (кулаки, когти)
            Special // Уникальное оружие с особой механикой
        }
    }
}