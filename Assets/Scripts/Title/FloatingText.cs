using UnityEngine;

using TMPro;

namespace Title
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private float moveUp = 1f;
        [SerializeField] private float duration = 0.8f;
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private TextMeshProUGUI text;
        private CanvasGroup group;
        private Vector3 startPos;
        private float timer;

        void Awake()
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
            group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            
            startPos = transform.position;
            timer = 0f;
        }

        public void Setup(string message, Color color)
        {
            if (text != null)
            {
                text.text = message;
                text.color = color;
            }
        }

        void Update()
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // движение вверх с кривой
            transform.position = startPos + new Vector3(0, moveCurve.Evaluate(t) * moveUp, 0);

            // прозрачность с кривой
            group.alpha = alphaCurve.Evaluate(t);

            if (timer >= duration)
                Destroy(gameObject);
        }
    }
}