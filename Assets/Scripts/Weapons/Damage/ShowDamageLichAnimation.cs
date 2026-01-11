using UnityEngine;

using Spine.Unity;

namespace Damage
{
    public class ShowDamageLichAnimation : MonoBehaviour
    {
        [Header("Spine")] 
        
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        [Header("Animations")] 
        
        [SerializeField] private AnimationReferenceAsset showDamageAnimation;
        
        private void Awake()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);
        }
        
        private void Start()
        {
            Play(showDamageAnimation, false,1);
            Destroy(gameObject, 1.5f);
        }

        /// <summary>
        /// Универсальный запуск анимации Spine.
        /// </summary>
        private void Play(AnimationReferenceAsset anim, bool loop, float timeScale)
        {
            if (anim == null || skeletonAnimation == null) return;

            var entry = skeletonAnimation.AnimationState.SetAnimation(0, anim, loop);
            entry.TimeScale = timeScale;
        }

 
        //
        // /// <summary>
        // /// Поставить прозрачность (для fade-out).
        // /// Возвращает false, если SkeletonAnimation не задан.
        // /// </summary>
        // public bool TrySetAlpha(float a)
        // {
        //     if (skeletonAnimation == null || skeletonAnimation.Skeleton == null)
        //         return false;
        //
        //     a = Mathf.Clamp01(a);
        //     var c = skeletonAnimation.Skeleton.GetColor();
        //     c.a = a;
        //     skeletonAnimation.Skeleton.SetColor(c);
        //     skeletonAnimation.Update(0f);
        //     return true;
        // }
    }
}