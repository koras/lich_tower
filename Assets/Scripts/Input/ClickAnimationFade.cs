using UnityEngine;
using System.Collections;

namespace Input
{
    public class ClickAnimationFade : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [SerializeField] private float fadeDelay = 0f;
        [SerializeField] private float fadeDuration = 0.5f;

        private static readonly int PlayHash = Animator.StringToHash("Play");
        private Coroutine routine;

        private void Awake()
        {
            if (!animator) animator = GetComponent<Animator>();
            if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();

            // если спрайт на дочернем объекте
            //     if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        private void OnEnable()
        {
            // на случай пуллинга: при каждом включении запускаем заново
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(PlayFadeAndDisable());
        }

        private IEnumerator PlayFadeAndDisable()
        {
            // запускаем анимацию (должен быть Trigger "Play" в AnimatorController)
            if (animator) animator.SetTrigger(PlayHash);

            // если рендера нет, хотя бы выключим объект через delay
            if (!spriteRenderer)
            {
                yield return new WaitForSeconds(fadeDelay);
                gameObject.SetActive(false);
                yield break;
            }

            // гарантируем стартовую альфу = 1 (полезно после пуллинга)
            var c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;

            yield return new WaitForSeconds(fadeDelay);

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
                spriteRenderer.color = c;
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
