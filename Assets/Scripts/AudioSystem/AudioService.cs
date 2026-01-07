using System.Collections.Generic;
using UnityEngine;

namespace AudioSystem
{
    public class AudioService : MonoBehaviour
    {
        [SerializeField] private SoundLibrary library;
        [SerializeField] private AudioSettingsSO settings;

        [Header("Pool")]
        [SerializeField] private int poolSize = 24;

        private readonly Dictionary<SoundId, SoundEntry> _map = new();
        private readonly Dictionary<SoundId, float> _cooldowns = new();

        private AudioSource[] _sources;
        private int _cursor;

        public static AudioService I { get; private set; }

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            
            if (transform.parent != null)
                transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            BuildMap();
            BuildPool();
        }

        private void BuildMap()
        {
            _map.Clear();
            if (library == null || library.sounds == null) return;

            foreach (var s in library.sounds)
            {
                if (s == null || s.id == SoundId.None) continue;
                _map[s.id] = s;
                _cooldowns[s.id] = 0f;
            }
        }

        private void BuildPool()
        {
            _sources = new AudioSource[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"AudioSource_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sources[i] = src;
            }
        }

        private void Update()
        {
            // простая система кулдаунов
            if (_cooldowns.Count == 0) return;
            var keys = ListPool<SoundId>.Get();
            keys.AddRange(_cooldowns.Keys);

            float dt = Time.deltaTime;
            for (int i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                if (_cooldowns[k] > 0f) _cooldowns[k] -= dt;
            }

            ListPool<SoundId>.Release(keys);
        }

        public void Play(SoundId id, Vector3 pos)
        {
            if (!_map.TryGetValue(id, out var s)) return;
            if (s.clips == null || s.clips.Length == 0) return;

            if (s.chance < 1f && Random.value > s.chance) return;

            if (_cooldowns.TryGetValue(id, out var cd) && cd > 0f) return;
            _cooldowns[id] = s.cooldown;

            var clip = s.clips[Random.Range(0, s.clips.Length)];
            if (clip == null) return;

            var src = NextSource();
            src.transform.position = pos;

            src.spatialBlend = s.spatial ? s.spatialBlend : 0f;

            float vol = s.volume + Random.Range(-s.volumeRandom, s.volumeRandom);
            vol = Mathf.Clamp01(vol);

            float catMul = settings != null ? settings.GetCategoryMul(s.category) : 1f;
            float master = settings != null ? settings.master : 1f;
            src.volume = vol * catMul * master;

            float pitch = s.pitch + Random.Range(-s.pitchRandom, s.pitchRandom);
            src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);

            src.PlayOneShot(clip);
        }

        private AudioSource NextSource()
        {
            _cursor++;
            if (_cursor >= _sources.Length) _cursor = 0;
            return _sources[_cursor];
        }
    }

    // микро-пул листов чтобы Update не аллоцировал
    internal static class ListPool<T>
    {
        private static readonly Stack<List<T>> _stack = new();
        public static List<T> Get() => _stack.Count > 0 ? _stack.Pop() : new List<T>(16);
        public static void Release(List<T> list) { list.Clear(); _stack.Push(list); }
    }
}
