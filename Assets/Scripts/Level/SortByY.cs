using UnityEngine;
using UnityEngine.Rendering;


namespace Level
{
    [RequireComponent(typeof(SortingGroup))]
    public class SortByY : MonoBehaviour
    {
        [SerializeField] private int offset = 0;
        [SerializeField] private float precision = 100f; // 100 = 0.01 по Y

        private SortingGroup _sg;

        private void Awake() => _sg = GetComponent<SortingGroup>();

        private void LateUpdate()
        {
            // ниже по Y -> больше order -> поверх
            _sg.sortingOrder = offset + Mathf.RoundToInt(-transform.position.y * precision);
        }
    }

}
