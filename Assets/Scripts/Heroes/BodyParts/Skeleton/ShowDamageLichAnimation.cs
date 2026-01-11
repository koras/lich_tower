using UnityEngine;
using Spine.Unity;


namespace Heroes.BodyParts.Skeleton
{
    [RequireComponent(typeof(SkeletonAnimation))]
    public class ShowDamageLichAnimation : MonoBehaviour
    {
        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        [Header("Animations")]
        [SerializeField] private AnimationReferenceAsset showDamageAnimation;

        private void Awake()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        private void Start()
        {
            Play(showDamageAnimation, true, 3f);
            Destroy(gameObject, 2f);
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

        /// <summary>
        /// Поставить прозрачность (для fade-out).
        /// </summary>
        public bool TrySetAlpha(float a)
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null)
                return false;

            a = Mathf.Clamp01(a);
            var c = skeletonAnimation.Skeleton.GetColor();
            c.a = a;
            skeletonAnimation.Skeleton.SetColor(c);
            skeletonAnimation.Update(0f);
            return true;
        }
    }
}