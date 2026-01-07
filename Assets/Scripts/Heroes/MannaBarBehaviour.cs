using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Heroes
{
    public class MannaBarBehaviour : MonoBehaviour
    {
        [SerializeField] Transform target; // герой
        [SerializeField] public Color Low;
        [SerializeField] public Color High;
        [SerializeField] public Vector3 Offset;
        [SerializeField] Vector3 worldOffset = new(0, 2, 0);


        [SerializeField] RectTransform canvasRect; // RectTransform корневого Canvas (Overlay)
        [SerializeField] RectTransform barRect; // RectTransform контейнера бара (обёртка над Slider)
        [SerializeField] Slider _slider;

        public void SetManna(float manna, float maxManna)
        {
            _slider.value = manna / maxManna;
        }
    }
}