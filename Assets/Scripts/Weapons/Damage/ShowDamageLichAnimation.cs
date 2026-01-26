using System.Collections;
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

        [Header("Timing")]
        [SerializeField] private float showTime = 0.5f;      // сколько держим полностью видимым
        [SerializeField] private float fadeOutTime = 0.35f;  // сколько плавно исчезаем

        private void Awake()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);
        }

        private void Start()
        {
            Play(showDamageAnimation, false, 1f);
            StartCoroutine(LifeRoutine());
        }

        private IEnumerator LifeRoutine()
        {
            // гарантируем, что стартуем видимым
            SetAlpha(1f);

            // держим на экране
            if (showTime > 0f)
                yield return new WaitForSeconds(showTime);

            // плавное исчезновение
            float t = 0f;
            float dur = Mathf.Max(0.01f, fadeOutTime);

            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                SetAlpha(Mathf.Lerp(1f, 0f, t));
                yield return null;
            }

            Destroy(gameObject);
        }

        private void SetAlpha(float a)
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null) return;

            a = Mathf.Clamp01(a);
            var c = skeletonAnimation.Skeleton.GetColor();
            c.a = a;
            skeletonAnimation.Skeleton.SetColor(c);

            // чтобы применилось сразу, не ждать следующего апдейта
            skeletonAnimation.Update(0f);
        }

        private void Play(AnimationReferenceAsset anim, bool loop, float timeScale)
        {
            if (anim == null || skeletonAnimation == null) return;
            var entry = skeletonAnimation.AnimationState.SetAnimation(0, anim, loop);
            entry.TimeScale = timeScale;
        }
    }
}
