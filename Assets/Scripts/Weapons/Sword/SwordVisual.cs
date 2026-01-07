using System.Collections;
using UnityEngine;

 
namespace Weapons
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    
    public class SwordVisual : MonoBehaviour
    {
        // [SerializeField] private Sword sword;
        private Animator _animator;
        [SerializeField]private WeaponBase weapon; // слушаем базовое событие
        
        private static readonly int Attack1 = Animator.StringToHash("Attack");
        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
         //   weapon = GetComponentInParent<WeaponBase>();
        //    if (weapon != null) weapon.OnAttack += HandleAttack;
       //     else Debug.LogError("[SwordVisual] Не найден WeaponBase у родителя");
        }
        private void OnDisable()
        {
        //    if (weapon != null) weapon.OnAttack -= HandleAttack;
        }
        private void HandleAttack()
        {
         //   _animator.SetTrigger(Attack1); // триггер "Attack" должен быть в Animator Controller
        }

    }
}