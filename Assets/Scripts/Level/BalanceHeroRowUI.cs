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

            
            Debug.Log($"[RowUI] {Hero} fields: hp={(hpField!=null)} mana={(manaField!=null)} dmg={(dmgField!=null)} cost={(costField!=null)} xp={(xpField!=null)}");

        }
        private void Awake()
        {
            // если забыли проставить в инспекторе
            if (hpField == null || manaField == null || dmgField == null || costField == null || xpField == null)
            {
                var inputs = GetComponentsInChildren<TMP_InputField>(true);

                // Ожидаем порядок как в иерархии строки: HP, Mana, Dmg, Cost, XP
                // Если у тебя другой порядок, лучше искать по имени объекта.
                if (inputs.Length >= 5)
                {
                    hpField = inputs[0];
                    manaField = inputs[1];
                    dmgField = inputs[2];
                    costField = inputs[3];
                    xpField = inputs[4];
                }
            }
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

            f.onValueChanged.RemoveListener(OnAnyChanged);
            f.onValueChanged.AddListener(OnAnyChanged);

            f.onEndEdit.RemoveListener(OnAnyChanged);
            f.onEndEdit.AddListener(OnAnyChanged);
        }

        private void OnAnyChanged(string _)
        {
            if (_suppressEvents) return;
         //   if (!IsDirty) SetDirty(true);
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
          //  SetDirty(false);
        }

        private int ParseInt(TMP_InputField field, int fallback)
        {
            if (field == null) return fallback;
            return int.TryParse(field.text, out var v) ? v : fallback;
        }
    }
}