using UnityEngine; 
using Heroes; 
using Spine.Unity; 
using AudioSystem; 



namespace Weapons.Projectile
{
    
 

    public class LichAttack :  WeaponBase
  {
        private bool _hitAppliedThisSwing; 
        
        private void Awake()
        {
            if (_polygonCollider2D == null)
                _polygonCollider2D = GetComponent<PolygonCollider2D>();
                _polygonCollider2D.isTrigger = true;  // <— важно
            AttackColliderTurnOff();
        }
        
        /// <summary>
        /// Вызывается из Animation Event в момент удара.
        /// Тут НЕТ коллайдера, бьём только закреплённую цель.
        /// </summary>
        public override void HitAttack()
        {
            if (!canAttack)
            {
                Debug.Log($"Запрет на атаку");
                return;
            }

         //   Debug.Log("Sword.HitAttack()");  
            // цель не назначена
            if (_targetHealth == null || _currentTarget == null)
                return;

            // чтобы за один взмах не наносить урон несколько раз
            if (_hitAppliedThisSwing)
                return;

            _hitAppliedThisSwing = true;
            
            Vector2 hitDir = ((Vector2)_targetHealth.transform.position - (Vector2)transform.position).normalized;
            _targetHealth.TakeDamage(Damage,hitDir);


        }

        
        public override void Attack()
        {
            if (!canAttack)
            {
             //   Debug.Log($"Запрет на атаку");
                return;
            }
            // проверяем, можем ли вообще атаковать
            if (_currentTarget == null || _targetHealth == null)
            {
            //    Debug.Log("Sword.Attack: цели нет, атаковать некого");
                return;
            }
            if (_currentTarget == null)
            {
            //    Debug.Log("_currentTarget");
                    // return;
            }

            // кулдаун ЛОГИЧНО проверять на атаке, а не на SetEnemyTarget
            if (Time.time - _lastUseTime < Cooldown)
                return;

            _lastUseTime = Time.time;
            _hitAppliedThisSwing = false;   // новый взмах, сбрасываем флаг
            base.Attack();
            
            Vector2 hitDir = ((Vector2)_targetHealth.transform.position - (Vector2)transform.position).normalized;
            _targetHealth.TakeDamage(Damage,hitDir);
        }
     
 
        /// <summary>
        /// Закрепляем цель и кешируем её здоровье.
        /// БЕЗ кулдауна, это просто выбор цели.
        /// </summary>
        public override void SetEnemyTarget(Transform currentTarget)
        {
            _currentTarget = currentTarget;

            _targetHealth = _currentTarget
                ? _currentTarget.GetComponent<Heroes.HeroesBase>()
                  ?? _currentTarget.GetComponentInParent<Heroes.HeroesBase>()
                : null;
        }
        private void AttackColliderTurnOff() => _polygonCollider2D.enabled = false;
    }
}