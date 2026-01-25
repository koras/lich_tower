using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using Spine.Unity;

namespace Level.Loading
{
    public class SceneLoader : MonoBehaviour
    {
        public Slider progressBar;
        public string sceneToLoad;
        public static string TargetScene; // ← КУДА ИДЁМ
        [Header("Минимальное время загрузки")]
        [SerializeField] private float minLoadDuration = 3f;
        [SerializeField] private AnimationCurve progressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Spine")]
        public SkeletonAnimation loaderSpine;
        public string loopAnim = "idle";
        public string finishAnim = "idle";

        void Start()
        {
            if (loaderSpine != null)
                loaderSpine.AnimationState.SetAnimation(0, loopAnim, true);

            StartCoroutine(LoadSceneAsync());
        }

        IEnumerator LoadSceneAsync()
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(TargetScene);
            operation.allowSceneActivation = false;

            bool finishPlayed = false;
            float loadStartTime = Time.time;
            
            // Инициализируем progress с небольшим значением для визуала
            float visualProgress = 0.1f;
            progressBar.value = visualProgress;

            while (!operation.isDone)
            {
                // Реальный прогресс загрузки от Unity
                float realProgress = Mathf.Clamp01(operation.progress / 0.9f);
                
                // Время с начала загрузки
                float elapsedTime = Time.time - loadStartTime;
                float timeProgress = Mathf.Clamp01(elapsedTime / minLoadDuration);
                
                // Смешиваем реальный прогресс и временной
                // Пока реальная загрузка не завершена, используем меньший из двух
                float targetProgress = Mathf.Min(realProgress, timeProgress);
                
                // Применяем кривую для более плавного движения
                targetProgress = progressCurve.Evaluate(targetProgress);
                
                // Плавно интерполируем к целевому прогрессу
                visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, Time.deltaTime * 2f);
                progressBar.value = visualProgress;

                // Если прошло достаточно времени И загрузка фактически завершена
                if (elapsedTime >= minLoadDuration && realProgress >= 1f && !finishPlayed)
                {
                    finishPlayed = true;

                    // Гарантируем, что слайдер покажет 100%
                    progressBar.value = 1f;
                    
                    // Воспроизводим финишную анимацию
                    if (loaderSpine != null && !string.IsNullOrEmpty(finishAnim))
                    {
                        loaderSpine.AnimationState.SetAnimation(0, finishAnim, false);
                        yield return new WaitForSeconds(0.3f);
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.3f);
                    }

                    operation.allowSceneActivation = true;
                }

                yield return null;
            }
        }
    }
}