using UnityEngine;

namespace MineCraftUnity.Player
{
    [CreateAssetMenu(fileName = "NewCharacterClass", menuName = "MineCraft/Character Class")]
    public class CharacterClassDefinition : ScriptableObject
    {
        public CharacterClass characterClass;
        public CharacterStatBlock levelOneStats;
        public CharacterGrowth growth;
    }
}
