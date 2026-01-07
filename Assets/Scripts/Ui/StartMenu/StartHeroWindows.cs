using UnityEngine;
using Spine;
using Spine.Unity;


namespace StartMenu
{
    public class StartHeroWindows : MonoBehaviour
    {
        [Header("Spine Animation")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        
        [SerializeField] protected AnimationReferenceAsset idleAnimation;
        [SerializeField] private float animationSpeed = 1f;

        void Start()
        {
            // Автоматически находим компонент если не назначен в инспекторе
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            
            // Проверяем что все компоненты на месте
            if (skeletonAnimation == null)
            {
                Debug.LogError("SkeletonAnimation not found!", this);
                return;
            }

            if (idleAnimation == null)
            {
                Debug.LogError("Idle Animation not assigned!", this);
                return;
            }

            SetAnimation(idleAnimation, true, animationSpeed);
        }

        public void SetAnimation(AnimationReferenceAsset animationCurrent, bool loop, float timeScale)
        {
            if (skeletonAnimation != null && animationCurrent != null)
            {
                skeletonAnimation.AnimationState.SetAnimation(0, animationCurrent, loop).TimeScale = timeScale;
            }
        }

        // Метод для изменения скорости анимации
        public void SetAnimationSpeed(float speed)
        {
            animationSpeed = speed;
            if (skeletonAnimation != null)
            {
                skeletonAnimation.AnimationState.TimeScale = animationSpeed;
            }
        }
    }
}