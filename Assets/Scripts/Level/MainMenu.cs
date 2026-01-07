using AudioSystem;
using UnityEngine;

namespace Level
{
    
    public class MainMenu : MonoBehaviour
    {
        [Header("Звуки")]
    
        [SerializeField] private bool playAnimalSounds = true;
        [SerializeField] private Vector3 soundOffset = Vector3.zero;

        void Start()
        {
            MusicService.I.Play(MusicId.MainMenu);
        }

    }
}