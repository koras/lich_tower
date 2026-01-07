using System;
using UnityEngine;

namespace AudioSystem
{
 
    [CreateAssetMenu(menuName = "Audio/Music Library", fileName = "MusicLibrarySO")]
    public class MusicLibrarySO : ScriptableObject
    {
        [Serializable]
        public struct Track
        {
            public MusicId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;  // относительная громкость трека
            public bool loop;
        }

        public Track[] tracks;

        public bool TryGet(MusicId id, out Track track)
        {
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i].id == id)
                {
                    track = tracks[i];
                    return true;
                }
            }

            track = default;
            return false;
        }
    }
}