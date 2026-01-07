using UnityEngine;

namespace FX
{
    public class BoomSheetFX : MonoBehaviour
    {
        [Header("Настройки спауна")] [SerializeField]
        private GameObject prefabToSpawn; // Префаб для спауна

        [SerializeField] private float spawnInterval = 5f; // Интервал в секундах

        [Header("Позиция спауна")] [SerializeField]
        private Transform spawnPoint; // Точка спауна

        [SerializeField] private bool useRandomOffset = false;
        [SerializeField] private Vector2 randomOffsetRange = new Vector2(-2f, 2f);

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

        private void SpawnPrefab()
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
            Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            Debug.Log($"Создан объект: {prefabToSpawn.name} в позиции: {spawnPosition}");
        }
    }
}