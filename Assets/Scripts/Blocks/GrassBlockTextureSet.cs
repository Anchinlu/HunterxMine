using UnityEngine;

namespace MineCraftUnity.Blocks
{
    /// <summary>
    /// MC ref: assets/minecraft/models/block/grass_block.json
    /// top = grass_block_top (tinted), bottom = dirt, side = grass_block_side (no tint), SideOverlay = mask × GrassTint.
    /// </summary>
    [CreateAssetMenu(fileName = "GrassBlockTextureSet", menuName = "MineCraft/Blocks/Grass Block Textures")]
    public sealed class GrassBlockTextureSet : ScriptableObject
    {
        [Header("Textures (from minecraft-assets-26.2)")]
        public Texture2D Top;
        public Texture2D Bottom;
        public Texture2D Side;
        public Texture2D SideOverlay;

        [Header("Biome tint — MC plains grass #91BD59 (tintindex 0 on top only)")]
        public Color GrassTint = new(145f / 255f, 189f / 255f, 89f / 255f, 1f);
    }
}
