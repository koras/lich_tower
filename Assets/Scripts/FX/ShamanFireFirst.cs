using UnityEngine;
using Spine.Unity;

namespace FX
{
    public class ShamanFireFirst : MonoBehaviour
    {
        
        [Header("Время жизни снаряда")] 
        [SerializeField] private float _lifeTimer = 0.5f;
        /**
         *Это огонь
         * 
         * Когда шаман стреляет у пуля прилетает появляется огонь
         *
         * 
         * Дальше появляется туман
         */
        [Header("Компоненты")]
   		[SerializeField] public SkeletonAnimation skeletonAnimation;

        [SerializeField] protected AnimationReferenceAsset idleAnimation;
      

      //  private Animator _animator;

      
        private float _timer;

        void Start()
        {
            

            SetAnimation(idleAnimation, false, 0.7f);
           // PlayIdle();
        }
        private void Awake()
        {
         //   _animator = GetComponent<Animator>(); 
        }
        void Update()
        {
 
        }

        // set character  animation 
        public void SetAnimation(AnimationReferenceAsset animationCurrent, bool loop, float timeScale)
        {
            skeletonAnimation.AnimationState.SetAnimation(0, animationCurrent, loop).TimeScale = timeScale;
        }

        
        private void FixedUpdate()
        {
            
             _lifeTimer -= Time.fixedDeltaTime;
            if (_lifeTimer <= 0f)
             {
         //   Debug.Log("Снаряд истёк по времени");
              Destroy(gameObject);

            }
        }
        
     //   public void PlayIdle()
     //   {
     //          Debug.Log($"PlayIdle");
     //       SetAnimation(idleAnimation, true, 0.7f);
     //   }
      //  private void SpawnPrefab()
     //   {
//
     //   }
    }
}