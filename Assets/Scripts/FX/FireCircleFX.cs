using UnityEngine;

namespace FX
{
    [CreateAssetMenu(fileName = "FireCircleFX", menuName = "Weapons/FireCircleFX")]
    public class FireCircleFX : ScriptableObject
    {
        [Header("Main Settings")]
        public GameObject particlePrefab;
        public float rotationSpeed = 90f;
        public float lifeTime = 2f;
        public float heightAboveTarget = 1.5f;

        [Header("Fire Circle Properties")]
        public float circleRadius = 1f;
        public int particleCount = 16;
        public Color fireColorStart = new Color(1f, 0.5f, 0f, 1f);
        public Color fireColorEnd = new Color(1f, 0f, 0f, 1f);
        public float particleSize = 0.3f;
    }
}