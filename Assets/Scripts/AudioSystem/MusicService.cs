
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace AudioSystem
{
    public class MusicService : MonoBehaviour
    {
        public static MusicService I { get; private set; }

        [Header("Config")]
        [SerializeField] private AudioSettingsSO settings;
        [SerializeField] private MusicLibrarySO musicLibrary;

        [Header("Sources")]
        [SerializeField] private AudioSource a; // основной
        [SerializeField] private AudioSource b; // для кроссфейда

        private AudioSource _active;
        private AudioSource _inactive;

        private MusicId _current = MusicId.None;
        private Coroutine _fadeCo;

        private void Awake()
        {
            if (I != null)
            {
                Destroy(gameObject);
                return;
            }
            I = this;
            DontDestroyOnLoad(gameObject);

            if (a == null) a = gameObject.AddComponent<AudioSource>();
            if (b == null) b = gameObject.AddComponent<AudioSource>();

            SetupSource(a);
            SetupSource(b);

            _active = a;
            _inactive = b;
        }

        private void SetupSource(AudioSource s)
        {
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 0f; // 2D
        }

        public void Play(MusicId id)
        {
            if (id == _current) return;
            if (musicLibrary == null || settings == null) return;

            if (!musicLibrary.TryGet(id, out var track) || track.clip == null)
            {
                Debug.LogWarning($"[MusicService] Track not found: {id}");
                return;
            }

            _current = id;

            // готовим второй источник
            _inactive.clip = track.clip;
            _inactive.loop = track.loop;
            _inactive.volume = 0f;
            _inactive.Play();

            float targetVol = settings.master * settings.music * Mathf.Clamp01(track.volume);

            if (_fadeCo != null) StopCoroutine(_fadeCo);
            _fadeCo = StartCoroutine(CrossFade(targetVol, settings.musicFadeTime));
        }

        public void Stop()
        {
            _current = MusicId.None;
            if (_fadeCo != null) StopCoroutine(_fadeCo);
            _fadeCo = StartCoroutine(FadeOutAndStop(settings.musicFadeTime));
        }

        public void RefreshVolumes()
        {
            // вызови это, если меняешь settings.master/music в рантайме
            float baseVol = settings.master * settings.music;
            _active.volume = Mathf.Min(_active.volume, baseVol);
            _inactive.volume = Mathf.Min(_inactive.volume, baseVol);
        }

        private IEnumerator CrossFade(float targetVolume, float time)
        {
            time = Mathf.Max(0.01f, time);

            float t = 0f;
            float startA = _active.volume;
            float startB = _inactive.volume;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / time;

                _active.volume = Mathf.Lerp(startA, 0f, t);
                _inactive.volume = Mathf.Lerp(startB, targetVolume, t);

                yield return null;
            }

            _active.Stop();
            _active.clip = null;
            _active.volume = 0f;

            // свапаем
            var tmp = _active;
            _active = _inactive;
            _inactive = tmp;

            _fadeCo = null;
        }

        private IEnumerator FadeOutAndStop(float time)
        {
            time = Mathf.Max(0.01f, time);

            float t = 0f;
            float start = _active.volume;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / time;
                _active.volume = Mathf.Lerp(start, 0f, t);
                yield return null;
            }

            _active.Stop();
            _active.clip = null;
            _active.volume = 0f;

            _inactive.Stop();
            _inactive.clip = null;
            _inactive.volume = 0f;

            _fadeCo = null;
        }
    }
}
