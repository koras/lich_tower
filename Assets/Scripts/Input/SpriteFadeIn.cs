using UnityEngine;
using System.Collections;

namespace Input
{
    public class SpriteFadeIn : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private bool fadeOnStart = true;
        [SerializeField] private bool disableOnComplete = false;

        private SpriteRenderer spriteRenderer;
        private Color originalColor;

        void Start()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("SpriteFadeIn: SpriteRenderer не найден!");
                return;
            }

            originalColor = spriteRenderer.color;
        
            if (fadeOnStart)
            {
                StartFadeIn();
            }
        }

        public void StartFadeIn()
        {
            StartCoroutine(FadeInCoroutine());
        }

        IEnumerator FadeInCoroutine()
        {
            // Устанавливаем начальную прозрачность
            Color startColor = originalColor;
            startColor.a = 0f;
            spriteRenderer.color = startColor;

            // Включаем рендерер если он был выключен
            spriteRenderer.enabled = true;

            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            
                Color newColor = originalColor;
                newColor.a = alpha;
                spriteRenderer.color = newColor;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Устанавливаем финальный цвет
       //     spriteRenderer.color = originalColor;

            if (disableOnComplete)
            {
                enabled = false;
            }
        }

        // Для вызова из других скриптов
        public void FadeIn(float customDuration = 0.5f)
        {
            fadeDuration = customDuration;
            StartFadeIn();
        }
    }
}