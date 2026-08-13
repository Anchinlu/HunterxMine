using System.IO;
using UnityEngine;

namespace MineCraftUnity.WorldGen
{
    /// <summary>
    /// Paths to minecraft-assets-26.2 datapack used as MC 26.2 worldgen reference.
    /// </summary>
    public static class WorldGenDataPaths
    {
        private const string AssetsRoot = "Docs/tham khảo/minecraft-assets-26.2/minecraft-assets-26.2/data/minecraft/worldgen";

        public static string NoiseDirectory => Resolve($"{AssetsRoot}/noise");
        public static string DensityFunctionDirectory => Resolve($"{AssetsRoot}/density_function");
        public static string NoiseSettingsOverworld => Resolve($"{AssetsRoot}/noise_settings/overworld.json");
        public static string MultiNoiseOverworld => Resolve($"{AssetsRoot}/multi_noise_biome_source_parameter_list/overworld.json");

        private static string Resolve(string relative)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return Path.GetFullPath(Path.Combine(projectRoot ?? ".", relative));
        }
    }
}
