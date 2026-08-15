using UnityEngine;

namespace MineCraftUnity.UI
{
    [CreateAssetMenu(fileName = "HudSpriteLibrary", menuName = "MineCraft/HudSpriteLibrary")]
    public class HudSpriteLibrary : ScriptableObject
    {
        public Sprite Crosshair;
        public Sprite Hotbar;
        public Sprite HotbarSelection;
        
        public Sprite HeartContainer;
        public Sprite HeartFull;
        public Sprite HeartHalf;
        
        public Sprite FoodEmpty;
        public Sprite FoodHalf;
        public Sprite FoodFull;
        
        public Sprite ArmorEmpty;
        public Sprite ArmorHalf;
        public Sprite ArmorFull;
        
        public Sprite ExperienceBarBackground;
        public Sprite ExperienceBarProgress;
    }
}
