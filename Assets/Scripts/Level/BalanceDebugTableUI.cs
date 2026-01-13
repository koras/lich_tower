using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Heroes;
using Level;


namespace Level
{
    public class BalanceDebugTableUI : MonoBehaviour
    {
        [Header("Top UI")] [SerializeField] private TMP_Dropdown difficultyDropdown;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button reloadButton;
        [SerializeField] private Button resetDefaultButton;
        [SerializeField] private TMP_Text infoText;

        [Header("Table")] 
        
        [SerializeField] private Transform tableContent; // Content из ScrollView
        [SerializeField] private BalanceHeroRowUI rowPrefab; // prefab строки

        private readonly List<BalanceHeroRowUI> _rows = new();
        private bool _rebuilding;

        private void Awake()
        {
            if (difficultyDropdown)
                difficultyDropdown.onValueChanged.AddListener(_ => RebuildTable());

            if (saveButton) saveButton.onClick.AddListener(SaveAll);
            if (reloadButton) reloadButton.onClick.AddListener(Reload);
            if (resetDefaultButton) resetDefaultButton.onClick.AddListener(ResetDefault);
        }

        private void Start()
        {
            BuildDifficultyDropdown();
            RebuildTable();
            UpdateInfo();
        }

        private void BuildDifficultyDropdown()
        {
            if (!difficultyDropdown) return;

            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new List<string>
            {
                GameDifficulty.Easy.ToString(),
                GameDifficulty.Normal.ToString(),
                GameDifficulty.Hard.ToString()
            });

            // текущая сложность игры (из PlayerPrefs)
            difficultyDropdown.value = (int)GameSettings.Difficulty;
        }

        private GameDifficulty SelectedDifficulty =>
            (GameDifficulty)(difficultyDropdown ? difficultyDropdown.value : 1);

        public void RebuildTable()
        {
            if (_rebuilding) return;
            _rebuilding = true;

            ClearRows();

            var bm = BalanceManager.I;
            if (bm == null || !bm.IsLoaded)
            {
                UpdateInfo();
                _rebuilding = false;
                return;
            }

            // Перечисляем всех героев enum’а
            var heroes = (HeroesBase.Hero[])Enum.GetValues(typeof(HeroesBase.Hero));
            foreach (var hero in heroes)
            {
                // Берём данные из конфига, если нет, подставим дефолты
                HeroBalanceData data;
                if (!bm.TryGetHeroBalance(hero, SelectedDifficulty, out data))
                {
                    data = new HeroBalanceData { MaxHp = 100, MaxMana = 100, Damage = 10, Cost = 1, XpReward = 0 };
                }

                var row = Instantiate(rowPrefab, tableContent);
                row.Init(hero, data, OnRowDirtyChanged);

                _rows.Add(row);
            }

            UpdateInfo();
            _rebuilding = false;
        }

        private void ClearRows()
        {
            _rows.Clear();
            if (!tableContent) return;

            for (int i = tableContent.childCount - 1; i >= 0; i--)
                Destroy(tableContent.GetChild(i).gameObject);
        }

        private void OnRowDirtyChanged()
        {
            UpdateInfo();
        }

        private bool HasDirtyRows()
        {
            foreach (var r in _rows)
                if (r != null && r.IsDirty)
                    return true;
            return false;
        }

        private void SaveAll()
        {
            var bm = BalanceManager.I;
            if (bm == null) return;

            // Применяем все строки в memory-конфиг
            foreach (var row in _rows)
            {
                if (row == null) continue;

                var data = row.ReadData();
                bm.SetHeroBalance(row.Hero, SelectedDifficulty, data);
            }

            bm.SaveToDisk();

            // помечаем строки как сохранённые
            foreach (var row in _rows)
                row?.MarkSaved();

            UpdateInfo();
        }

        private void Reload()
        {
            var bm = BalanceManager.I;
            if (bm == null) return;

            bm.ReloadFromDisk();
            RebuildTable();
        }

        private void ResetDefault()
        {
            var bm = BalanceManager.I;
            if (bm == null) return;

            bm.ResetToDefaultFromStreaming();
            RebuildTable();
        }

        private void UpdateInfo()
        {
            if (!infoText) return;

            var bm = BalanceManager.I;
            string path = bm != null ? bm.GetPersistentPath() : "(BalanceManager missing)";

         //   infoText.text =
           //     $"Difficulty: {SelectedDifficulty}\n" +
          //      $"File: {path}\n" +
         //      $"State: {(HasDirtyRows() ? "Modified (not saved)" : "Clean")}";
        }
    }
}