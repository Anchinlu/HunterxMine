using System.Collections.Generic;
using MineCraftUnity.World;
using UnityEngine;

namespace MineCraftUnity.Rendering
{
    public static class CelestialMaterialLibrary
    {
        private const string ResourceRoot = "Environment/Celestial";

        private static Material _sunMaterial;
        private static Material _sunriseMaterial;
        private static Material _darkDiscMaterial;
        private static readonly Dictionary<MoonPhase, Material> MoonMaterials = new();

        public static Material SunMaterial => EnsureSunMaterial();
        public static Material SunriseMaterial => EnsureSunriseMaterial();
        public static Material DarkDiscMaterial => EnsureDarkDiscMaterial();

        public static Material GetMoonMaterial(MoonPhase phase)
        {
            if (MoonMaterials.TryGetValue(phase, out var cached))
            {
                return cached;
            }

            var fileName = phase.TextureFileName();
            var material = CreateMaterial($"moon/{fileName}", $"Mat_Moon_{fileName}");
            MoonMaterials[phase] = material;
            return material;
        }

        public static Material EnsureSunMaterial()
        {
            if (_sunMaterial != null)
            {
                return _sunMaterial;
            }

            _sunMaterial = CreateMaterial("sun", "Mat_SunBillboard");
            return _sunMaterial;
        }

        private static Material EnsureSunriseMaterial()
        {
            if (_sunriseMaterial != null)
            {
                return _sunriseMaterial;
            }

            var shader = Shader.Find("MineCraft/CelestialSunrise")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            _sunriseMaterial = new Material(shader) { name = "Mat_SunriseFan" };
            _sunriseMaterial.SetColor("_Color", Color.white);
            return _sunriseMaterial;
        }

        private static Material EnsureDarkDiscMaterial()
        {
            if (_darkDiscMaterial != null)
            {
                return _darkDiscMaterial;
            }

            var shader = Shader.Find("MineCraft/CelestialDarkDisc")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            _darkDiscMaterial = new Material(shader) { name = "Mat_SkyDarkDisc" };
            _darkDiscMaterial.SetColor("_Color", new Color(0.02f, 0.03f, 0.08f, 1f));
            return _darkDiscMaterial;
        }

        private static Material CreateMaterial(string relativePath, string matName)
        {
            var shader = Shader.Find("MineCraft/CelestialBillboard")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader) { name = matName };

            var texture = LoadTexture(relativePath);
            if (texture != null && material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_Color", Color.white);
            }
            else
            {
                Debug.LogWarning($"[CelestialMaterialLibrary] Texture missing: Resources/{ResourceRoot}/{relativePath}");
            }

            return material;
        }

        private static Texture2D LoadTexture(string relativePath)
        {
            var resourcePath = $"{ResourceRoot}/{relativePath}";
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null)
            {
                Debug.LogWarning($"[CelestialMaterialLibrary] Texture not found at Resources/{resourcePath}");
            }

            return tex;
        }
    }
}
