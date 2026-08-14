using UnityEditor;
using UnityEngine;

namespace MineCraftUnity.Editor
{
    public class TextureImporterSetup : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath.Contains("Minecraft/Blocks"))
            {
                TextureImporter importer = (TextureImporter)assetImporter;
                if (importer.filterMode != FilterMode.Point || importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = true; // Optional: Minecraft style sometimes disables this, but usually good for chunks with proper filtering
                }
            }
            else if (assetPath.Contains("Minecraft/Colormap"))
            {
                TextureImporter importer = (TextureImporter)assetImporter;
                if (importer.filterMode != FilterMode.Point || !importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false;
                    importer.isReadable = true;
                }
            }
        }
    }
}
