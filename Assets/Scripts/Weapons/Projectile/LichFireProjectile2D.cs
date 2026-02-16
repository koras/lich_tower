using UnityEngine;
using Weapons;
using System.Collections.Generic;

using AudioSystem; 

namespace Weapons.Projectile
{
    
        
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class LichFireProjectile2D : MonoBehaviour
    {
        [Header("Компоненты")]
        private Animator _animator;
        private SpriteRenderer _sprite;
        [Header("Звуки")]
        private bool playAnimalSounds = true;
        [SerializeField] private Vector3 soundOffset = Vector3.zero;

        [Header("Время жизни")]
        [SerializeField] private float lifeTime = 4f;

        [Header("Плавное исчезновение")]
        [SerializeField] private float fadeDuration = 1f;

        
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _sprite = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            //  Debug.LogError($"ExplosionBoomLichExplosionBoomLichExplosionBoomLichExplosionBoomLich");
  
            StartCoroutine(LifeRoutine());
        }

        private System.Collections.IEnumerator LifeRoutine()
        {
            // живём
            yield return new WaitForSeconds(lifeTime);

            // плавно исчезаем
            float t = 0f;
            Color c = _sprite.color;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
                _sprite.color = c;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}