#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MineCraftUnity.World;

namespace MineCraftUnity.Tests
{
    public static class BiomeColorTest
    {
        [MenuItem("Minecraft/Test Biome Colors")]
        public static void TestColors()
        {
            BiomeRegistry.EnsureLoaded();

            BiomeId[] testBiomes = {
                BiomeId.Plains,
                BiomeId.Jungle,
                BiomeId.Taiga,
                BiomeId.Swamp,
                BiomeId.CherryGrove,
                BiomeId.Desert,
                BiomeId.DarkForest
            };

            Debug.Log("--- BIOME COLOR TEST ---");
            foreach (var id in testBiomes)
            {
                var grass = BiomeRegistry.GetGrassTint(id);
                var foliage = BiomeRegistry.GetFoliageTint(id);
                
                string grassHex = ColorUtility.ToHtmlStringRGB(grass);
                string foliageHex = ColorUtility.ToHtmlStringRGB(foliage);

                Debug.Log($"[{id}] Grass: #{grassHex} | Foliage: #{foliageHex}");
            }
            Debug.Log("--- END OF TEST ---");
        }
    }
}
#endif
