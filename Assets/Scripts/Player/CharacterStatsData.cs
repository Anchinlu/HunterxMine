using System;

namespace MineCraftUnity.Player
{
    [Serializable]
    public struct CharacterStatBlock
    {
        public int hp;
        public int strength;
        public int defense;
        public int agility;
        public int dexterity;
        public int intelligence;
        public int mana;
        public int stamina;
    }

    [Serializable]
    public struct CharacterGrowth
    {
        public int hpPerLevel;
        public int strengthPerLevel;
        public int defensePerLevel;
        public int agilityPerLevel;
        public int dexterityPerLevel;
        public int intelligencePerLevel;
        public int manaPerLevel;
        public int staminaPerLevel;
    }
}
