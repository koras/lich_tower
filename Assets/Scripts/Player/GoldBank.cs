using UnityEngine;
using System;
using TMPro; // если используете TextMeshPro

namespace Player
{
    public class GoldBank : MonoBehaviour
    {
        
        
        [Header("Настройки")]
        [SerializeField, Min(0)] private int _startGold = 100000;
        [SerializeField, Min(1)] private int _addAmount = 1000; // количество золота за нажатие
        public int Gold { get; private set; }

        public event Action<int> OnGoldChanged; // новый баланс

        
        
        // Новый метод для добавления золота через кнопку
        public void AddGold()
        {
            Add(_addAmount);
        }

        // Метод для установки количества добавляемого золота
        public void SetAddAmount(int amount)
        {
            if (amount > 0)
                _addAmount = amount;
        }

        
        
        
        private void Awake()
        {
            Load();
            OnGoldChanged?.Invoke(Gold);
        }

        public bool Has(int amount) => amount <= Gold;

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (Gold < amount) return false;

            Gold -= amount;
            Debug.Log($"Spend Gold {Gold}");

            OnGoldChanged?.Invoke(Gold);
            Save();
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;

            Gold += amount;
            Debug.Log($"Gold add {Gold}");

            OnGoldChanged?.Invoke(Gold);
            Save();
        }

        private void Save()
        {
            SaveData data = new SaveData
            {
                Gold = Gold
            };

            SaveManager.Save(data);
        }

        private void Load()
        {
            if (SaveManager.TryLoad(out SaveData data))
            {
                Gold = data.Gold;
                Debug.Log($"Gold loaded {Gold}");
            }
            else
            {
                Gold = _startGold;
                Debug.Log($"Gold init {_startGold}");
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        private void OnDisable()
        {
            Save();
        }
    }
}