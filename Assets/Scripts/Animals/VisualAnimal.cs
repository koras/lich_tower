using Spine.Unity;
using UnityEngine;
using AudioSystem; 

namespace Animals
{
    public class VisualAnimal : MonoBehaviour
    {
        
        
        [Header("Звуки")]
        [SerializeField] private bool playAnimalSounds = true;
        [SerializeField] private Vector3 soundOffset = Vector3.zero;
        
        
        [Header("Анимации")]
        [Header("Spine")] [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private AnimationReferenceAsset idle;
        [SerializeField] private AnimationReferenceAsset idleDig; // idle2 (копает)
        [SerializeField] private AnimationReferenceAsset walk;
        [SerializeField] private AnimationReferenceAsset run;
        [SerializeField] private AnimationReferenceAsset death;

        [Header("Настройки")] [SerializeField] private float idleSpeed = 0.7f;
        [SerializeField] private float walkSpeed = 1f;
        [SerializeField] private float runSpeed = 1.2f;
        [SerializeField] private Transform _visualRoot; // сюда закидывай объект со SkeletonAnimation
        private bool _facingLeft;
        private AnimalsAI _ai;
        private Transform _root;

        private void Awake()
        {
            _ai = GetComponentInParent<AnimalsAI>();
            _root = transform;
            if (_visualRoot == null) _visualRoot = transform;
        }
        public void SetFacing(bool left)
        {
            if (_facingLeft == left) return;
            _facingLeft = left;

            _visualRoot.localRotation = left
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;
        }
        private void OnEnable()
        {
            if (_ai != null)
                _ai.OnStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (_ai != null)
                _ai.OnStateChanged -= OnStateChanged;
        }
        
        private void PlaySound(SoundId id)
        {
            if (!playAnimalSounds) return;
            if (AudioService.I == null) return;
            AudioService.I.Play(id, transform.position + soundOffset);
        }
        
        private void OnStateChanged(AnimalsAI.State state)
        {
            switch (state)
            {
                case AnimalsAI.State.Idle:
                    
                    PlaySound(SoundId.AnimalPigIdle);
                    Play(idle, true, idleSpeed);
                //    PlaySound(SoundId.Animal_Idle);
                    break;

                case AnimalsAI.State.IdleDig:
                    Play(idleDig, true, idleSpeed);
                    break;

                case AnimalsAI.State.Walk:
                    PlaySound(SoundId.AnimalPigRun);
                    Play(walk, true, walkSpeed);
                    break;

                case AnimalsAI.State.Run:
                 //   PlaySound(SoundId.AnimalPigRun);
                    Play(run, true, runSpeed);
                    break;

                case AnimalsAI.State.Death:
                    PlaySound(SoundId.AnimalTap);
                    Play(death, false, 1f);
                    break;
            }
        }

        /// <summary>
        /// Универсальный метод проигрывания анимации.
        /// </summary>
        private void Play(AnimationReferenceAsset anim, bool loop, float timeScale)
        {
            if (anim == null || skeletonAnimation == null) return;

            var entry = skeletonAnimation.AnimationState.SetAnimation(0, anim, loop);
            entry.TimeScale = timeScale;
        }
        public bool TrySetAlpha(float a)
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null) return false;

            a = Mathf.Clamp01(a);
            var c = skeletonAnimation.Skeleton.GetColor();
            c.a = a;
            skeletonAnimation.Skeleton.SetColor(c);
            skeletonAnimation.Update(0f);
            return true;
        }

    }
}