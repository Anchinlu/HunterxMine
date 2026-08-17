using UnityEditor;
using UnityEngine;
using MineCraftUnity.Player;
using System.IO;

namespace MineCraftUnity.Editor
{
    public static class CharacterClassAssetGenerator
    {
        [InitializeOnLoadMethod]
        private static void GenerateAssets()
        {
            string folderPath = "Assets/Resources/CharacterClasses";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                var folders = folderPath.Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    if (!AssetDatabase.IsValidFolder(currentPath + "/" + folders[i]))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath += "/" + folders[i];
                }
            }

            GenerateClass(CharacterClass.Warrior, 38, 40, 32, 27, 25, 15, 15, 38);
            GenerateClass(CharacterClass.Archer, 25, 22, 18, 40, 43, 20, 22, 30);
            GenerateClass(CharacterClass.Mage, 20, 12, 15, 22, 25, 48, 45, 24);
            GenerateClass(CharacterClass.HeavyArmor, 50, 35, 48, 12, 15, 10, 10, 45);
            GenerateClass(CharacterClass.Assassin, 22, 37, 14, 48, 40, 18, 20, 35);

            AssetDatabase.SaveAssets();
        }

        private static void GenerateClass(CharacterClass charClass, int hp, int str, int def, int agi, int dex, int intel, int mana, int sta)
        {
            string assetPath = $"Assets/Resources/CharacterClasses/{charClass}.asset";
            if (AssetDatabase.LoadAssetAtPath<CharacterClassDefinition>(assetPath) != null)
            {
                return;
            }

            var asset = ScriptableObject.CreateInstance<CharacterClassDefinition>();
            asset.characterClass = charClass;
            asset.levelOneStats = new CharacterStatBlock
            {
                hp = hp,
                strength = str,
                defense = def,
                agility = agi,
                dexterity = dex,
                intelligence = intel,
                mana = mana,
                stamina = sta
            };
            asset.growth = new CharacterGrowth
            {
                hpPerLevel = hp / 10 + 1,
                strengthPerLevel = str / 10 + 1,
                defensePerLevel = def / 10 + 1,
                agilityPerLevel = agi / 10 + 1,
                dexterityPerLevel = dex / 10 + 1,
                intelligencePerLevel = intel / 10 + 1,
                manaPerLevel = mana / 10 + 1,
                staminaPerLevel = sta / 10 + 1
            };

            AssetDatabase.CreateAsset(asset, assetPath);
        }
    }
}
