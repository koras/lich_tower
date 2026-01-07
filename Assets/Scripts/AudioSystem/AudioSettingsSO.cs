using UnityEngine;

namespace AudioSystem
{
    [CreateAssetMenu(menuName = "Audio/Audio Settings", fileName = "AudioSettings")]
    public class AudioSettingsSO : ScriptableObject
    {
        [Range(0f, 1f)] public float master = 1f;
        [Range(0f, 1f)] public float sfx = 1f;
        [Range(0f, 1f)] public float ui = 1f;
        [Range(0f, 1f)] public float music = 0.5f;
        [Range(0f, 1f)] public float footsteps = 1f;
        [Range(0f, 1f)] public float animals = 1f;

        [Min(0f)] public float musicFadeTime = 0.6f;
        
        public float GetCategoryMul(SoundCategory c) => c switch
        {
            SoundCategory.Sfx => sfx,
            SoundCategory.Ui => ui,
            SoundCategory.Music => music,
            SoundCategory.Footsteps => footsteps,
            SoundCategory.Animals => animals,
            _ => 1f
        };
    }
}