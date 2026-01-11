using UnityEngine;

namespace Heroes.BodyParts.Skeleton
{
    public class DebugGibsSpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GibsContainer2D gibsPrefab;

        [Header("Spawn")]
        [SerializeField] private Vector2 spawnPosition = new Vector2(0f, 2f);
        [SerializeField] private bool randomX = true;
        [SerializeField] private float randomXRange = 3f;

        [Header("Timing")]
        [SerializeField] private float spawnInterval = 5f;

        [Header("Debug")]
        [SerializeField] private bool enabledSpawner = true;

        private float timer;

        private void Update()
        {
            if (!enabledSpawner || gibsPrefab == null)
                return;

            timer += Time.deltaTime;

            if (timer >= spawnInterval)
            {
                timer = 0f;
                Spawn();
            }
        }

        private void Spawn()
        {
            Vector2 pos = spawnPosition;

            if (randomX)
                pos.x += Random.Range(-randomXRange, randomXRange);

            Instantiate(gibsPrefab, pos, Quaternion.identity);
        }
    }
}