using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using MineCraftUnity.WorldGen.Synth;
using UnityEngine;

namespace MineCraftUnity.WorldGen.Noise
{
    /// <summary>
    /// Loads MC 26.2 worldgen/noise/*.json definitions and builds NormalNoise instances.
    /// </summary>
    public static class NoiseRegistry
    {
        private static readonly Dictionary<string, NoiseParameters> ParametersByName = new(StringComparer.Ordinal);
        private static bool _initialized;

        public static IReadOnlyDictionary<string, NoiseParameters> AllParameters
        {
            get
            {
                EnsureInitialized();
                return ParametersByName;
            }
        }

        public static void Initialize() => LoadAll();

        public static bool TryGetParameters(string name, out NoiseParameters parameters)
        {
            EnsureInitialized();
            return ParametersByName.TryGetValue(name, out parameters);
        }

        public static NoiseParameters GetParameters(string name)
        {
            if (!TryGetParameters(name, out NoiseParameters parameters))
            {
                throw new KeyNotFoundException($"Noise definition not found: {name}");
            }

            return parameters;
        }

        public static NormalNoise CreateNormalNoise(string name, IRandomSource random) =>
            NormalNoise.Create(random, GetParameters(name));

        public static NormalNoise CreateNormalNoise(string name, long seed) =>
            CreateNormalNoise(name, new XoroshiroRandomSource(seed));

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                LoadAll();
            }
        }

        private static void LoadAll()
        {
            ParametersByName.Clear();

            if (WorldGenDataPaths.UseResources)
            {
                LoadFromResources();
                _initialized = true;
                return;
            }

            string noiseDirectory = WorldGenDataPaths.NoiseDirectory;
            if (!Directory.Exists(noiseDirectory))
            {
                Debug.LogError($"NoiseRegistry: directory not found: {noiseDirectory}");
                _initialized = true;
                return;
            }

            foreach (string filePath in Directory.EnumerateFiles(noiseDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileNameWithoutExtension(filePath);
                if (name.StartsWith("_", StringComparison.Ordinal))
                {
                    continue;
                }

                try
                {
                    string json = File.ReadAllText(filePath);
                    if (!TryParseNoiseJson(json, out int firstOctave, out double[] amplitudes))
                    {
                        Debug.LogWarning($"NoiseRegistry: skipping invalid noise file {filePath}");
                        continue;
                    }

                    ParametersByName[name] = new NoiseParameters(firstOctave, amplitudes);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"NoiseRegistry: failed to load {filePath}: {ex.Message}");
                }
            }

            _initialized = true;
            if (ParametersByName.Count == 0)
            {
                Debug.LogError($"NoiseRegistry: loaded 0 noise definitions from {noiseDirectory}");
            }
            else
            {
                Debug.Log($"NoiseRegistry: loaded {ParametersByName.Count} overworld noise definitions (top-level, expected 61)");
            }
        }

        private static void LoadFromResources()
        {
            var assets = UnityEngine.Resources.LoadAll<UnityEngine.TextAsset>(
                $"{WorldGenDataPaths.ResourceRoot}/noise");

            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("NoiseRegistry: no noise TextAssets found in Resources.");
                return;
            }

            foreach (var asset in assets)
            {
                if (asset.name.StartsWith("_", StringComparison.Ordinal))
                {
                    continue;
                }

                try
                {
                    if (!TryParseNoiseJson(asset.text, out int firstOctave, out double[] amplitudes))
                    {
                        Debug.LogWarning($"NoiseRegistry: skipping invalid noise resource {asset.name}");
                        continue;
                    }

                    ParametersByName[asset.name] = new NoiseParameters(firstOctave, amplitudes);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"NoiseRegistry: failed to parse {asset.name}: {ex.Message}");
                }
            }

            if (ParametersByName.Count == 0)
            {
                Debug.LogError("NoiseRegistry: loaded 0 noise definitions from Resources.");
            }
            else
            {
                Debug.Log($"NoiseRegistry: loaded {ParametersByName.Count} noise definitions from Resources.");
            }
        }

        private static bool TryParseNoiseJson(string json, out int firstOctave, out double[] amplitudes)
        {
            firstOctave = 0;
            amplitudes = Array.Empty<double>();

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            var octaveMatch = Regex.Match(json, "\"firstOctave\"\\s*:\\s*(-?\\d+)", RegexOptions.CultureInvariant);
            if (!octaveMatch.Success || !int.TryParse(octaveMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out firstOctave))
            {
                return false;
            }

            var amplitudesMatch = Regex.Match(json, "\"amplitudes\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline | RegexOptions.CultureInvariant);
            if (!amplitudesMatch.Success)
            {
                return false;
            }

            var values = new List<double>();
            foreach (Match valueMatch in Regex.Matches(amplitudesMatch.Groups[1].Value, "-?\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?", RegexOptions.CultureInvariant))
            {
                if (double.TryParse(valueMatch.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double amplitude))
                {
                    values.Add(amplitude);
                }
            }

            if (values.Count == 0)
            {
                return false;
            }

            amplitudes = values.ToArray();
            return true;
        }
    }
}
