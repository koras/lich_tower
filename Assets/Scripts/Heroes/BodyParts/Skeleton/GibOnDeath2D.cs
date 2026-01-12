using UnityEngine;

namespace Heroes.BodyParts.Skeleton
{
     
    [RequireComponent(typeof(Rigidbody2D))]
    public class GibOnDeath2D : MonoBehaviour
    {
        [SerializeField] private GameObject gibsPrefab;
        [SerializeField] private float destroyAfter = 3f;

        [Header("Force")]
        [SerializeField] private float forceMin = 2f;
        [SerializeField] private float forceMax = 6f;
        [SerializeField] private float torqueMin = -200f;
        [SerializeField] private float torqueMax = 200f;

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void SpawnGibs()
        {
            if (!gibsPrefab) return;

            var gibs = Instantiate(gibsPrefab, transform.position, transform.rotation);

            foreach (var pieceRb in gibs.GetComponentsInChildren<Rigidbody2D>())
            {
                var dir = Random.insideUnitCircle.normalized;
                var force = Random.Range(forceMin, forceMax);

                pieceRb.linearVelocity = rb.linearVelocity;
                pieceRb.AddForce(dir * force, ForceMode2D.Impulse);
                pieceRb.AddTorque(Random.Range(torqueMin, torqueMax));
            }

            Destroy(gibs, destroyAfter);
        }
    }
}
