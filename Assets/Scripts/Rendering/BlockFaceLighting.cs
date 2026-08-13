using MineCraftUnity.Blocks;
using UnityEngine;

namespace MineCraftUnity.Rendering
{
    /// <summary>
    /// MC ref: ModelBlockRenderer face brightness — top brightest, sides/east-west darker.
    /// </summary>
    public static class BlockFaceLighting
    {
        public static float GetShade(BlockFace face) =>
            face switch
            {
                BlockFace.Up => 1f,
                BlockFace.Down => 0.5f,
                BlockFace.North or BlockFace.South => 0.8f,
                _ => 0.6f
            };

        public static Color32 ApplyShade(Color32 color, BlockFace face)
        {
            var shade = GetShade(face);
            return new Color32(
                (byte)(color.r * shade),
                (byte)(color.g * shade),
                (byte)(color.b * shade),
                color.a);
        }

        public static Color32 GetShadeColor(BlockFace face)
        {
            var shade = GetShade(face);
            var channel = (byte)(shade * 255f);
            return new Color32(channel, channel, channel, 255);
        }
    }
}
