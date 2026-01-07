using System.Collections.Generic;
using UnityEngine;

namespace Weapons.GobArcher
{
      public class ArrowPool : MonoBehaviour
    {
        public static ArrowPool Instance { get; private set; }
        
        [SerializeField] private GobArrowProjectile2D prefab;
        [SerializeField] private int prewarm = 64;
        [SerializeField] private bool disableInsteadOfDestroy = true; // новая опция

        private readonly Stack<GobArrowProjectile2D> _pool = new();
        private readonly List<GobArrowProjectile2D> _activeArrows = new();
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            for (int i = 0; i < prewarm; i++)
                CreateOne();
        }

        private GobArrowProjectile2D CreateOne()
        {
            var a = Instantiate(prefab);
            a.transform.SetParent(transform, false);
            a.gameObject.SetActive(false);
            a.SetPool(this);
            _pool.Push(a);
            return a;
        }

        public GobArrowProjectile2D Get(Vector2 pos, Quaternion rot)
        {
            if (_pool.Count == 0) CreateOne();

            var a = _pool.Pop();
            a.transform.SetPositionAndRotation(pos, rot);
            a.gameObject.SetActive(true);
            
            if (!_activeArrows.Contains(a))
                _activeArrows.Add(a);
                
            return a;
        }

        public void Release(GobArrowProjectile2D arrow)
        {
            if (_activeArrows.Contains(arrow))
                _activeArrows.Remove(arrow);
                
            arrow.gameObject.SetActive(false);
            
            // Возвращаем в пул только если не уничтожаем
            if (!disableInsteadOfDestroy || arrow == null || arrow.gameObject == null)
                return;
                
            _pool.Push(arrow);
        }

        // Метод для принудительной очистки всех активных стрел
        public void CleanupAllArrows()
        {
            for (int i = _activeArrows.Count - 1; i >= 0; i--)
            {
                if (_activeArrows[i] != null)
                {
                    _activeArrows[i].StartDespawn();
                }
            }
            _activeArrows.Clear();
        }
    }
}