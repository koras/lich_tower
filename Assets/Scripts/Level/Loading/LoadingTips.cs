using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;


namespace Level.Loading
{
    public class LoadingTips : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text tipText;

        [Header("Настройки")]
        [SerializeField] private float changeInterval = 2.5f;
        [TextArea(2, 3)]
        [SerializeField] private List<string> tips = new List<string>();

        private int _lastIndex = -1;

        private void OnEnable()
        {
            if (tips.Count == 0 || tipText == null)
                return;

            StartCoroutine(TipsRoutine());
        }

        private IEnumerator TipsRoutine()
        {
            while (true)
            {
                int index;
                do
                {
                    index = Random.Range(0, tips.Count);
                }
                while (index == _lastIndex && tips.Count > 1);

                _lastIndex = index;
                tipText.text = tips[index];

                yield return new WaitForSeconds(changeInterval);
            }
        }
    }
}