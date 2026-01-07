using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Level;

namespace Ui
{
    public class SpawnTimerUI : MonoBehaviour
    {
        [Header("Ссылки на UI")] [SerializeField]
        private TextMeshProUGUI timerText;

        [SerializeField] private Slider timerSlider;
        [SerializeField] private GameObject waveWarningPanel;
        [SerializeField] private TextMeshProUGUI waveCounterText;

        [Header("Настройки")] [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = Color.red;
        [SerializeField] private float warningThreshold = 10f; // секунды до предупреждения

        private float maxSpawnTime;
        private int waveCount = 0;

        private void Start()
        {
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.OnSpawnTimerUpdate += UpdateTimerUI;
                SpawnManager.Instance.OnWaveSpawned += OnWaveSpawned;

                maxSpawnTime = SpawnManager.Instance.GetTimeUntilNextSpawn();
                UpdateTimerUI(maxSpawnTime);
            }

            waveWarningPanel.SetActive(false);
        }

        private void UpdateTimerUI(float timeRemaining)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60);
                int seconds = Mathf.FloorToInt(timeRemaining % 60);
                timerText.text = $"{minutes:00}:{seconds:00}";

                // Меняем цвет при приближении спауна
                timerText.color = timeRemaining <= warningThreshold ? warningColor : normalColor;
            }

            if (timerSlider != null)
            {
                timerSlider.maxValue = maxSpawnTime;
                timerSlider.value = maxSpawnTime - timeRemaining;
            }

            // Активируем предупреждение
            if (waveWarningPanel != null)
            {
                bool showWarning = timeRemaining <= warningThreshold && timeRemaining > 0;
                waveWarningPanel.SetActive(showWarning);
            }
        }

        private void OnWaveSpawned()
        {
            waveCount++;

            if (waveCounterText != null)
            {
                waveCounterText.text = $"Волна: {waveCount}";
            }

            // Обновляем максимальное время для слайдера
            maxSpawnTime = SpawnManager.Instance.GetTimeUntilNextSpawn();

            // Анимация или эффект для новой волны
            StartCoroutine(WaveSpawnEffect());
        }

        private System.Collections.IEnumerator WaveSpawnEffect()
        {
            if (waveCounterText != null)
            {
                var originalScale = waveCounterText.transform.localScale;
                waveCounterText.transform.localScale = originalScale * 1.2f;
                yield return new WaitForSeconds(0.3f);
                waveCounterText.transform.localScale = originalScale;
            }
        }

        private void OnDestroy()
        {
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.OnSpawnTimerUpdate -= UpdateTimerUI;
                SpawnManager.Instance.OnWaveSpawned -= OnWaveSpawned;
            }
        }
    }
}