using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using Spine.Unity;

namespace Level
{
    public class SceneLoader : MonoBehaviour
    {
        public Slider progressBar;
        public string sceneToLoad;

        [Header("Spine")]
        public SkeletonAnimation loaderSpine; // объект на сцене
        public string loopAnim = "idle";      // как у тебя называется
        public string finishAnim = "finish";  // опционально

        void Start()
        {
            if (loaderSpine != null)
                loaderSpine.AnimationState.SetAnimation(0, loopAnim, true);

            StartCoroutine(LoadSceneAsync());
        }

        IEnumerator LoadSceneAsync()
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
            operation.allowSceneActivation = false;

            bool finishPlayed = false;

            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                progressBar.value = progress;

                if (progress >= 1f && !finishPlayed)
                {
                    finishPlayed = true;

                    // если есть финиш-анимация, сыграем её один раз
                    if (loaderSpine != null && !string.IsNullOrEmpty(finishAnim))
                    {
                        loaderSpine.AnimationState.SetAnimation(0, finishAnim, false);
                        yield return new WaitForSeconds(0.3f); // под длину finish
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