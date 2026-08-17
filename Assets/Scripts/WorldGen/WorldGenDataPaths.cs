using System.IO;
using UnityEngine;

namespace MineCraftUnity.WorldGen
{
    /// <summary>
    /// Paths to minecraft-assets-26.2 datapack used as MC 26.2 worldgen reference.
    /// In Editor: resolves to project-level Docs/... directory (filesystem).
    /// In Build: data is loaded from Assets/Resources/WorldGen/... via Resources.Load.
    /// </summary>
    public static class WorldGenDataPaths
    {
        private const string AssetsRoot = "Docs/tham khảo/minecraft-assets-26.2/minecraft-assets-26.2/data/minecraft/worldgen";

        /// <summary>
        /// Resource sub-path for worldgen data when loaded from Resources.
        /// </summary>
        public const string ResourceRoot = "WorldGen";

        /// <summary>
        /// Whether runtime should load data from Resources instead of the filesystem.
        /// Always true in builds; false in Editor where the Docs directory is available.
        /// </summary>
        public static bool UseResources
        {
            get
            {
#if UNITY_EDITOR
                return false;
#else
                return true;
#endif
            }
        }

        public static string NoiseDirectory => Resolve($"{AssetsRoot}/noise");
        public static string DensityFunctionDirectory => Resolve($"{AssetsRoot}/density_function");
        public static string NoiseSettingsOverworld => Resolve($"{AssetsRoot}/noise_settings/overworld.json");
        public static string MultiNoiseOverworld => Resolve($"{AssetsRoot}/multi_noise_biome_source_parameter_list/overworld.json");
        public static string BiomeDirectory => Resolve($"{AssetsRoot}/biome");

        private static string Resolve(string relative)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return Path.GetFullPath(Path.Combine(projectRoot ?? ".", relative));
        }
    }
}
