using System;
using Spine;
using UnityEngine;
using Spine.Unity;
using System.Collections;
using AudioSystem; 

namespace Heroes
{
    // [RequireComponent(typeof(SpriteRenderer))]
    // [RequireComponent(typeof(Animator))]
    public class BaseVisualCharacter : MonoBehaviour
    {
                      
        [Header("Что за герой")]
        [SerializeField] private Hero _who;

                
        [Header("Звуки")]
         private bool playAnimalSounds = true;
        [SerializeField] private Vector3 soundOffset = Vector3.zero;

        [Header("Логирование")] 
        [SerializeField] private bool debugAI = true;
        
        [Header("Маркировка цели для отладки")]
        [SerializeField] private bool showTargetInfo = true;
        [SerializeField] private Color aimLineColor = Color.cyan;
        
        
        
        //     [SerializeField] private Animator _animator; 
        [Header("Анимация")] 
        
        
        [SerializeField] public SkeletonAnimation skeletonAnimation;

        [SerializeField] protected AnimationReferenceAsset idleAnimation;
        [SerializeField] public AnimationReferenceAsset attackAnimation;
        [SerializeField] public AnimationReferenceAsset _attackCastLichAnimation;
        [SerializeField] public AnimationReferenceAsset walkAnimation;
        [SerializeField] public AnimationReferenceAsset gethitAnimation;
        [SerializeField] public AnimationReferenceAsset deathAnimation;
        [SerializeField] public AnimationReferenceAsset appearAnimation;
        
        
        [Header("Анимация босса")] 
        [SerializeField] public AnimationReferenceAsset laughterAnimation;
        [SerializeField] public AnimationReferenceAsset angryAnimation;

        [Header("Флэш урона")] [SerializeField]
        private Color hitColor = new Color(1f, 0.25f, 0.25f, 1f);

        [SerializeField] private float hitFlashDuration = 0.12f;

        private Coroutine _resetRoutine;
        private Color _baseColor = Color.white;
        private bool _baseCaptured;

        private WarriorAI _ai; // ссылка на контроллер ИИ

      //  [Header("Объект для разворота")]
        private Transform _visualRoot; // корневой объект визуала (если есть)

        private void UpdateTargetDebug()
        {
            if (!showTargetInfo || _ai == null) return;

            // Получаем цель через рефлексию (если нет публичного метода)
            Transform target = GetCurrentTarget();
            if (target != null)
            {
                Debug.DrawLine(transform.position, target.position, aimLineColor);
                
                float distance = Vector3.Distance(transform.position, target.position);
              //  DLog($"[BaseVisualCharacter] Цель: {target.name}, Дистанция: {distance:F2}, озиция цели: {{target.position}}");
            }
        }
        private Transform GetCurrentTarget()
        {
            // Попробуем получить цель разными способами
            if (_ai == null) return null;

            // Способ 1: через рефлексию (если поле приватное)
            var field = _ai.GetType().GetField("_currentTarget", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(_ai) as Transform;
            }

            // Способ 2: через свойство (если есть)
            var property = _ai.GetType().GetProperty("CurrentTarget");
            if (property != null)
            {
                return property.GetValue(_ai) as Transform;
            }

            return null;
        }
        private void UpdateVisualRotation()
        {
            if (_ai == null) return;
            bool faceLeft = _ai.turnToFace();
            _visualRoot.localRotation = faceLeft ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        }
 
        
        
        
        public void FlashHit(float? duration = null, Color? color = null)
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null) return;

            float d = duration ?? hitFlashDuration;
            Color c = color ?? hitColor;

            // если прошлый флэш еще жив — гасим его и СНАЧАЛА возвращаем базовый
            if (_resetRoutine != null)
            {
                StopCoroutine(_resetRoutine);
                _resetRoutine = null;
            }

            skeletonAnimation.Skeleton.SetColor(_baseColor);

            // красим в хит-цвет
            skeletonAnimation.Skeleton.SetColor(c);
            skeletonAnimation.Update(0);

            // запускаем новый откат к базовому
            _resetRoutine = StartCoroutine(ResetAfter(d));
        }

        private IEnumerator ResetAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (skeletonAnimation != null && skeletonAnimation.Skeleton != null)
            {
                skeletonAnimation.Skeleton.SetColor(_baseColor);
                skeletonAnimation.Update(0);
            }

            _resetRoutine = null;
        }


        private IEnumerator FlashRoutine(float duration, Color color)
        {
            var skeleton = skeletonAnimation.Skeleton;
            var originalColor = skeleton.GetColor(); // общий цвет скелета

            skeleton.SetColor(color);

         //  DLog("Добавляем цвет");
            skeletonAnimation.Update(0); // на всякий случай протолкнуть цвет в рендер

            yield return new WaitForSeconds(duration);

            skeleton.SetColor(originalColor);
           // DLog("Добавляем цвет");
            skeletonAnimation.Update(0);
          //  _flashRoutine = null;
        }


        // set character  animation 
        public void SetAnimation(AnimationReferenceAsset animationCurrent, bool loop, float timeScale)
        {
            skeletonAnimation.AnimationState.SetAnimation(0, animationCurrent, loop).TimeScale = timeScale;
        }

        void Start()
        {
            _ai = GetComponentInParent<WarriorAI>();
            skeletonAnimation.AnimationState.Event += HandleSpineEvent;
        }

        private void HandleSpineEvent(TrackEntry trackEntry, Spine.Event e)
        {
            switch (e.Data.Name)
            {
                case "Attack":
                case "attack":
                    DLog("Удар! Наносим урон!");
                    Attack();
                    break;
                case "Hit2":
                 //   DLog("Удар! Наносим урон!");
                    Attack();
                    break;
                case "lich_fireball":
                   DLog("Удар! Наносим урон! lich_fireball");
                    AttackLichFireball();
                    break;
                case "lich_fireball_end":
                  //  DLog("Удар! Наносим урон! lich_fireball");
                    AttackLichFireballEnd();
                    break;
                case "Appear":
                case "appear":
                //    DLog("Появление героя");
                    Appear();
                    break;
            }
        }

        private void Awake()
        {
            //  if (!_ai) _ai = GetComponentInParent<WarriorAI>();
                  if (!_visualRoot) _visualRoot = transform; // если не задано — вращаем сам объект
        }

        private void OnEnable()
        {
            if (_ai != null)
                _ai.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (_ai != null)
                _ai.StateChanged -= OnStateChanged;
        }

        /// <summary>
        /// Реакция на смену состояния в AI
        /// </summary>
        private void OnStateChanged(WarriorAI.State state)
        {
         //   DLog($"new SetAnimation state {state}");
            switch (state)
            {
                case WarriorAI.State.Idle:
                    PlayIdle();
                    break;
                case WarriorAI.State.MovingToBoss:
                case WarriorAI.State.Chasing:
                    PlayWalk();
                    break;
                case WarriorAI.State.Attacking:
                    PlayAttack();
                    break;
                case WarriorAI.State.Appear:
                    PlayAppear();
                    break;
                case WarriorAI.State.Death:
                    PlayDeath();
                    break;
                case WarriorAI.State.Roaming:
                    PlayRoaming();
                    break;
                case WarriorAI.State.RoamingWait:
                    PlayIdle();
                    break;
            }
        }

        public void PlayIdle()
        {
          //  DLog($"PlayIdle");
            SetAnimation(idleAnimation, true, 0.7f);
        }

        public void PlayAppear()
        {
            if ( _who == Hero.SkeletonArcher || _who == Hero.Skeleton  )
            {
                PlaySound(SoundId.SummonSkeleton);
            } else 
            if ( _who == Hero.GobArcher || _who == Hero.OrcWar)
            {
            //    PlaySound(SoundId.SkeletonAttack);
            } else if ( _who == Hero.Lich)
            {
          //      PlaySound(SoundId.AttackLichDefault);
            }else if ( _who == Hero.Shaman)
            {
             //   PlaySound(SoundId.AttackShamanDefault);
            }
            
            SetAnimation(appearAnimation, false, 1f);
        }


        public void PlayWalk()
        {
         //   DLog($"PlayWalk");
            SetAnimation(walkAnimation, true, 1f);
        }

        public void PlayRoaming()
        {
         //   DLog($"PlayWalk");
            SetAnimation(walkAnimation, true, 0.5f);
        }
        public void PlayCastMagicLich()
        { 
          //  DLog($"PlayCastMagicLich");
            SetAnimation(_attackCastLichAnimation, false, 1f);
        }
        public void PlayAttack()
        {
            if (!_ai.canAttack)
            {
                SetAnimation(idleAnimation, true, 0.7f);
             //   DLog($"Запрещена атака");
                return;
            }
            
            
            
 
            
            
            
          //  DLog($"PlayAttack");
            SetAnimation(attackAnimation, true, 1f);
        }


        public void PlayDeath()
        {

            if (_who == Hero.SkeletonArcher || _who == Hero.Skeleton)
            {
              //  Debug.Log($"Скелет звук смерти");
                PlaySound(SoundId.SkeletonDeath);
            } 
            if (_who == Hero.OrcWar || _who == Hero.GobArcher)
            {
              //  Debug.Log($"Orc звук смерти");
                PlaySound(SoundId.OrcDeath);
            } 
            SetAnimation(deathAnimation, false, 1f);
        }
        
        private void PlaySound(SoundId id)
        {
            
            if (!playAnimalSounds) return;
            if (AudioService.I == null) return;
            AudioService.I.Play(id, transform.position + soundOffset);
        }

        /// <summary>
        /// Вызывается из анимации в момент удара
        /// </summary>
        public void Attack()
        {
            if (!_ai.canAttack)
            {
                SetAnimation(idleAnimation, true, 0.7f);
                return;
            }  
            
             
            if ( _who == Hero.SkeletonArcher || _who == Hero.GobArcher)
            {
                PlaySound(SoundId.Archer_Attack);
            } else 
            if ( _who == Hero.Skeleton || _who == Hero.OrcWar)
            {
                PlaySound(SoundId.SkeletonAttack);
            } else if ( _who == Hero.Lich)
            {
                PlaySound(SoundId.AttackLichDefault);
            }else if ( _who == Hero.Shaman)
            {
                PlaySound(SoundId.AttackShamanDefault);
            }
            // else
            // {
            //     
            //     PlaySound(SoundId.Archer_Attack);
            // }g
            
       //     Debug.LogError($"_who {_who}");
             
            _ai.InvokeAttackFromAnimation();
        }

        private void AttackLichFireball()
        {  
          //  PlaySound(SoundId.ExplosionBoomLich);
            _ai.InvokeAttackLichFireballFromAnimation();
        }
        
        private void AttackLichFireballEnd()
        {             
            _ai.SetCanAttack(true);
            _ai.InvokeAppearFromAnimation();
        }
        
        public void Appear()
        { 
            _ai.InvokeAppearFromAnimation();
        }


        private void Update()
        {
            if (_ai == null) return;
            bool faceLeft = _ai.turnToFace();
            _visualRoot.localRotation = faceLeft ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;

        }
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void DLog(string msg)
        {
            if (debugAI) Debug.Log(msg);
        }
        
        // Возможные герои
        public enum Hero
        {
            Skeleton,
            SkeletonArcher,
            GobArcher,
            OrcWar,
            Lich,
            Shaman,
        }
        
    }
}