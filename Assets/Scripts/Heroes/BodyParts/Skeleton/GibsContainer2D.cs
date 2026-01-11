using System.Collections;
using UnityEngine;

namespace Heroes.BodyParts.Skeleton
{
    public class GibsContainer2D : MonoBehaviour
    {
        [Header("Lifetime")]
        [SerializeField] private float autoDespawnSeconds = 5f;

        [Header("Scatter")]
        [SerializeField] private float forceMin = 2f;
        [SerializeField] private float forceMax = 6f;
        [SerializeField] private float torqueMin = -200f;
        [SerializeField] private float torqueMax = 200f;

        [Header("Gold")]
        [SerializeField] private int goldAmount = 10;

        private bool collected;
        private Coroutine despawnRoutine;

        private void Start()
        {
            // На всякий случай: если забыли вызвать Scatter извне
            Scatter();

            despawnRoutine = StartCoroutine(AutoDespawn());
        }

        public void Scatter(Vector2 inheritVelocity = default)
        {
            var rbs = GetComponentsInChildren<Rigidbody2D>();

            foreach (var rb in rbs)
            {
                rb.linearVelocity = inheritVelocity;

                var dir = Random.insideUnitCircle.normalized;
                var force = Random.Range(forceMin, forceMax);

                rb.AddForce(dir * force, ForceMode2D.Impulse);
                rb.AddTorque(Random.Range(torqueMin, torqueMax));
            }
        }

        public void CollectAll()
        {
            if (collected) return;
            collected = true;

            if (despawnRoutine != null)
                StopCoroutine(despawnRoutine);

            // Тут начисляешь золото в свою систему
            // Например: Wallet.AddGold(goldAmount);
            Debug.Log($"Collected gold: {goldAmount}");

            Destroy(gameObject);
        }

        private IEnumerator AutoDespawn()
        {
            yield return new WaitForSeconds(autoDespawnSeconds);
            Destroy(gameObject);
        }
    }
}