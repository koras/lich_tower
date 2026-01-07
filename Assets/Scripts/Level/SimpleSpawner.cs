using UnityEngine;

namespace Level
{
    public class SimpleSpawner : MonoBehaviour
    {
        [Header("Настройки спауна")]
        [SerializeField] private GameObject prefabToSpawn; // Префаб для спауна
        [SerializeField] private float spawnInterval = 5f; // Интервал в секундах
        
        [Header("Позиция спауна")]
        [SerializeField] private Transform spawnPoint;     // Точка спауна
        [SerializeField] private bool useRandomOffset = false;
        [SerializeField] private Vector2 randomOffsetRange = new Vector2(-2f, 2f);

        [Header("Damage")]
        [SerializeField] private int  _damage;     // Точка спауна
     


        [Header("Позиция цели")]
        [SerializeField] private Transform _targetPoint;    
            [SerializeField] private bool _homing = false;           // самонаводящийся?
            [SerializeField] private float turnSpeed = 720f;        // град/сек при самонаводке
        private float _timer;




        void Start()
        {
            _timer = spawnInterval; // Спаун сразу при старте
        }

        void Update()
        {
            _timer -= Time.deltaTime;
            
            if (_timer <= 0f)
            {
                SpawnPrefab();
                _timer = spawnInterval; // Сброс таймера
            }
        }

        void SpawnPrefab()
        {
            if (prefabToSpawn == null)
            {
                Debug.LogWarning("Префаб для спауна не назначен!");
                return;
            }

            // Определяем позицию спауна
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
            
            // Добавляем случайное смещение если нужно
            if (useRandomOffset)
            {
                float offsetX = Random.Range(randomOffsetRange.x, randomOffsetRange.y);
                float offsetY = Random.Range(randomOffsetRange.x, randomOffsetRange.y);
                spawnPosition += new Vector3(offsetX, offsetY, 0f);
            }

            // Создаем объект
                // Создаем объект
                var spawnedObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

                // Получаем компонент Projectile2D и вызываем Init
                var projectile = spawnedObject.GetComponent<Weapons.Projectile.LichBombProjectile2D>();
                if (projectile != null)
                {
                    Debug.LogWarning($"отключил LichBombProjectile2D Projectile2D!");

                  //  projectile.Init(
                  //      target: _targetPoint,
                  //      damage: _damage//, 
                    //    homing: _homing,
                     //   turn: turnSpeed
                 //   );
                }
                else
                {
                    Debug.LogWarning($"Созданный объект..... {prefabToSpawn.name} не имеет компонента Projectile2D!");
                }


     //       Debug.Log($"Создан объект: {prefabToSpawn.name} в позиции: {spawnPosition}");
        }
    }
}