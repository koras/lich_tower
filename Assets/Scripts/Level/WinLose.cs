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

        [Header("Animations")] [SerializeField]
        private AnimationReferenceAsset _idle;

        [SerializeField] private bool _rePlay;

        [Header("Speed")] [SerializeField] private float _idleSpeed = 1f;

        [Header("toScene")] [SerializeField] private string _targetScene;
        [SerializeField] private bool _invokeToScene;


        [Header("Transition Settings")] [SerializeField]
        private float _delayBeforeTransition = 3f; // Задержка перед переходом

        public void Load()
        {
            AudioSystem.MusicService.I?.Stop();
            Debug.Log($"[SceneTransition] nextSceneName: {_targetScene}");
            // SceneLoader.TargetScene = _targetScene;
            SceneManager.LoadScene(_targetScene, LoadSceneMode.Single);
        }

        private void Start()
        {
            if (_invokeToScene)
            {
                Play(_idle, _rePlay, _idleSpeed);
                StartCoroutine(TransitionAfterDelay());
            }
        }

        /// <summary>
        /// Переход на сцену через указанную задержку
        /// </summary>
        private IEnumerator TransitionAfterDelay()
        {
            yield return new WaitForSeconds(_delayBeforeTransition);

            // Проверяем, установлена ли целевая сцена
            if (!string.IsNullOrEmpty(_targetScene))
            {
                Load();
            }
            else
            {
                Debug.LogWarning($"[WinLose] TargetScene не установлена!");
            }
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

            var entry = skeletonAnimation.AnimationState.SetAnimation(0, anim, loop);
            entry.TimeScale = timeScale;
        }
    }
}