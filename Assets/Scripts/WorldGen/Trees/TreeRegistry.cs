using MineCraftUnity.Blocks;
using MineCraftUnity.World;

namespace MineCraftUnity.WorldGen.Trees
{
    public static class TreeRegistry
    {
        private static readonly TreeFeature _treeFeature = new TreeFeature();

        private static readonly TreeConfiguration _oakConfig = new TreeConfiguration(
            new StraightTrunkPlacer(4, 2, 0),
            new BlobFoliagePlacer(2, 0, 3),
            BlockId.OakLog,
            BlockId.OakLeaves
        );

        private static readonly TreeConfiguration _birchConfig = new TreeConfiguration(
            new StraightTrunkPlacer(5, 2, 0),
            new BlobFoliagePlacer(2, 0, 3),
            BlockId.BirchLog,
            BlockId.BirchLeaves
        );

        private static readonly TreeConfiguration _spruceConfig = new TreeConfiguration(
            new StraightTrunkPlacer(5, 2, 1),
            new SpruceFoliagePlacer(2, 0, 2, 3),
            BlockId.SpruceLog,
            BlockId.SpruceLeaves
        );

        private static readonly TreeConfiguration _megaSpruceConfig = new TreeConfiguration(
            new GiantTrunkPlacer(13, 2, 14),
            new MegaJungleFoliagePlacer(2, 0, 2),
            BlockId.SpruceLog,
            BlockId.SpruceLeaves
        );

        private static readonly TreeConfiguration _megaPineConfig = new TreeConfiguration(
            new GiantTrunkPlacer(13, 2, 14),
            new MegaPineFoliagePlacer(1, 0, 3, 7),
            BlockId.SpruceLog,
            BlockId.SpruceLeaves
        );

        private static readonly TreeConfiguration _acaciaConfig = new TreeConfiguration(
            new ForkingTrunkPlacer(5, 2, 2),
            new AcaciaFoliagePlacer(2, 0),
            BlockId.AcaciaLog,
            BlockId.AcaciaLeaves
        );

        private static readonly TreeConfiguration _darkOakConfig = new TreeConfiguration(
            new DarkOakTrunkPlacer(6, 2, 1),
            new DarkOakFoliagePlacer(0, 0),
            BlockId.DarkOakLog,
            BlockId.DarkOakLeaves
        );

        private static readonly TreeConfiguration _cherryConfig = new TreeConfiguration(
            new CherryTrunkPlacer(7, 1, 0, 1, 3, 2, 4, -4, -3, -1, 0),
            new CherryFoliagePlacer(4, 0, 0.25f, 0.16f, 0.08f),
            BlockId.CherryLog,
            BlockId.CherryLeaves
        );

        private static readonly TreeConfiguration _jungleConfig = new TreeConfiguration(
            new StraightTrunkPlacer(4, 8, 0),
            new BlobFoliagePlacer(2, 0, 3),
            BlockId.JungleLog,
            BlockId.JungleLeaves
        );

        private static readonly TreeConfiguration _megaJungleConfig = new TreeConfiguration(
            new MegaJungleTrunkPlacer(10, 2, 19),
            new MegaJungleFoliagePlacer(2, 0, 2),
            BlockId.JungleLog,
            BlockId.JungleLeaves
        );

        private static readonly TreeConfiguration _mangroveConfig = new TreeConfiguration(
            new MangroveRootPlacer(2, 1, 4),
            new RandomSpreadFoliagePlacer(3, 0, 2, 3),
            BlockId.MangroveLog,
            BlockId.MangroveLeaves
        );

        private static readonly TreeConfiguration _paleOakConfig = new TreeConfiguration(
            new DarkOakTrunkPlacer(6, 2, 1),
            new DarkOakFoliagePlacer(0, 0),
            BlockId.PaleOakLog,
            BlockId.PaleOakLeaves
        );

        public static TreeFeature GetTreeFeature(BiomeId biome)
        {
            return _treeFeature;
        }

        public static TreeConfiguration GetConfiguration(BiomeId biome)
        {
            switch (biome)
            {
                case BiomeId.BirchForest:
                case BiomeId.OldGrowthBirchForest:
                    return _birchConfig;
                
                case BiomeId.Taiga:
                case BiomeId.SnowyTaiga:
                case BiomeId.Grove:
                    return _spruceConfig;

                case BiomeId.OldGrowthSpruceTaiga:
                    return _megaSpruceConfig;

                case BiomeId.OldGrowthPineTaiga:
                    return _megaPineConfig;

                case BiomeId.Savanna:
                case BiomeId.SavannaPlateau:
                case BiomeId.WindsweptSavanna:
                    return _acaciaConfig;

                case BiomeId.DarkForest:
                    return _darkOakConfig;

                case BiomeId.CherryGrove:
                    return _cherryConfig;

                case BiomeId.Jungle:
                case BiomeId.SparseJungle:
                    return _jungleConfig;

                case BiomeId.BambooJungle:
                    return _megaJungleConfig; // Map BambooJungle to MegaJungle as a proxy

                case BiomeId.MangroveSwamp:
                    return _mangroveConfig;

                case BiomeId.PaleGarden:
                    return _paleOakConfig;

                default:
                    return _oakConfig;
            }
        }
    }
}
