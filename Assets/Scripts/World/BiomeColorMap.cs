using MineCraftUnity.WorldGen;
using UnityEngine;

namespace MineCraftUnity.World
{
    public static class BiomeColorMap
    {
        private static Color32[] _grassMap;
        private static int _grassWidth;
        private static int _grassHeight;

        private static Color32[] _foliageMap;
        private static int _foliageWidth;
        private static int _foliageHeight;

        private static bool _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized) return;

            LoadMap("Minecraft/Colormap/grass", out _grassMap, out _grassWidth, out _grassHeight);
            LoadMap("Minecraft/Colormap/foliage", out _foliageMap, out _foliageWidth, out _foliageHeight);
            _initialized = true;
        }

        private static void LoadMap(string path, out Color32[] pixels, out int width, out int height)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                width = tex.width;
                height = tex.height;
                pixels = tex.GetPixels32();
            }
            else
            {
                Debug.LogWarning($"[BiomeColorMap] Failed to load colormap at {path}");
                width = 256;
                height = 256;
                pixels = new Color32[width * height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color32(121, 192, 90, 255); // Default green fallback
                }
            }
        }

        public static Color GetGrassColor(float temperature, float downfall)
        {
            EnsureInitialized();
            return SampleColor(_grassMap, _grassWidth, _grassHeight, temperature, downfall);
        }

        public static Color GetFoliageColor(float temperature, float downfall)
        {
            EnsureInitialized();
            return SampleColor(_foliageMap, _foliageWidth, _foliageHeight, temperature, downfall);
        }

        private static Color SampleColor(Color32[] map, int width, int height, float temperature, float downfall)
        {
            float adjTemp = Mathf.Clamp01(temperature);
            float adjHumid = Mathf.Clamp01(downfall * adjTemp);
            
            // X: 1.0 (hot) -> 0, 0.0 (cold) -> width-1
            int pixelX = Mathf.FloorToInt((1f - adjTemp) * (width - 1));
            // Y: 1.0 (wet) -> height-1, 0.0 (dry) -> 0 (Unity GetPixels32 reads bottom-up)
            int pixelY = Mathf.FloorToInt(adjHumid * (height - 1));
            
            pixelX = Mathf.Clamp(pixelX, 0, width - 1);
            pixelY = Mathf.Clamp(pixelY, 0, height - 1);
            
            int index = pixelY * width + pixelX;
            return map[index];
        }
    }
}
