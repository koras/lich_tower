using UnityEngine;
using Unity.Cinemachine;

namespace Weapons
{
    public class BombImpulse : MonoBehaviour
    {
        [SerializeField] private CinemachineImpulseSource impulse;

        private void Awake()
        {
            if (impulse == null)
                impulse = GetComponent<CinemachineImpulseSource>();
        }

        public void Shake(float force = 1f)
        {
            if (impulse == null)
                impulse = GetComponent<CinemachineImpulseSource>();

            if (impulse == null)
            {
                Debug.LogWarning("[BombImpulse] Нет CinemachineImpulseSource на объекте");
                return;
            }

            impulse.GenerateImpulseWithForce(force);
        }
    }
}