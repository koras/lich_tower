using UnityEngine;
using Heroes;
using Config; // наверху

namespace Level
{
    public class SpawnInHero : MonoBehaviour
    {
        [Header("Герои")] 
        
        [SerializeField] public HeroesBase _GobArcherPrefab;
        [SerializeField] public HeroesBase _OrcWarPrefab;
        [SerializeField] public HeroesBase _SkeletonPrefab;
        [SerializeField] public HeroesBase _SkeletonArcherPrefab;
 
        

        [Header("Зона спауна")]
        [Tooltip(
            "Если есть BoxCollider2D/BoxCollider — возьмем его. Иначе — SpriteRenderer/Renderer bounds. Если ничего — используем transform как точку.")]
        [SerializeField]
        private bool use2D = true;

        [SerializeField] private float topYOffset = 0.05f; // поднимем над верхом платформы (в юнитах)

        [SerializeField]
        private Vector2 extraPadding = new Vector2(0.05f, 0f); // чуть «внутрь» края, чтобы не рождать на краю

        [Header("Проверка места (опционально)")] [SerializeField]
        private float minSeparationRadius = 0.3f; // расстояние до других коллайдеров при спауне

        [SerializeField] private LayerMask separationMask = ~0; // где проверяем «пересечения»
        [SerializeField] private int maxTries = 15; // сколько попыток найти свободную точку

        // Start is called once before the first execution of Update after the MonoBehaviour is created

        
        // Update is called once per frame
    

        public int getCost(State who = State.GobArcher)
        {
            int cost = HeroCostStorage.GetCost(who);

            // на всякий случай запасной вариант: если в конфиге 0, можно взять из префаба
            if (cost <= 0)
            {
                var prefab = GetPrefab(who);
                if (prefab != null)
                {
                    cost = prefab.GetGold();
                    Debug.LogWarning($"[{name}] Цена {who} взята из префаба ({cost}), так как в конфиге 0");
                }
            }

            return cost;
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
            var instance = Instantiate(prefab, spawnPos, Quaternion.identity);
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
                    if (use2D)
                    {
                        if (!Physics2D.OverlapCircle(p, minSeparationRadius, separationMask))
                        {
                            pos = p;
                            return true;
                        }
                    }
                    else
                    {
                        if (!Physics.CheckSphere(p, minSeparationRadius, separationMask))
                        {
                            pos = p;
                            return true;
                        }
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
            if (use2D)
            {
                var box2D = GetComponent<BoxCollider2D>();
                if (box2D)
                    return new Vector3(box2D.bounds.center.x, box2D.bounds.max.y + topYOffset, transform.position.z);

                var sr = GetComponent<SpriteRenderer>();
                if (sr)
                    return new Vector3(sr.bounds.center.x, sr.bounds.max.y + topYOffset, transform.position.z);

                return transform.position + Vector3.up * topYOffset;
            }
            else
            {
                var box = GetComponent<BoxCollider>();
                if (box)
                    return new Vector3(box.bounds.center.x, box.bounds.max.y + topYOffset, box.bounds.center.z);

                var rend = GetComponent<Renderer>();
                if (rend)
                    return new Vector3(rend.bounds.center.x, rend.bounds.max.y + topYOffset, rend.bounds.center.z);

                return transform.position + Vector3.up * topYOffset;
            }
        }

        private Vector3 GetRandomPointOnTop()
        {
            // Пытаемся взять габариты платформы из коллайдера, иначе из рендера
            if (use2D)
            {
                var box2D = GetComponent<BoxCollider2D>();
                if (box2D)
                {
                    var b = box2D.bounds;
                    float x = Random.Range(b.min.x + extraPadding.x, b.max.x - extraPadding.x);
                    float y = b.max.y + topYOffset;
                    return new Vector3(x, y, transform.position.z);
                }

                var sr = GetComponent<SpriteRenderer>();
                if (sr)
                {
                    var b = sr.bounds;
                    float x = Random.Range(b.min.x + extraPadding.x, b.max.x - extraPadding.x);
                    float y = b.max.y + topYOffset;
                    return new Vector3(x, y, transform.position.z);
                }
            }
            else
            {
                var box = GetComponent<BoxCollider>();
                if (box)
                {
                    var b = box.bounds;
                    float x = Random.Range(b.min.x + extraPadding.x, b.max.x - extraPadding.x);
                    float z = Random.Range(b.min.z + extraPadding.x, b.max.z - extraPadding.x);
                    float y = b.max.y + topYOffset;
                    return new Vector3(x, y, z);
                }

                var rend = GetComponent<Renderer>();
                if (rend)
                {
                    var b = rend.bounds;
                    float x = Random.Range(b.min.x + extraPadding.x, b.max.x - extraPadding.x);
                    float z = Random.Range(b.min.z + extraPadding.x, b.max.z - extraPadding.x);
                    float y = b.max.y + topYOffset;
                    return new Vector3(x, y, z);
                }
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