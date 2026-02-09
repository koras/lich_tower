using UnityEngine;
using DG.Tweening;


namespace Player
{
    public class WeaponController : MonoBehaviour
    {
        [Header("Joystick")] [SerializeField] private DynamicJoystick rightJoystick; // Ссылка на правый джойстик
        [SerializeField] private float joystickDeadZone = 0.1f; // Минимальное значение для реакции

        [Header("Aiming")] [SerializeField] private Transform weaponPivot; // Объект-родитель для оружия
        [SerializeField] private SpriteRenderer aimSprite; // Спрайт стрелки прицеливания
        [SerializeField] private float maxAimLength = 3f; // Максимальная длина стрелки
        [SerializeField] private float aimFadeSpeed = 8f; // Скорость появления/исчезновения

        [Header("Shooting")] [SerializeField] private GameObject projectilePrefab; // Префаб снаряда
        [SerializeField] private Transform firePoint; // Точка вылета снаряда
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private float shootCooldown = 0.5f;

        private bool isAiming = false;
        private bool canShoot = true;
        private Vector2 aimDirection;
        private float aimAlpha = 0f; // Текущая прозрачность прицела (0-1)
        private Vector3 originalAimScale; // Исходный масштаб спрайта стрелки

        private void Start()
        {
            // Сохраняем исходный масштаб спрайта
            if (aimSprite != null)
            {
                originalAimScale = aimSprite.transform.localScale;
                aimSprite.color = new Color(aimSprite.color.r, aimSprite.color.g, aimSprite.color.b, 0f);
                aimSprite.enabled = true; // Оставляем включенным, но делаем прозрачным
            }
        }

        private void Update()
        {
            HandleAiming();
            UpdateAimFade();
        }

        private void HandleAiming()
        {
            // Получаем ввод с правого джойстика
            float horizontalInput = rightJoystick.Horizontal;
            float verticalInput = rightJoystick.Vertical;
            Vector2 inputVector = new Vector2(horizontalInput, verticalInput);

            // Проверяем, активен ли джойстик (палец игрока на нем)
            if (inputVector.magnitude > joystickDeadZone)
            {
                isAiming = true;
                aimDirection = inputVector.normalized;

                // Поворачиваем оружие в направлении прицеливания
                if (weaponPivot != null)
                {
                    float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
                    weaponPivot.rotation = Quaternion.Euler(0, 0, angle);
                }

                // Обновляем позицию и размер стрелки прицела
                UpdateAimSprite();
            }
            else
            {
                // Если джойстик отпустили и до этого прицеливались - стреляем
                if (isAiming && canShoot)
                {
                 //   Shoot();
                 Debug.Log("Стреляем");
                }

                isAiming = false;
            }
        }

        private void UpdateAimFade()
        {
            // Плавно меняем прозрачность прицела
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

            // Вычисляем длину стрелки в зависимости от силы натяжения джойстика
            float joystickMagnitude = new Vector2(rightJoystick.Horizontal, rightJoystick.Vertical).magnitude;
            float currentAimLength = Mathf.Clamp(joystickMagnitude * maxAimLength, 0.2f, maxAimLength);

            // Позиция стрелки - посередине между игроком и точкой прицеливания
            Vector2 aimPosition = (Vector2)firePoint.position + aimDirection * (currentAimLength / 2f);
            aimSprite.transform.position = aimPosition;

            // Поворачиваем стрелку в направлении прицеливания
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            aimSprite.transform.rotation = Quaternion.Euler(0, 0, angle);

            // Масштабируем стрелку по длине (только по X)
            Vector3 newScale = originalAimScale;
            newScale.x = originalAimScale.x * (currentAimLength / maxAimLength);
            aimSprite.transform.localScale = newScale;
        }

        private void Shoot()
        {
            if (projectilePrefab == null || firePoint == null) return;

            // Создаем снаряд
            GameObject projectile = Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.Euler(0, 0, Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg)
            );

            // Задаем скорость снаряду
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = aimDirection * projectileSpeed;
            }

            // Можно добавить эффект выстрела
            // Instantiate(muzzleFlashEffect, firePoint.position, firePoint.rotation);

            // Запускаем перезарядку
            StartCoroutine(ShootCooldown());
        }

        private System.Collections.IEnumerator ShootCooldown()
        {
            canShoot = false;
            yield return new WaitForSeconds(shootCooldown);
            canShoot = true;
        }

        // Вспомогательный метод для отладки (визуализация в редакторе)
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && isAiming && firePoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(firePoint.position, (Vector2)firePoint.position + aimDirection * maxAimLength);
                Gizmos.DrawSphere((Vector2)firePoint.position + aimDirection * maxAimLength, 0.1f);
            }
        }
    }
}