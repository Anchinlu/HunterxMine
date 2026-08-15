using System;
using UnityEngine;

namespace MineCraftUnity.Player
{
    public class PlayerStats : MonoBehaviour
    {
        public event Action StatsChanged;

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
    }
}
