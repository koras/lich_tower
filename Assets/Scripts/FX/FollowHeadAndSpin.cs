using UnityEngine;


namespace FX
{
    public class FollowHeadAndSpin : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public Transform target; // герой
        public Vector3 offset = new Vector3(0f, 1.2f, 0f);
        public float spinSpeed = 60f; // градусов в секунду вокруг Z

        void LateUpdate()
        {
            if (!target) return;
            transform.position = target.position + offset;
            // лёгкое общее вращение кольца (поверх Rotation over Lifetime)
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime, Space.World);
        }
    }
}