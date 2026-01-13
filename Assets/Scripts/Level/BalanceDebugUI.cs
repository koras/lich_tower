using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Heroes;
using Level;

namespace Level
{
    public class BalanceDebugUI : MonoBehaviour
    {
        [Header("UI")] [SerializeField] private TMP_Dropdown difficultyDropdown;
        [SerializeField] private TMP_Dropdown heroDropdown;

        [SerializeField] private TMP_InputField hpField;
        [SerializeField] private TMP_InputField manaField;
        [SerializeField] private TMP_InputField dmgField;
        [SerializeField] private TMP_InputField costField;
        [SerializeField] private TMP_InputField xpField;

        [SerializeField] private Button applyButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button reloadButton;
        [SerializeField] private Button resetDefaultButton;

        [Header("Info")] [SerializeField] private TMP_Text infoText;
        [SerializeField] private Image dirtyIndicator; // маленький квадратик/иконка "изменено"

        private bool _dirty;

        private void Awake()
        {
            if (applyButton) applyButton.onClick.AddListener(OnApplyClicked);
            if (saveButton) saveButton.onClick.AddListener(OnSaveClicked);
            if (reloadButton) reloadButton.onClick.AddListener(OnReloadClicked);
            if (resetDefaultButton) resetDefaultButton.onClick.AddListener(OnResetDefaultClicked);

            if (difficultyDropdown) difficultyDropdown.onValueChanged.AddListener(_ => OnSelectionChanged());
            if (heroDropdown) heroDropdown.onValueChanged.AddListener(_ => OnSelectionChanged());

            HookDirty(hpField);
            HookDirty(manaField);
            HookDirty(dmgField);
            HookDirty(costField);
            HookDirty(xpField);
        }

        private void Start()
        {
            BuildDropdowns();
            RefreshFieldsFromConfig();
            UpdateInfo();
            SetDirty(false);
        }

        private void BuildDropdowns()
        {
            // Difficulty
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                GameDifficulty.Easy.ToString(),
                GameDifficulty.Normal.ToString(),
                GameDifficulty.Hard.ToString()
            });

            // выставим текущую сложность игры
            difficultyDropdown.value = (int)GameSettings.Difficulty;

            // Heroes
            heroDropdown.ClearOptions();
            var names = Enum.GetNames(typeof(HeroesBase.Hero));
            heroDropdown.AddOptions(new System.Collections.Generic.List<string>(names));
            heroDropdown.value = 0;
        }

        private void HookDirty(TMP_InputField f)
        {
            if (f == null) return;
            f.onValueChanged.AddListener(_ => SetDirty(true));
        }

        private GameDifficulty SelectedDifficulty =>
            (GameDifficulty)difficultyDropdown.value;

        private HeroesBase.Hero SelectedHero =>
            (HeroesBase.Hero)heroDropdown.value;

        private void OnSelectionChanged()
        {
            RefreshFieldsFromConfig();
            UpdateInfo();
            SetDirty(false);
        }

        private void RefreshFieldsFromConfig()
        {
            var bm = BalanceManager.I;
            if (bm == null || !bm.IsLoaded)
            {
                SetFields(0, 0, 0, 0, 0);
                return;
            }

            if (bm.TryGetHeroBalance(SelectedHero, SelectedDifficulty, out var data))
            {
                SetFields(data.MaxHp, data.MaxMana, data.Damage, data.Cost, data.XpReward);
            }
            else
            {
                // если нет в конфиге, показываем дефолты (или нули)
                SetFields(100, 100, 10, 1, 0);
            }
        }

        private void SetFields(int hp, int mana, int dmg, int cost, int xp)
        {
            if (hpField) hpField.text = hp.ToString();
            if (manaField) manaField.text = mana.ToString();
            if (dmgField) dmgField.text = dmg.ToString();
            if (costField) costField.text = cost.ToString();
            if (xpField) xpField.text = xp.ToString();
        }

        private void OnApplyClicked()
        {
            var bm = BalanceManager.I;
            if (bm == null) return;

            var data = new HeroBalanceData
            {
                MaxHp = ParseInt(hpField, 100),
                MaxMana = ParseInt(manaField, 100),
                Damage = ParseInt(dmgField, 10),
                Cost = ParseInt(costField, 1),
                XpReward = ParseInt(xpField, 0)
            };

            bm.SetHeroBalance(SelectedHero, SelectedDifficulty, data);
            SetDirty(true);
            UpdateInfo();
        }

        private void OnSaveClicked()
        {
            var bm = BalanceManager.I;
            if (bm == null) return;

            // Применяем на всякий случай, чтобы "сохранить" сохраняло то, что в полях
            OnApplyClicked();

            bm.SaveToDisk();
            SetDirty(false);
            UpdateInfo();
        }

        private void OnReloadClicked()
        {
            var bm = BalanceManager.I;
            if (bm == null) return;

            bm.ReloadFromDisk();
            RefreshFieldsFromConfig();
            SetDirty(false);
            UpdateInfo();
        }

        private void OnResetDefaultClicked()
        {
            var bm = BalanceManager.I;
            if (bm == null) return;

            bm.ResetToDefaultFromStreaming();
            RefreshFieldsFromConfig();
            SetDirty(false);
            UpdateInfo();
        }

        private int ParseInt(TMP_InputField field, int fallback)
        {
            if (field == null) return fallback;
            if (int.TryParse(field.text, out var v)) return v;
            return fallback;
        }

        private void SetDirty(bool dirty)
        {
            _dirty = dirty;
            if (dirtyIndicator) dirtyIndicator.enabled = dirty;
        }

        private void UpdateInfo()
        {
            if (infoText == null) return;

            var bm = BalanceManager.I;
            string path = bm != null ? bm.GetPersistentPath() : "(BalanceManager missing)";
            infoText.text =
                $"Editing: {SelectedDifficulty} / {SelectedHero}\n" +
                $"File: {path}\n" +
                $"State: {(_dirty ? "Modified (not saved)" : "Saved/clean")}";
        }
    }
}