using UnityEngine;
using Weapons;
using System.Collections.Generic;

using AudioSystem; 

namespace Weapons.Projectile
{
    public class LichBombHoleProjectile2D : MonoBehaviour
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
        private void PlaySound(SoundId id)
        {
            
            if (!playAnimalSounds) return;
            if (AudioService.I == null) return;
            AudioService.I.Play(id, transform.position + soundOffset);
        }
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _sprite = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            Debug.LogError($"ExplosionBoomLichExplosionBoomLichExplosionBoomLichExplosionBoomLich");
            PlaySound(SoundId.ExplosionBoomLich);
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