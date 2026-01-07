using Spine.Unity;
using UnityEngine;

namespace Animals
{
    /// <summary>
    /// Визуал для птицы (Spine).
    /// Слушает BirdAI и включает нужные анимации: Fly / Death.
    /// Отдельно умеет:
    /// - SetFacing (флип влево/вправо)
    /// - TrySetAlpha (прозрачность для fade-out)
    /// </summary>
    public class BirdVisualAnimal : MonoBehaviour
    {
        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        [Header("Animations")]
        [SerializeField] private AnimationReferenceAsset fly;
        [SerializeField] private AnimationReferenceAsset death;

        [Header("Speed")]
        [SerializeField] private float flySpeed = 1f;
        [SerializeField] private float deathSpeed = 1f;

        [Header("Flip Root (куда применять разворот)")]
        [Tooltip("Сюда назначь объект, который нужно флипать (обычно root со SkeletonAnimation).")]
        [SerializeField] private Transform visualRoot;

        private bool _facingLeft;
        private BirdAI _ai;

        private void Awake()
        {
            _ai = GetComponentInParent<BirdAI>();

            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);

            if (visualRoot == null)
                visualRoot = skeletonAnimation != null ? skeletonAnimation.transform : transform;
        }

        private void OnEnable()
        {
            if (_ai != null)
                _ai.OnStateChanged += OnStateChanged;

            // если BirdAI уже в каком-то состоянии (например, объект включили позже)
            if (_ai != null)
                OnStateChanged(_ai.CurrentState);
        }

        private void OnDisable()
        {
            if (_ai != null)
                _ai.OnStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(BirdAI.State state)
        {
            switch (state)
            {
                case BirdAI.State.Fly:
                    Play(fly, true, flySpeed);
                    break;

                case BirdAI.State.Death:
                   Play(death, false, deathSpeed);
                    break;
            }
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
        /// Флип визуала. Вызывай из BirdAI по направлению движения.
        /// </summary>
        public void SetFacing(bool left)
        {
            if (visualRoot == null) return;
            if (_facingLeft == left) return;

            _facingLeft = left;
            visualRoot.localRotation = left
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;
        }

        /// <summary>
        /// Поставить прозрачность (для fade-out).
        /// Возвращает false, если SkeletonAnimation не задан.
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
