using UnityEngine;
using DG.Tweening;
using Weapons;
using System;
using Weapons.Range;
using Heroes;

namespace Player
{
    public class WeaponController : MonoBehaviour
    {
        [Header("Joystick")] 
        [SerializeField] private DynamicJoystick rightJoystick;
        [SerializeField] private float joystickDeadZone = 0.1f;

        private Transform firePoint;
        private float _lastJoystickMagnitude;
        
        [Header("Aiming")] 
        [SerializeField] private Transform weaponPivot;
        [SerializeField] private SpriteRenderer aimSprite;
        [SerializeField] private float maxAimLength = 3f;
        [SerializeField] private float aimFadeSpeed = 8f;
        [SerializeField] private float minShootDistance = 1f; // Минимальное расстояние выстрела

        [Header("Shooting")] 
        [SerializeField] private float shootCooldown = 0.5f;
        [SerializeField] private float distanceMultiplier = 1.5f; // Множитель расстояния (можно регулировать)

        
        private Joystick _rightJoyBase;
        
        private bool isAiming = false;
        private bool canShoot = true;
        private Vector2 _aimDirection;
        private float aimAlpha = 0f;
        private Vector3 originalAimScale;
 

         private HeroesBase _heroesBase;

        // Для визуализации точки попадания (опционально)
        [Header("Debug")]
        [SerializeField] private bool showHitPointDebug = true;
        private Vector3 lastHitPoint;


        private WarriorAI _ai;

        private void Awake()
        {
           // _ai =  GetComponentInParent<WarriorAI>();
            _ai =  GetComponent<WarriorAI>();
            if (_ai == null)
            {
                Debug.Log($"[WeaponController] Оружие не найдено");
            }
  //          _heroesBase =  GetComponentInParent<HeroesBase>();
            _heroesBase =  GetComponent<HeroesBase>();
            if (_heroesBase == null)
            {
                Debug.Log($"[WeaponController] Герой не найдено");
            }
            
 
        }

        private void Start()
        {
            if (aimSprite != null)
            {
                originalAimScale = aimSprite.transform.localScale;
                aimSprite.color = new Color(aimSprite.color.r, aimSprite.color.g, aimSprite.color.b, 0f);
                aimSprite.enabled = true;
            }
        }

        private void Update()
        {
            HandleAiming();
            UpdateAimFade();
        }

        private void HandleAiming()
        {
      
            
            float horizontalInput = rightJoystick.Horizontal;
            float verticalInput = rightJoystick.Vertical;
            Vector2 inputVector = new Vector2(horizontalInput, verticalInput);

            if (inputVector.magnitude > joystickDeadZone)
            {
                Debug.Log($"[WeaponController] HandleAiming inputVector.magnitude > joystickDeadZone");
                isAiming = true;
                _aimDirection= inputVector.normalized;
                _lastJoystickMagnitude = inputVector.magnitude; // <-- запомнили силу

                if (weaponPivot != null)
                {
                    float angle = Mathf.Atan2(_aimDirection.y, _aimDirection.x) * Mathf.Rad2Deg;
                    weaponPivot.rotation = Quaternion.Euler(0, 0, angle);
                }

                UpdateAimSprite();
            }
            else
            {
                if (isAiming && canShoot)
                {
                    PerformShoot(_aimDirection, _lastJoystickMagnitude); // <-- передаем сохраненное
                    Debug.Log($"[WeaponController] HandleAiming isAiming && canShoot");
                   // PerformShoot(_aimDirection);
                }

                isAiming = false;
            }
        }

        private void PerformShoot(Vector2 aimDirection, float joystickMagnitude)
        {
            if (_ai == null)
            {
                Debug.Log($"[WeaponController] не назначен _ai ");
                return;
            }
 
            // Вычисляем силу натяжения джойстика
          //  float joystickMagnitude = new Vector2(rightJoystick.Horizontal, rightJoystick.Vertical).magnitude;
            
            // Вычисляем расстояние выстрела с учетом силы джойстика
            float shootDistance = CalculateShootDistance(joystickMagnitude);
            
            // Вычисляем точку попадания (куда летит снаряд/наносится урон)
            Vector3 hitPoint = CalculateHitPoint(shootDistance);
            
            // Сохраняем для дебага
            lastHitPoint = hitPoint;
            // ПЕРЕДАЕМ НАПРАВЛЕНИЕ ВМЕСТО ТОЧКИ!
            _ai.StartAttackAndSetTargetDirection(aimDirection);
          
            Debug.Log($"[WeaponController] Shoot! Direction: {aimDirection}, Distance: {shootDistance:F2}, Point: {hitPoint}");

        }

        private float CalculateShootDistance(float joystickMagnitude)
        {
            // Минимальное расстояние + часть от максимального в зависимости от силы джойстика
            float distance = minShootDistance + (joystickMagnitude * (maxAimLength - minShootDistance));
            
            // Можно добавить множитель для регулировки дальности
            distance *= distanceMultiplier;
            
            return Mathf.Clamp(distance, minShootDistance, maxAimLength * distanceMultiplier);
        }

        private Vector3 CalculateHitPoint(float distance)
        {
            if (firePoint == null)
                return transform.position + (Vector3)_aimDirection * distance;
            
            // От точки выстрела откладываем расстояние в направлении прицела
            return firePoint.position + (Vector3)_aimDirection * distance;
        }

        private void UpdateAimFade()
        {
            float targetAlpha = isAiming ? 1f : 0f;
            aimAlpha = Mathf.MoveTowards(aimAlpha, targetAlpha, aimFadeSpeed * Time.deltaTime);

            if (aimSprite != null)
            {
                Color currentColor = aimSprite.color;
                currentColor.a = aimAlpha;
                aimSprite.color = currentColor;
            }
        }

        private void UpdateAimSprite()
        {
            if (aimSprite == null || firePoint == null) return;

            float joystickMagnitude = new Vector2(rightJoystick.Horizontal, rightJoystick.Vertical).magnitude;
            float currentAimLength = Mathf.Clamp(joystickMagnitude * maxAimLength, 0.2f, maxAimLength);

            // Позиция стрелки - посередине между игроком и точкой прицеливания
            Vector2 aimPosition = (Vector2)firePoint.position + _aimDirection * (currentAimLength / 2f);
            aimSprite.transform.position = aimPosition;

            float angle = Mathf.Atan2(_aimDirection.y, _aimDirection.x) * Mathf.Rad2Deg;
            aimSprite.transform.rotation = Quaternion.Euler(0, 0, angle);

            Vector3 newScale = originalAimScale;
            newScale.x = originalAimScale.x * (currentAimLength / maxAimLength);
            aimSprite.transform.localScale = newScale;
        }

        

        // private void OnDrawGizmos()
        // {
        //     if (Application.isPlaying && isAiming && firePoint != null)
        //     {
        //         // Линия прицеливания
        //         Gizmos.color = Color.red;
        //         float joystickMagnitude = new Vector2(rightJoystick.Horizontal, rightJoystick.Vertical).magnitude;
        //         float debugDistance = CalculateShootDistance(joystickMagnitude);
        //         Vector3 debugHitPoint = CalculateHitPoint(debugDistance);
        //         
        //         Gizmos.DrawLine(firePoint.position, debugHitPoint);
        //         
        //         // Точка попадания
        //         Gizmos.color = Color.green;
        //         Gizmos.DrawSphere(debugHitPoint, 0.15f);
        //     }
        //
        //     // Отображаем последнюю точку попадания (даже когда не целимся)
        //     if (showHitPointDebug && Application.isPlaying)
        //     {
        //         Gizmos.color = Color.yellow;
        //         Gizmos.DrawSphere(lastHitPoint, 0.1f);
        //         Gizmos.DrawWireSphere(lastHitPoint, 0.2f);
        //     }
        // }

        // Вспомогательный метод для получения данных о выстреле (если нужно другим скриптам)
        public Vector3 GetLastHitPoint()
        {
            return lastHitPoint;
        }

        public Vector2 GetLastAimDirection()
        {
            return _aimDirection;
        }
    }
}