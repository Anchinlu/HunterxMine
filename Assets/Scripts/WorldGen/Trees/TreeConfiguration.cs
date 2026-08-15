using MineCraftUnity.Blocks;

namespace MineCraftUnity.WorldGen.Trees
{
    public class TreeConfiguration
    {
        public TrunkPlacer TrunkPlacer { get; }
        public FoliagePlacer FoliagePlacer { get; }
        public BlockId TrunkProvider { get; }
        public BlockId FoliageProvider { get; }

        public TreeConfiguration(
            TrunkPlacer trunkPlacer,
            FoliagePlacer foliagePlacer,
            BlockId trunkProvider,
            BlockId foliageProvider)
        {
            TrunkPlacer = trunkPlacer;
            FoliagePlacer = foliagePlacer;
            TrunkProvider = trunkProvider;
            FoliageProvider = foliageProvider;
        }
    }
}
