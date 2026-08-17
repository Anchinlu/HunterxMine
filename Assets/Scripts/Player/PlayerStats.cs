using System;
using UnityEngine;

namespace MineCraftUnity.Player
{
    public enum StatType
    {
        Health,
        Mana,
        Stamina,
        Strength,
        Defense,
        Agility,
        Dexterity,
        Intelligence
    }

    public class PlayerStats : MonoBehaviour
    {
        public event Action StatsChanged;

        [SerializeField] private CharacterClass currentClass = CharacterClass.Warrior;
        [SerializeField] private int level = 1;

        [SerializeField] private int unspentStatPoints = 3;

        [Header("Allocated Bonuses")]
        [SerializeField] private int bonusHealth;
        [SerializeField] private int bonusMana;
        [SerializeField] private int bonusStamina;
        [SerializeField] private int bonusStrength;
        [SerializeField] private int bonusDefense;
        [SerializeField] private int bonusAgility;
        [SerializeField] private int bonusDexterity;
        [SerializeField] private int bonusIntelligence;

        public CharacterClass CurrentClass => currentClass;
        public int UnspentStatPoints
        {
            get => unspentStatPoints;
            private set
            {
                unspentStatPoints = Mathf.Max(0, value);
                NotifyChanged();
            }
        }
        public int Level
        {
            get => level;
            set
            {
                var newLevel = Mathf.Max(1, value);
                if (newLevel != level)
                {
                    level = newLevel;
                    NotifyChanged();
                }
            }
        }

        public int Strength { get; private set; }
        public int Defense { get; private set; }
        public int Agility { get; private set; }
        public int Dexterity { get; private set; }
        public int Intelligence { get; private set; }

        [SerializeField] private float _health = 100f;
        [SerializeField] private float _maxHealth = 100f;
        
        [SerializeField] private float _mana = 100f;
        [SerializeField] private float _maxMana = 100f;
        
        [SerializeField] private float _stamina = 100f;
        [SerializeField] private float _maxStamina = 100f;

        [SerializeField] private int _foodLevel = 20;
        [SerializeField] private int _maxFoodLevel = 20;
        [SerializeField] private float _saturation = 5f;
        [SerializeField] private int _armorValue = 0;
        [SerializeField] private float _experienceProgress = 0f;
        [SerializeField] private int _experienceLevel = 0;

        public float Health
        {
            get => _health;
            set { _health = Mathf.Clamp(value, 0f, _maxHealth); NotifyChanged(); }
        }

        public float MaxHealth
        {
            get => _maxHealth;
            set { _maxHealth = Mathf.Max(1f, value); NotifyChanged(); }
        }

        public float Mana
        {
            get => _mana;
            set { _mana = Mathf.Clamp(value, 0f, _maxMana); NotifyChanged(); }
        }

        public float MaxMana
        {
            get => _maxMana;
            set { _maxMana = Mathf.Max(1f, value); NotifyChanged(); }
        }

        public float Stamina
        {
            get => _stamina;
            set { _stamina = Mathf.Clamp(value, 0f, _maxStamina); NotifyChanged(); }
        }

        public float MaxStamina
        {
            get => _maxStamina;
            set { _maxStamina = Mathf.Max(1f, value); NotifyChanged(); }
        }

        public int FoodLevel
        {
            get => _foodLevel;
            set { _foodLevel = Mathf.Clamp(value, 0, _maxFoodLevel); NotifyChanged(); }
        }

        public int MaxFoodLevel
        {
            get => _maxFoodLevel;
            set { _maxFoodLevel = Mathf.Max(1, value); NotifyChanged(); }
        }

        public float Saturation
        {
            get => _saturation;
            set { _saturation = Mathf.Max(0f, value); NotifyChanged(); }
        }

        public int ArmorValue
        {
            get => _armorValue;
            set { _armorValue = Mathf.Max(0, value); NotifyChanged(); }
        }

        public float ExperienceProgress
        {
            get => _experienceProgress;
            set { _experienceProgress = Mathf.Clamp01(value); NotifyChanged(); }
        }

        public int ExperienceLevel
        {
            get => _experienceLevel;
            set { _experienceLevel = Mathf.Max(0, value); NotifyChanged(); }
        }

        private void NotifyChanged()
        {
            StatsChanged?.Invoke();
        }

        public void AddStatPoints(int points)
        {
            if (points <= 0) return;
            // Use field directly so we only call NotifyChanged once if we want, or use property.
            // Property calls NotifyChanged.
            UnspentStatPoints += points;
        }

        public bool SpendStatPoint(StatType stat)
        {
            if (unspentStatPoints <= 0) return false;

            switch (stat)
            {
                case StatType.Health: bonusHealth++; _maxHealth++; break;
                case StatType.Mana: bonusMana++; _maxMana++; break;
                case StatType.Stamina: bonusStamina++; _maxStamina++; break;
                case StatType.Strength: bonusStrength++; Strength++; break;
                case StatType.Defense: bonusDefense++; Defense++; break;
                case StatType.Agility: bonusAgility++; Agility++; break;
                case StatType.Dexterity: bonusDexterity++; Dexterity++; break;
                case StatType.Intelligence: bonusIntelligence++; Intelligence++; break;
            }

            unspentStatPoints--;
            NotifyChanged();
            return true;
        }

        public void ApplyClass(CharacterClassDefinition definition, bool refillResources = true)
        {
            if (definition == null) return;

            currentClass = definition.characterClass;
            
            int lvlOffset = level - 1;
            var baseStats = definition.levelOneStats;
            var growth = definition.growth;

            _maxHealth = baseStats.hp + growth.hpPerLevel * lvlOffset + bonusHealth;
            Strength = baseStats.strength + growth.strengthPerLevel * lvlOffset + bonusStrength;
            Defense = baseStats.defense + growth.defensePerLevel * lvlOffset + bonusDefense;
            Agility = baseStats.agility + growth.agilityPerLevel * lvlOffset + bonusAgility;
            Dexterity = baseStats.dexterity + growth.dexterityPerLevel * lvlOffset + bonusDexterity;
            Intelligence = baseStats.intelligence + growth.intelligencePerLevel * lvlOffset + bonusIntelligence;
            _maxMana = baseStats.mana + growth.manaPerLevel * lvlOffset + bonusMana;
            _maxStamina = baseStats.stamina + growth.staminaPerLevel * lvlOffset + bonusStamina;

            if (refillResources)
            {
                _health = _maxHealth;
                _mana = _maxMana;
                _stamina = _maxStamina;
            }
            else
            {
                _health = Mathf.Clamp(_health, 0, _maxHealth);
                _mana = Mathf.Clamp(_mana, 0, _maxMana);
                _stamina = Mathf.Clamp(_stamina, 0, _maxStamina);
            }

            NotifyChanged();
        }
    }
}
