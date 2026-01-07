using AudioSystem;
using UnityEngine;

namespace Level
{
    public class Level : MonoBehaviour
    {
        [Header("Звуки")]
    
        [SerializeField] private bool playAnimalSounds = true;
        [SerializeField] private Vector3 soundOffset = Vector3.zero;

        void Start()
        { 
            //   PlaySound(SoundId.Horn);
        }
        
        private void PlaySound(SoundId id)
        {
            if (!playAnimalSounds) return;
            if (AudioService.I == null) return;
          AudioService.I.Play(id, transform.position + soundOffset);
        }
    }
}