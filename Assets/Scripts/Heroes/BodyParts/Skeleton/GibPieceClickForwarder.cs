using UnityEngine;

namespace Heroes.BodyParts.Skeleton
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Collider2D))]
    public class GibPieceClickForwarder : MonoBehaviour
    {
        private GibsContainer2D container;

        private void Awake()
        {
            container = GetComponentInParent<GibsContainer2D>();
        }

        private void OnMouseDown()
        {
            // Тап по любому кусочку = собрать всё
            if (container != null)
                container.CollectAll();
        } 
    }
}