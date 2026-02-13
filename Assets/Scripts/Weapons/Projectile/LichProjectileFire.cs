using UnityEngine;
using Damage;
using Heroes;

namespace Weapons.Projectile
{
    public class LichProjectileFire  : MonoBehaviour
    {
        [Header("Настройки снаряда")]
        [SerializeField] private float speed = 15f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private LayerMask targetLayerMask;
        [SerializeField] private GameObject hitEffectPrefab;
        
         
        [SerializeField] private ShowDamageLichAnimation _showDamageLichAnimationPrefab;
        
        
        
        private Vector2 _direction;
        private Vector2 _startPosition;
        private int _damage;
        private int _team;
        private Transform _owner;
        private bool _isInitialized;
        private bool _hasHit;

        private void Update()
        {
            if (!_isInitialized || _hasHit) return;

            // Движение
            transform.Translate(_direction * (speed * Time.deltaTime), Space.World);
            
            // Проверка максимальной дистанции
            if (Vector2.Distance(_startPosition, transform.position) >= maxDistance)
            {
                Destroy(gameObject);
                return;
            }

            // Проверка столкновений
            CheckCollision();
        }

        public void Initialize(Vector2 direction, int damage, int team, Transform owner = null)
        {
            _direction = direction.normalized;
            _damage = damage;
            _team = team;
            _owner = owner;
            _startPosition = transform.position;
            _isInitialized = true;
            _hasHit = false;

            // Поворачиваем снаряд в сторону движения
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void CheckCollision()
        {
            // Каст коллайдера вперед
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.5f, targetLayerMask);
            
            foreach (var hit in hits)
            {
                if (_hasHit) return;
                
                HeroesBase target = hit.GetComponent<HeroesBase>();
                
                if (target != null)
                {
                    // Проверяем, что цель враг и не владелец
                    if (target.GetTeam() != _team && target != _owner?.GetComponent<HeroesBase>())
                    {
                        HitTarget(target);
                        ShowDamageLichAnimation(target);
                            //showDamageLichAnimationPrefab
                        break;
                    }
                }
            }
        }

        private void ShowDamageLichAnimation(HeroesBase target)
        {
            // Transform fp = target.transform.position;
            Vector2 spawnPos = target.transform.position;
            spawnPos.y -= 0.1f;
            Instantiate(_showDamageLichAnimationPrefab, spawnPos, Quaternion.identity);
        }


        private void HitTarget(HeroesBase target)
        {
            if (_hasHit) return;
            _hasHit = true;

            // Наносим урон
            target.TakeDamage(_damage, _owner);
            
            // Эффект попадания
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            // Уничтожаем снаряд
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}