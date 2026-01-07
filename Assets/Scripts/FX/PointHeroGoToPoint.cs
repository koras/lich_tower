using UnityEngine;
using Spine.Unity;


namespace FX
{
    public class PointHeroGoToPoint : MonoBehaviour
    {
       [SerializeField] private float lifetime = 2f;

      private void Start()
      {
         Destroy(gameObject, lifetime);
       }	
    }
}