using UnityEngine;

namespace MineCraftUnity.World
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.biome.Biome — colors loaded from worldgen/biome/*.json.
    /// </summary>
    public sealed class BiomeDefinition
    {
        public BiomeId Id;
        public float Temperature = 0.5f;
        public float Downfall = 0.5f;
        public Color WaterColor = new(0.247f, 0.463f, 0.894f, 1f);
        public Color? GrassColor;
        public Color? FoliageColor;
        public string GrassColorModifier;
        public Color? WaterFogColor;
        public Color? SkyColor;
        public bool HasPrecipitation = true;
    }
}
