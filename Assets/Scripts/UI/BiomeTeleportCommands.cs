using MineCraftUnity.Core;
using MineCraftUnity.Player;
using MineCraftUnity.Rendering;
using MineCraftUnity.World;
using MineCraftUnity.WorldGen;
using UnityEngine;

namespace MineCraftUnity.UI
{
    internal static class BiomeTeleportCommands
    {
        public static bool TryExecute(string[] parts, out string response)
        {
            response = string.Empty;
            if (parts.Length < 3)
            {
                response = "Usage: locate biome <name>  or  tp biome <name>";
                return false;
            }

            if (!string.Equals(parts[1], "biome", System.StringComparison.OrdinalIgnoreCase))
            {
                response = "Usage: locate biome <name>  or  tp biome <name>";
                return false;
            }

            var biomeToken = string.Join(" ", parts, 2, parts.Length - 2);
            if (!BiomeNameParser.TryParse(biomeToken, out var biomeId, out var parseError))
            {
                response = parseError;
                return false;
            }

            var chunkManager = Object.FindFirstObjectByType<ChunkManager>();
            if (chunkManager == null)
            {
                response = "ChunkManager not found.";
                return false;
            }

            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                response = "Player not found.";
                return false;
            }

            if (!(chunkManager.Generator is OverworldGenerator overworldGen))
            {
                response = "Cannot locate biomes in the current world mode.";
                return false;
            }

            var originX = Mathf.FloorToInt(player.transform.position.x);
            var originZ = Mathf.FloorToInt(player.transform.position.z);
            var randomState = overworldGen.RandomState;
            var result = BiomeLocator.Locate(randomState, biomeId, originX, originZ);

            if (!result.Found)
            {
                response =
                    $"Could not find {BiomeRegistry.GetDisplayName(biomeId)} within {BiomeLocator.DefaultMaxRadiusBlocks} blocks.";
                return false;
            }

            player.TeleportToSurfaceWaitLoad(chunkManager, result.BlockX, result.BlockZ);

            var biomeName = BiomeRegistry.GetDisplayName(biomeId);
            response =
                $"Teleported to {biomeName} at {result.BlockX} ~ ~ {result.BlockZ} ({result.DistanceBlocks} blocks away).";
            return true;
        }
    }
}
