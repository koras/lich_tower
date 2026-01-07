using UnityEngine;
using Player; // чтобы видеть GoldBank

namespace Other
{
//RequireComponent(typeof(Collider2D))]
    public class GhostSpirit : MonoBehaviour
    {
        [Header("Ссылки")]
        // [SerializeField] 
        private Animator _animator;
        [SerializeField] private GoldBank _goldBank;

        [Header("Анимации (triggers/states)")] [SerializeField]
        private string appearTrigger = "AppearGhostAnimation";

        [SerializeField] private string lookState = "LookGhostAnimation"; // зацикленный стейт
        [SerializeField] private string lookToWakeTrigger = "LookToWakeGhostAnimation";
        [SerializeField] private string wakeTrigger = "WakeGhostAnimation";
        [SerializeField] private string boomTrigger = "BoomGhostAnimation";

        [Header("Время анимаций")] [SerializeField]
        private float appearDuration = 0.8f;

        [SerializeField] private float lookMinTime = 1f;
        [SerializeField] private float lookMaxTime = 4f;
        [SerializeField] private float lookToWakeDuration = 0.5f;
        [SerializeField] private float wakeDuration = 0.7f;

        [Header("Движение духа")] [SerializeField]
        private float moveUpSpeed = 0.5f;

        [SerializeField] private float floatAmplitude = 0.2f;
        [SerializeField] private float floatFrequency = 1.5f;
        [SerializeField] private float despawnY = 10f; // когда улетит выше этого Y — исчезает

        [Header("Награда")] [SerializeField] private int goldReward = 100;

        private bool _canBeClicked = false;
        private bool _isBooming = false;
        private bool _isGone = false;
        private bool _canFloat = false; // <- ВАЖНО: парение включаем не сразу

        private float _floatTime;
        private float _baseX;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            _baseX = transform.position.x;
        }

        private void Start()
        {
            StartCoroutine(LifeCycle());
        }

        private void Update()
        {
            // парение только когда разрешено
            if (!_canFloat || _isBooming || _isGone)
                return;

            _floatTime += Time.deltaTime;

            float x = _baseX + Mathf.Sin(_floatTime * floatFrequency) * floatAmplitude;
            float y = transform.position.y + moveUpSpeed * Time.deltaTime;

            transform.position = new Vector3(x, y, transform.position.z);

            if (transform.position.y > despawnY)
            {
                _isGone = true;
                Destroy(gameObject);
            }
        }

        private System.Collections.IEnumerator LifeCycle()
        {
            // 1. Появление
            if (!string.IsNullOrEmpty(appearTrigger))
                _animator.SetTrigger(appearTrigger);

            yield return new WaitForSeconds(appearDuration);

            // 2. Смотрит по сторонам
            if (!string.IsNullOrEmpty(lookState))
                _animator.Play(lookState);

            _canBeClicked = true; // можно уже кликать в этом состоянии
            float lookTime = Random.Range(lookMinTime, lookMaxTime);
            yield return new WaitForSeconds(lookTime);

            if (_isBooming) yield break;

            // 3. Переход к пробуждению
            if (!string.IsNullOrEmpty(lookToWakeTrigger))
                _animator.SetTrigger(lookToWakeTrigger);

            yield return new WaitForSeconds(lookToWakeDuration);
            if (_isBooming) yield break;

            // >>> ПОСЛЕ LookToWakeGhostAnimation НАЧИНАЕМ ПАРИТЬ <<<
            

            // 4. Анимация пробуждения
            if (!string.IsNullOrEmpty(wakeTrigger))
                _animator.SetTrigger(wakeTrigger);

            yield return new WaitForSeconds(wakeDuration);
            if (_isBooming) yield break;
            _canFloat = true;
            // дальше он просто парит вверх, пока не улетит за карту
        }

        private void OnMouseDown()
        {
            if (!_canBeClicked || _isBooming || _isGone)
                return;

            ClickedByPlayer();
        }

        private void ClickedByPlayer()
        {
            _isBooming = true;
            _canBeClicked = false;
            _canFloat = false; // на взрыве уже не парим

            if (_goldBank != null && goldReward > 0)
            {
                _goldBank.Add(goldReward);
            }
            else
            {
                Debug.LogWarning("[GhostSpirit] Нет GoldBank или награда <= 0");
            }

            if (!string.IsNullOrEmpty(boomTrigger))
                _animator.SetTrigger(boomTrigger);

            var col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;

            StartCoroutine(DestroyAfterBoom());
        }

        private System.Collections.IEnumerator DestroyAfterBoom()
        {
            float boomDuration = 0.8f; // можно вынести в инспектор
            yield return new WaitForSeconds(boomDuration);
            Destroy(gameObject);
        }
    }
}