using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Heroes;
using Level;

namespace Level
{
    public class BalanceHeroRowUI : MonoBehaviour
    {
        [Header("UI")] [SerializeField] private TMP_Text heroNameText;

        [SerializeField] private TMP_InputField hpField;
        [SerializeField] private TMP_InputField manaField;
        [SerializeField] private TMP_InputField dmgField;
        [SerializeField] private TMP_InputField costField;
        [SerializeField] private TMP_InputField xpField;

        [SerializeField] private Image dirtyMark;

        public HeroesBase.Hero Hero { get; private set; }
        public bool IsDirty { get; private set; }

        private Action _onDirtyChanged;
        private bool _suppressEvents;

        public void Init(HeroesBase.Hero hero, HeroBalanceData data, Action onDirtyChanged)
        {
            Hero = hero;
            _onDirtyChanged = onDirtyChanged;

            if (heroNameText) heroNameText.text = hero.ToString();

            _suppressEvents = true;
            SetFields(data);
            _suppressEvents = false;

            HookDirty(hpField);
            HookDirty(manaField);
            HookDirty(dmgField);
            HookDirty(costField);
            HookDirty(xpField);

            SetDirty(false);
        }

        public void SetFields(HeroBalanceData data)
        {
            if (hpField) hpField.text = data.MaxHp.ToString();
            if (manaField) manaField.text = data.MaxMana.ToString();
            if (dmgField) dmgField.text = data.Damage.ToString();
            if (costField) costField.text = data.Cost.ToString();
            if (xpField) xpField.text = data.XpReward.ToString();
        }

        private void HookDirty(TMP_InputField f)
        {
            if (f == null) return;

            // На всякий случай снимаем старые листенеры, если row будет переиспользоваться
            f.onValueChanged.RemoveListener(OnAnyChanged);
            f.onValueChanged.AddListener(OnAnyChanged);
        }

        private void OnAnyChanged(string _)
        {
            if (_suppressEvents) return;
            if (!IsDirty) SetDirty(true);
        }

        private void SetDirty(bool dirty)
        {
            IsDirty = dirty;
            if (dirtyMark) dirtyMark.enabled = dirty;
            _onDirtyChanged?.Invoke();
        }

        public HeroBalanceData ReadData()
        {
            return new HeroBalanceData
            {
                MaxHp = ParseInt(hpField, 100),
                MaxMana = ParseInt(manaField, 100),
                Damage = ParseInt(dmgField, 10),
                Cost = ParseInt(costField, 1),
                XpReward = ParseInt(xpField, 0)
            };
        }

        public void MarkSaved()
        {
            SetDirty(false);
        }

        private int ParseInt(TMP_InputField field, int fallback)
        {
            if (field == null) return fallback;
            return int.TryParse(field.text, out var v) ? v : fallback;
        }
    }
}