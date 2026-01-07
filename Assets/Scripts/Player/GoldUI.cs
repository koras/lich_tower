using UnityEngine;

using TMPro; // если используете TextMeshPro

namespace Player
{
    public class GoldUI : MonoBehaviour
    {
        
        
        [Header("Настройки")]
        [SerializeField] private GoldBank _goldBank = null;
        [SerializeField] private TMP_Text _goldText = null;  // если TextMeshPro
    
        
 
        
        private void OnEnable()
        {
            if (_goldBank != null)
            {
                _goldBank.OnGoldChanged += HandleGoldChanged;
                // сразу отобразим текущее
                HandleGoldChanged(_goldBank.Gold);
            }
        }

        private void OnDisable()
        {
            if (_goldBank != null)
            {
                _goldBank.OnGoldChanged -= HandleGoldChanged;
            }
        }

        private void HandleGoldChanged(int newGold)
        {
            if (_goldText != null)
                _goldText.text = $"{newGold}";
        }
        
        
        public enum State
        {
            Skeleton,
            SkeletonArcher,
            GobArcher,
            OrcWar,
        }
    }
}