using UnityEngine;
using Spine.Unity;
using Level.Loading;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Level
{
    public class WinLose : MonoBehaviour
    {
        [Header("Spine")] [SerializeField] private SkeletonAnimation skeletonAnimation;
        [Header("Animations")] 
        [SerializeField] private AnimationReferenceAsset _idle;
        [SerializeField] private bool _rePlay;
        [Header("Speed")] [SerializeField] private float _idleSpeed = 1f;
        public void Load()
        {
            AudioSystem.MusicService.I?.Stop();
            Play(_idle, _rePlay, _idleSpeed);
        }
        
        /// <summary>
        /// Универсальный запуск анимации Spine.
        /// </summary>
        private void Play(AnimationReferenceAsset anim, bool loop, float timeScale)
        {
            if (anim == null || skeletonAnimation == null)
            {
                Debug.Log($"[WinLose] не найден skeletonAnimation");
            }
            var entry = skeletonAnimation.AnimationState.SetAnimation(0, anim, loop);
            entry.TimeScale = timeScale;
        }
    }
}