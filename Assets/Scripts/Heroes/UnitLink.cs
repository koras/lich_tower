using UnityEngine;

namespace Heroes
{
    public class UnitLink : MonoBehaviour
    {
        public HeroesBase Hp;
        private void Awake() => Hp = GetComponentInParent<HeroesBase>();
    }
}