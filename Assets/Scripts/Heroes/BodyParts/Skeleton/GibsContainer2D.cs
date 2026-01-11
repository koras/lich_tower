using System.Collections;
using UnityEngine;

namespace Heroes.BodyParts.Skeleton
{
    public class GibsContainer2D : MonoBehaviour
    {
        [Header("Lifetime")]
        [SerializeField] private float autoDespawnSeconds = 5f;

        [Header("Fade")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Ground clamp")]
        [SerializeField] private float minY = 1f;
        [Header("Scatter tuning")]
        [SerializeField] private float pushMultiplier = 2.0f;   // усиление в сторону
        [SerializeField] private float maxHorizontalSpeed = 6f; // ограничитель по X
        
        
        [Header("Scale punch")]
        [SerializeField] private float scaleMultiplier = 1.3f;
        [SerializeField] private float scaleUpTime = 0.06f;
        [SerializeField] private float scaleDownTime = 0.12f;

        [Header("Scatter")]
        [SerializeField] private float sidewaysSpeedMin = 0.8f;
        [SerializeField] private float sidewaysSpeedMax = 1.8f;

        [SerializeField] private float jumpSpeedMin = 2.8f;
        [SerializeField] private float jumpSpeedMax = 3.6f;

        [Tooltip("Ширина веера в градусах вокруг направления pushDir")]
        [SerializeField] private float fanHalfAngle = 35f;

        [SerializeField] private float angularMin = -80f;
        [SerializeField] private float angularMax = 80f;

        [Header("Physics control")]
        [SerializeField] private float physicsDisableDelay = 0.7f;

        [Header("Damping")]
        [SerializeField] private float linearDrag = 3f;
        [SerializeField] private float angularDrag = 6f;

        [Header("Gravity")]
        [SerializeField] private float gravityScale = 1.2f;

        [Header("Gold")]
        [SerializeField] private int goldAmount = 10;

        private bool collected;
        private Coroutine despawnRoutine;

        private SpriteRenderer[] spriteRenderers;
        private Rigidbody2D[] rbs;

        private void Awake()
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            rbs = GetComponentsInChildren<Rigidbody2D>(true);
        }

        private void Start()
        {
            // По умолчанию "в стороны", если не передали направление удара
            Scatter(default, Vector2.left);

            StartCoroutine(ScalePunchSmooth());
            StartCoroutine(DisablePhysicsAfterDelay());
            despawnRoutine = StartCoroutine(AutoDespawnWithFade());
        }

        private void LateUpdate()
        {
            ClampPiecesY();
        }

        private void ClampPiecesY()
        {
            foreach (var rb in rbs)
            {
                var pos = rb.position;
                if (pos.y < minY)
                {
                    pos.y = minY;
                    rb.position = pos;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }
        }

        /// <summary>
        /// Разлёт с направлением. pushDir: куда "толкнуло" осколки (обычно ПРОТИВОПОЛОЖНО направлению удара).
        /// Пример: удар справа => pushDir = Vector2.left.
        /// </summary>
        public void Scatter(Vector2 inheritVelocity, Vector2 pushDir)
        {
            if (pushDir.sqrMagnitude < 0.0001f)
                pushDir = Vector2.left;

            pushDir.Normalize();

            foreach (var rb in rbs)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.simulated = true;

                rb.linearDamping = linearDrag;
                rb.angularDamping = angularDrag;
                rb.gravityScale = gravityScale;

                // ───── ВЕЕР ВОКРУГ НАПРАВЛЕНИЯ УДАРА ─────
                float deltaAngle = Random.Range(-fanHalfAngle, fanHalfAngle);
                Vector2 dir = Rotate(pushDir, deltaAngle).normalized;

                // ───── СКОРОСТИ ─────
                float sideSpeed = Random.Range(sidewaysSpeedMin, sidewaysSpeedMax);
                float jumpSpeed = Random.Range(jumpSpeedMin, jumpSpeedMax);

                // ───── УСИЛЕНИЕ В СТОРОНУ УДАРА ─────
                Vector2 velocity =
                    inheritVelocity * 0.2f +
                    dir * (sideSpeed * pushMultiplier) +   // ← ВОТ ЗДЕСЬ УСИЛЕНИЕ
                    Vector2.up * jumpSpeed;

                // ───── ОГРАНИЧЕНИЕ, ЧТОБ НЕ УЛЕТАЛИ В КОСМОС ─────
                velocity.x = Mathf.Clamp(velocity.x, -maxHorizontalSpeed, maxHorizontalSpeed);

                rb.linearVelocity = velocity;
                rb.angularVelocity = Random.Range(angularMin, angularMax);
            }
        }

        /// <summary>
        /// Удобный оверлоад: если не надо inheritVelocity.
        /// </summary>
        public void Scatter(Vector2 pushDir)
        {
            Scatter(default, pushDir);
        }

        private IEnumerator DisablePhysicsAfterDelay()
        {
            yield return new WaitForSeconds(physicsDisableDelay);

            foreach (var rb in rbs)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = true; // клики живы
            }
        }

        public void CollectAll()
        {
            if (collected) return;
            collected = true;

            if (despawnRoutine != null)
                StopCoroutine(despawnRoutine);

            // Wallet.AddGold(goldAmount);
            Debug.Log($"Collected gold: {goldAmount}");

            Destroy(gameObject);
        }

        private IEnumerator AutoDespawnWithFade()
        {
            // если fadeDuration больше жизни, подрежем
            float wait = Mathf.Max(0f, autoDespawnSeconds - fadeDuration);
            yield return new WaitForSeconds(wait);

            // fade
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(t / fadeDuration);

                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    var sr = spriteRenderers[i];
                    if (sr == null) continue;
                    var c = sr.color;
                    c.a = a;
                    sr.color = c;
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator ScalePunchSmooth()
        {
            var pieces = GetComponentsInChildren<Transform>(includeInactive: true);

            Vector3[] original = new Vector3[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
                original[i] = pieces[i].localScale;

            Vector3[] target = new Vector3[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
                target[i] = original[i] * scaleMultiplier;

            float t = 0f;
            while (t < scaleUpTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / scaleUpTime);
                k = EaseOutCubic(k);

                for (int i = 0; i < pieces.Length; i++)
                    pieces[i].localScale = Vector3.LerpUnclamped(original[i], target[i], k);

                yield return null;
            }

            t = 0f;
            while (t < scaleDownTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / scaleDownTime);
                k = EaseInCubic(k);

                for (int i = 0; i < pieces.Length; i++)
                    pieces[i].localScale = Vector3.LerpUnclamped(target[i], original[i], k);

                yield return null;
            }

            for (int i = 0; i < pieces.Length; i++)
                pieces[i].localScale = original[i];
        }

        private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
        private static float EaseInCubic(float x) => x * x * x;

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float s = Mathf.Sin(rad);
            float c = Mathf.Cos(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}