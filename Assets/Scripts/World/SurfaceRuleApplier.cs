using MineCraftUnity.Blocks;

using MineCraftUnity.Core;



namespace MineCraftUnity.World

{

    /// <summary>

    /// MC ref: SurfaceRuleData.overworld — ON_FLOOR / UNDER_FLOOR / waterStartCheck(-6).

    /// </summary>

    public static class SurfaceRuleApplier

    {

        /// <summary>MC waterStartCheck(-6, -1) — shallow water if depth &lt;= 6.</summary>

        public const int DeepWaterThreshold = 6;



        public const int BeachMaxBelowSea = 1;

        public const int BeachMaxAboveSea = 1;



        public readonly struct ColumnContext

        {

            public readonly int WorldX;

            public readonly int WorldZ;

            public readonly int SurfaceHeight;

            public readonly BiomeId Biome;



            public ColumnContext(int worldX, int worldZ, int surfaceHeight, BiomeId biome)

            {

                WorldX = worldX;

                WorldZ = worldZ;

                SurfaceHeight = surfaceHeight;

                Biome = biome;

            }



            public int WaterDepthAboveFloor =>

                SurfaceHeight < WorldConstants.SeaLevel

                    ? WorldConstants.SeaLevel - SurfaceHeight

                    : 0;



            public bool IsUnderwater => WaterDepthAboveFloor > 0;



            /// <summary>MC notUnderDeepWater.</summary>

            public bool IsShallowUnderwater =>

                WaterDepthAboveFloor > 0 && WaterDepthAboveFloor <= DeepWaterThreshold;

        }



        /// <summary>

        /// MC: shallow underwater applies UNDER_FLOOR dirt; deep ocean only ON_FLOOR (top block).

        /// </summary>

        public static int GetSurfaceLayerStart(in ColumnContext context, int topSolid, int bedrockTop)

        {

            if (!context.IsUnderwater || context.IsShallowUnderwater)

            {

                return System.Math.Max(bedrockTop + 1, topSolid - WorldConstants.DirtDepth + 1);

            }



            return topSolid;

        }



        public static BlockId GetBlockForColumn(int worldY, in ColumnContext context)

        {

            if (worldY <= WorldConstants.MinY + WorldConstants.BedrockLayers - 1)

            {

                return BlockId.Bedrock;

            }



            var surfaceHeight = context.SurfaceHeight;

            if (worldY > surfaceHeight)

            {

                return worldY <= WorldConstants.SeaLevel ? BlockId.Water : BlockId.Air;

            }



            if (worldY == surfaceHeight)

            {

                return GetTopSurfaceBlock(in context);

            }



            if (worldY > surfaceHeight - WorldConstants.DirtDepth)

            {

                return GetSubsurfaceBlock(in context);

            }



            return BlockId.Stone;

        }



        /// <summary>MC ON_FLOOR + biomeSurfaceRule.</summary>

        private static BlockId GetTopSurfaceBlock(in ColumnContext context)

        {

            if (context.SurfaceHeight > WorldConstants.SeaLevel)

            {

                return BiomeRegistry.GetTopSurfaceBlock(context.Biome, isUnderwater: false, context.IsShallowUnderwater);

            }



            if (IsCoastalBeach(in context))

            {

                return BlockId.Sand;

            }



            return BiomeRegistry.GetTopSurfaceBlock(context.Biome, context.IsUnderwater, context.IsShallowUnderwater);

        }



        /// <summary>MC UNDER_FLOOR → biomeUnderSurfaceRule when notUnderDeepWater.</summary>

        private static BlockId GetSubsurfaceBlock(in ColumnContext context)

        {

            if (!context.IsUnderwater)

            {

                return BiomeRegistry.GetSubsurfaceBlock(context.Biome, isUnderwater: false, context.IsShallowUnderwater);

            }



            if (!context.IsShallowUnderwater)

            {

                return BlockId.Stone;

            }



            if (IsCoastalBeach(in context))

            {

                return BlockId.Sand;

            }



            return BiomeRegistry.GetSubsurfaceBlock(context.Biome, context.IsUnderwater, context.IsShallowUnderwater);

        }



        private static bool IsCoastalBeach(in ColumnContext context)

        {

            var surfaceHeight = context.SurfaceHeight;

            if (surfaceHeight < WorldConstants.SeaLevel - BeachMaxBelowSea ||

                surfaceHeight > WorldConstants.SeaLevel + BeachMaxAboveSea)

            {

                return false;

            }



            return context.Biome is BiomeId.Beach or BiomeId.SnowyBeach;

        }

    }

}

