using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
using Heroes;

namespace Weapons
{
    public class WeaponOwner : MonoBehaviour 
    { 
        [Header("Слот для оружия (куда крепим)")]
        public Transform handPivot;

 


        [Header("Ссылка на здоровье владельца")]
        public HeroesBase health;

        private void Awake()
        {
            if (health == null) health = GetComponent<HeroesBase>();
        }
    }
}