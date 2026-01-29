using UnityEngine;
using Spine.Unity;

namespace Level
{
    public class WinLose : MonoBehaviour
    {
        [Header("Spine")] 
        
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        [Header("Animations")] [SerializeField]
        private AnimationReferenceAsset Idle;

        [Header("Speed")] [SerializeField] private float IdleSpeed = 1f;

        private void Start()
        {
            Play(Idle, false, IdleSpeed);
        }

        /// <summary>
        /// Универсальный запуск анимации Spine.
        /// </summary>
        private void Play(AnimationReferenceAsset anim, bool loop, float timeScale)
        {
            if (anim == null || skeletonAnimation == null)
            {
                Debug.Log($"[WinLose] не найден skeletonAnimation ");
            }

            skeletonAnimation.AnimationState.SetAnimation(0, anim, loop);
        }
    }
}