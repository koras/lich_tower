using UnityEngine;
using Heroes;
using Config; // наверху

namespace Level
{
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpawnPlatform : MonoBehaviour
    {
        [Header("Герои")] 
        [SerializeField] public HeroesBase _GobArcherPrefab;
        [SerializeField] public HeroesBase _OrcWarPrefab;
        [SerializeField] public HeroesBase _SkeletonPrefab;
        [SerializeField] public HeroesBase _SkeletonArcherPrefab;
 
        
        [Header("Зона спауна")]
        [SerializeField] private float topYOffset = 0.05f; // поднимем над верхом платформы (в юнитах)

        [SerializeField]
        private Vector2 extraPadding = new Vector2(0.05f, 0f); // чуть «внутрь» края, чтобы не рождать на краю

        [Header("Проверка места (опционально)")] [SerializeField]
        private float minSeparationRadius = 0.3f; // расстояние до других коллайдеров при спауне

        [SerializeField] private LayerMask separationMask = ~0; // где проверяем «пересечения»
        [SerializeField] private int maxTries = 15; // сколько попыток найти свободную точку

        // Start is called once before the first execution of Update after the MonoBehaviour is created

        
        // Update is called once per frame
    

        // В начало класса добавьте
        private void Start()
        {
            // Автоматически регистрируемся в менеджере
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.AddPlatform(this);
            }
        }
        
        

        // Update is called once per frame
        public HeroesBase InvokeSpawn(State who = State.GobArcher)
        {
            var prefab = GetPrefab(who);
            if (prefab == null)
            {
                Debug.LogError($"[{name}] Префаб для {who} не назначен.");
                return null;
            }

            if (!TryGetSpawnPoint(out var spawnPos))
            {
                Debug.LogWarning($"[{name}] Не удалось найти свободную точку для спауна, ставлю в центр платформы.");
                spawnPos = GetPlatformTopCenter();
            }

            Debug.Log($"Instantiate");
            var instance = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            return instance;
        }


        private HeroesBase GetPrefab(State who)
        {
            return who switch
            {
                State.Skeleton => _SkeletonPrefab,
                State.SkeletonArcher => _SkeletonArcherPrefab,
                State.GobArcher => _GobArcherPrefab,
                State.OrcWar => _OrcWarPrefab,
                _ => _SkeletonPrefab
            };
        }

        private bool TryGetSpawnPoint(out Vector3 pos)
        {
            // несколько попыток, чтобы не спаунить впритык к другим
            for (int i = 0; i < maxTries; i++)
            {
                var p = GetRandomPointOnTop();
                if (minSeparationRadius > 0.001f)
                {
                 
                        if (!Physics2D.OverlapCircle(p, minSeparationRadius, separationMask))
                        {
                            pos = p;
                            return true;
                        }
                 
                }
                else
                {
                    pos = p;
                    return true;
                }
            }

            pos = Vector3.zero;
            return false;
        }


        private Vector3 GetPlatformTopCenter()
        {
    
                var box2D = GetComponent<BoxCollider2D>();
                if (box2D)
                    return new Vector3(box2D.bounds.center.x, box2D.bounds.max.y + topYOffset, transform.position.z);

                return transform.position + Vector3.up * topYOffset;
       
        }

        private Vector3 GetRandomPointOnTop()
        {
                var box2D = GetComponent<BoxCollider2D>();
                if (box2D)
                {
                    var b = box2D.bounds;
                    float x = Random.Range(b.min.x + extraPadding.x, b.max.x - extraPadding.x);
                    float y = b.max.y + topYOffset;
                    return new Vector3(x, y, transform.position.z);
                }

     

            // Фоллбек: если ничего нет, спауним в позиции платформы
            return GetPlatformTopCenter();
        }


        // Возможные герои
        public enum State
        {
            Skeleton,
            SkeletonArcher,
            GobArcher,
            OrcWar,
        }
    }
}