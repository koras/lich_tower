using System.Collections;
using UnityEngine;

namespace Heroes.BodyParts.Skeleton
{
    public class GibsContainer2D : MonoBehaviour
    {
        // ------------------------------
// ВРЕМЯ ЖИЗНИ / УДАЛЕНИЕ ОБЪЕКТА
// ------------------------------
        [Header("⏱️ Время жизни")] [SerializeField]
        private float autoDespawnSeconds = 5f; // Через сколько секунд удалить контейнер с кусками

// ------------------------------
// ПЛАВНОЕ ИСЧЕЗНОВЕНИЕ (FADE OUT)
// ------------------------------
        [Header("🌫️ Плавное исчезновение")] [SerializeField]
        private float fadeDuration = 0.5f; // Сколько секунд уходит на прозрачность перед удалением

// ------------------------------
// ПОЛ / ОГРАНИЧЕНИЕ ПО Y (если используешь clamp или виртуальный пол)
// ------------------------------
        [Header("🧱 Пол / ограничение по Y")] [SerializeField]
        private float minY = 1f; // Служебный параметр (лучше использовать floorY для виртуального пола)

// ------------------------------
// НАСТРОЙКИ РАЗЛЁТА (ГОРИЗОНТАЛЬ)
// ------------------------------
        [Header("💥 Разлёт: горизонталь")] [SerializeField]
        private float pushMultiplier = 0.5f; // Общий множитель силы разлёта по X (чем больше, тем дальше разлетятся)

        [SerializeField]
        private float maxHorizontalSpeed = 4.5f; // Ограничение максимальной скорости по X (чтобы не улетали в космос)

// ------------------------------
// "УДАР" МАСШТАБОМ (визуальный punch)
// ------------------------------
        [Header("📏 Визуальный 'пинок' масштабом")] [SerializeField]
        private float scaleMultiplier = 1.2f; // Во сколько раз увеличиваем куски на пике

        [SerializeField] private float scaleUpTime = 0.06f; // Время увеличения
        [SerializeField] private float scaleDownTime = 0.12f; // Время возврата в нормальный размер

// ------------------------------
// СЛУЧАЙНЫЙ РАЗБРОС СКОРОСТЕЙ (X и стартовый прыжок по Y)
// ------------------------------
        [Header("🎲 Разлёт: диапазоны скоростей")] [SerializeField]
        private float sidewaysSpeedMin = 2.0f; // Минимальная скорость по X

        [SerializeField] private float sidewaysSpeedMax = 5.0f; // Максимальная скорость по X

        [SerializeField] private float jumpSpeedMin = 4.5f; // Минимальный стартовый прыжок по Y (виртуальная парабола)
        [SerializeField] private float jumpSpeedMax = 8.0f; // Максимальный стартовый прыжок по Y

// ------------------------------
// ВЕЕР РАЗЛЁТА (направление + рандомный угол)
// ------------------------------
        [Header("🧭 Веер разлёта")]
        [Tooltip("Ширина веера (в градусах) вокруг направления pushDir. Больше угол = сильнее разброс по сторонам.")]
        [SerializeField]
        private float fanHalfAngle = 35f; // ± угол отклонения вокруг pushDir

// ------------------------------
// ВРАЩЕНИЕ КУСКОВ (рандомная угловая скорость)
// ------------------------------
        [Header("🌀 Вращение кусков")] [SerializeField]
        private float angularMin = -80f; // Минимальная угловая скорость (град/сек)

        [SerializeField] private float angularMax = 80f; // Максимальная угловая скорость (град/сек)

// ------------------------------
// УПРАВЛЕНИЕ ФИЗИКОЙ (если будешь отключать/успокаивать)
// ------------------------------
        [Header("🛠️ Управление физикой")] [SerializeField]
        private float physicsDisableDelay = 0.7f; // Через сколько секунд можно начать "успокаивать" физику

// ------------------------------
// ДЕМПФИРОВАНИЕ (сопротивление движению / вращению)
// ------------------------------
        [Header("🧲 Демпфирование (сопротивление)")] [SerializeField]
        private float linearDrag = 1f; // Сопротивление движению (меньше = дольше летят по X)

        [SerializeField]
        private float angularDrag = 6f; // Сопротивление вращению (больше = быстрее остановится вращение)

// ------------------------------
// ГРАВИТАЦИЯ UNITY (если вдруг включишь обычную физику)
// ------------------------------
        [Header("🌍 Гравитация Unity (если не используешь виртуальный пол)")] [SerializeField]
        private float gravityScale = 1.2f; // Масштаб гравитации Rigidbody2D (в твоём режиме обычно 0)

// ------------------------------
// НАГРАДА / ЛУТ (если собираешь золото)
// ------------------------------
        [Header("💰 Награда")] [SerializeField]
        private int goldAmount = 10; // Сколько золота дать при сборе

// ------------------------------
// СЛУЖЕБНЫЕ ФЛАГИ / СОСТОЯНИЕ
// ------------------------------
        [Header("📌 Состояние")] [SerializeField]
        private bool _scattered; // Чтобы Scatter не вызывался повторно

        private bool collected; // Уже собрали (чтобы не собирать дважды)
        private Coroutine despawnRoutine; // Рутины на авто-удаление

// ------------------------------
// КЕШ КОМПОНЕНТОВ
// ------------------------------
        [Header("🔎 Кеш компонентов")] private SpriteRenderer[] spriteRenderers; // Все рендереры внутри (для fade)
        private Rigidbody2D[] rbs; // Все Rigidbody2D внутри (для разлёта)

// ------------------------------
// ВИРТУАЛЬНЫЙ ПОЛ (когда в мире нет гравитации)
// ------------------------------
        [Header("🧱 Виртуальный пол (если в мире нет гравитации)")] [SerializeField]
        private bool useVirtualFloor = true; // Включить ручную параболу + пол

        [Tooltip("Насколько быстро куски падают вниз. Больше значен ие = быстрее прибивает к полу.")] [SerializeField]
        private float fakeGravity = 18f;

        [Tooltip("Отскок от пола (0..1). 0 = без отскока, 0.2..0.3 = лёгкий отскок.")] [SerializeField]
        private float bounce = 0.25f;

        [Tooltip("Затухание отскоков. 1 = не затухает, 0.9 = постепенно умирает.")] [SerializeField]
        private float yDamping = 0.9f;

        [Tooltip("Если скорость по Y меньше этого порога, считаем что кусок 'улёгся' и больше не подпрыгивает.")]
        [SerializeField]
        private float stopVyThreshold = 0.15f;

// ------------------------------
// ТРЕНИЕ ПО X НА ПОЛУ (когда уже лежит)
// ------------------------------
        [Header("🧼 Трение по X на полу")] [SerializeField]
        private float groundFriction = 0.2f; // Чем больше, тем быстрее остановятся по X на полу

// ------------------------------
// ОТЛАДКА (логи, интервал печати)
// ------------------------------
        [Header("🐛 Отладка")] [SerializeField]
        private bool debugGibs = true; // Включить подробные логи


        [SerializeField] private float debugPrintInterval = 0.2f;
        // Как часто печатать логи (в секундах), чтобы не убивать консоль и FPS


        private float _dbgT;
        private int _dbgFrames;

        private float[] _vy;
        private float[] _y;

        private float _floorY;
        private float floorOffset = 0f; // насколько ниже спавна пол

        private void Awake()
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            rbs = GetComponentsInChildren<Rigidbody2D>(true);

            _vy = new float[rbs.Length];
            _y = new float[rbs.Length];
        }

        private void Start()
        {
            // По умолчанию "в стороны", если не передали направление удара
            //    Scatter(default, Vector2.left);

            StartCoroutine(ScalePunchSmooth());
            //StartCoroutine(DisablePhysicsAfterDelay());
            despawnRoutine = StartCoroutine(AutoDespawnWithFade());
        }

        private void LateUpdate()
        {
            if (useVirtualFloor)
                SimulateVirtualFloor();
            //  ClampPiecesY();
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
            if (_scattered) return;
            _scattered = true;

            float dirX = (pushDir.sqrMagnitude > 0.0001f) ? Mathf.Sign(pushDir.x) : -1f;
            if (dirX == 0f) dirX = -1f;

            if (debugGibs)
            {
                Debug.Log(
                    $"[GIBS Scatter] root={name} pushDir={pushDir} dirX={dirX} inheritVel={inheritVelocity} minY={minY} useVirtualFloor={useVirtualFloor}");
            }

            _floorY = transform.position.y - floorOffset;
// или если хочешь ровно под самым нижним куском:
            //    _floorY = float.MaxValue;
            //    for (int i=0;i<rbs.Length;i++) if (rbs[i]!=null) _floorY = Mathf.Min(_floorY, rbs[i].position.y);
            //     _floorY -= floorOffset;

            for (int i = 0; i < rbs.Length; i++)
            {
                var rb = rbs[i];
                if (rb == null) continue;

                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.simulated = true;

                rb.linearDamping = linearDrag;
                rb.angularDamping = angularDrag;
                rb.gravityScale = 0f;

                rb.constraints = RigidbodyConstraints2D.FreezePositionY;

                float speedX = Random.Range(sidewaysSpeedMin, sidewaysSpeedMax) * pushMultiplier;
                float vx = Mathf.Clamp(dirX * speedX + inheritVelocity.x * 0.2f, -maxHorizontalSpeed,
                    maxHorizontalSpeed);

                rb.linearVelocity = new Vector2(vx, 0f);
                rb.angularVelocity = Random.Range(angularMin, angularMax);

                _y[i] = rb.position.y;
                _vy[i] = Random.Range(jumpSpeedMin, jumpSpeedMax);

                if (debugGibs)
                {
                    Debug.Log(
                        $"[GIBS Scatter] root={name} spawnY={transform.position.y:F2} floorY={_floorY:F2} minY={minY:F2} pushDir={pushDir} dirX={dirX}");
                }
            }
        }


        private void SimulateVirtualFloor()
        {
            float dt = Time.deltaTime;

            if (debugGibs)
            {
                _dbgT += dt;
                if (_dbgT >= debugPrintInterval)
                {
                    _dbgT = 0f;
                    _dbgFrames++;
                }
            }

            for (int i = 0; i < rbs.Length; i++)
            {
                var rb = rbs[i];
                if (rb == null || !rb.simulated) continue;


                // интеграция
                _vy[i] -= fakeGravity * dt;
                _y[i] += _vy[i] * dt;

                // пол
                float floor = _floorY;
                if (_y[i] <= floor)
                {
                    _y[i] = floor;

                    if (Mathf.Abs(_vy[i]) < stopVyThreshold)
                    {
                        _vy[i] = 0f;
                    }
                    else
                    {
                        _vy[i] = -_vy[i] * bounce;
                        _vy[i] *= yDamping;
                    }
                }

                // применяем Y к Rigidbody позиционно, X оставляем физике
                var p = rb.position;
                p.y = _y[i];
                rb.position = p;

                //    float floor = _floorY;

                bool onFloor = _y[i] <= floor + 0.001f;

                if (onFloor)
                {
                    var v = rb.linearVelocity;
                    v.x = Mathf.Lerp(v.x, 0f, groundFriction * dt);
                    rb.linearVelocity = v;
                }

                Debug.Log(
                    $"[GIBS step #{i}] y={_y[i]:F2} vy={_vy[i]:F2} rbX={rb.position.x:F2} rbVx={rb.linearVelocity.x:F2} floor={floor:F2} onFloor={onFloor}");
                if (debugGibs && _dbgFrames > 0) // печать только когда "тик" вывода случился
                {
                    // печатаем 1 раз за интервал, но для всех костей
                    // Чтобы не спамить слишком жирно, можно логать только i==0 и i==last
                    Debug.Log(
                        $"[GIBS step #{i}] y={_y[i]:F2} vy={_vy[i]:F2} rbX={rb.position.x:F2} rbVx={rb.linearVelocity.x:F2} onFloor={(_y[i] <= minY + 0.001f)}");
                }
            }

            if (debugGibs && _dbgFrames > 0)
                _dbgFrames = 0;
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
                // мягко успокаиваем, но не превращаем в стоп-кадр
                rb.linearVelocity = new Vector2(0f, 0f);
                rb.angularVelocity = 0f;

                rb.linearDamping = 10f;
                rb.angularDamping = 10f;

                // НЕ НАДО Kinematic, иначе это выглядит как “заморозка”
                // rb.bodyType = RigidbodyType2D.Kinematic;
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