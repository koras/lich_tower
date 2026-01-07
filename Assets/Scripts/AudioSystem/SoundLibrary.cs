using System;
using UnityEngine;

namespace AudioSystem
{
    [Serializable]
    public class SoundEntry
    {
        public SoundId id;
        public string nameSound = "Описание";
        public SoundCategory category = SoundCategory.Sfx;

        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 0.3f)] public float volumeRandom = 0.05f;

        [Range(0.5f, 2f)] public float pitch = 1f;
        [Range(0f, 0.3f)] public float pitchRandom = 0.05f;

        [Range(0f, 1f)] public float chance = 1f;

        [Min(0f)] public float cooldown = 0.05f; // защита от спама
        public bool spatial = true;
        [Range(0f, 1f)] public float spatialBlend = 1f;
        
        // Дополнительные полезные методы
        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0)
                return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }
        
        public float GetRandomVolume()
        {
            return Mathf.Clamp01(volume + UnityEngine.Random.Range(-volumeRandom, volumeRandom));
        }
        
        public float GetRandomPitch()
        {
            return Mathf.Clamp(pitch + UnityEngine.Random.Range(-pitchRandom, pitchRandom), 0.1f, 3f);
        }
    }

    [CreateAssetMenu(menuName = "Audio/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        public SoundEntry[] sounds;
        
        // Методы для поиска звуков
        public SoundEntry GetSoundById(SoundId id)
        {
            if (sounds == null) return null;
            
            foreach (var sound in sounds)
            {
                if (sound.id == id)
                    return sound;
            }
            return null;
        }
        
        public SoundEntry GetSoundByName(string name)
        {
            if (sounds == null) return null;
            
            foreach (var sound in sounds)
            {
                if (sound.nameSound == name)
                    return sound;
            }
            return null;
        }
        
        public SoundEntry[] GetSoundsByCategory(SoundCategory category)
        {
            if (sounds == null) return null;
            
            return Array.FindAll(sounds, sound => sound.category == category);
        }
    }
}