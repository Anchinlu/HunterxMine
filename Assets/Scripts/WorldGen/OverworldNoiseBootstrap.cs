using MineCraftUnity.WorldGen.Density;
using MineCraftUnity.WorldGen.Noise;
using MineCraftUnity.WorldGen.Synth;

namespace MineCraftUnity.WorldGen
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.NoiseRouterData — builds overworld NoiseRouter from seed.
    /// </summary>
    public static class OverworldNoiseBootstrap
    {
        public const float GlobalOffset = DensityFunctions.GlobalOffset;
        public const double SurfaceDensityThreshold = 1.5625;
        public const double CheeseNoiseTarget = -0.703125;
        public const double NoiseZero = 0.390625;
        public const int OverworldCellHeight = 8;

        private const double BlendingFactor = 10.0;
        private const int DensityYAnchorBottom = -64;
        private const int DensityYAnchorTop = 320;
        private const double DensityYBottom = 1.5;
        private const double DensityYTop = -1.5;
        private const double BaseDensityMultiplier = 4.0;

        private static class NoiseNames
        {
            public const string Shift = "offset";
            public const string Continentalness = "continentalness";
            public const string ContinentalnessLarge = "continentalness_large";
            public const string Erosion = "erosion";
            public const string ErosionLarge = "erosion_large";
            public const string Ridge = "ridge";
            public const string Jagged = "jagged";
            public const string Temperature = "temperature";
            public const string TemperatureLarge = "temperature_large";
            public const string Vegetation = "vegetation";
            public const string VegetationLarge = "vegetation_large";
            public const string AquiferBarrier = "aquifer_barrier";
            public const string AquiferFluidLevelFloodedness = "aquifer_fluid_level_floodedness";
            public const string AquiferFluidLevelSpread = "aquifer_fluid_level_spread";
            public const string AquiferLava = "aquifer_lava";
            public const string SpaghettiRoughness = "spaghetti_roughness";
            public const string SpaghettiRoughnessModulator = "spaghetti_roughness_modulator";
            public const string Spaghetti3dRarity = "spaghetti_3d_rarity";
            public const string Spaghetti3dThickness = "spaghetti_3d_thickness";
            public const string Spaghetti3d1 = "spaghetti_3d_1";
            public const string Spaghetti3d2 = "spaghetti_3d_2";
            public const string CaveEntrance = "cave_entrance";
            public const string CaveLayer = "cave_layer";
            public const string CaveCheese = "cave_cheese";
            public const string Spaghetti2d = "spaghetti_2d";
            public const string Spaghetti2dModulator = "spaghetti_2d_modulator";
            public const string Spaghetti2dElevation = "spaghetti_2d_elevation";
            public const string Spaghetti2dThickness = "spaghetti_2d_thickness";
            public const string Pillar = "pillar";
            public const string PillarRareness = "pillar_rareness";
            public const string PillarThickness = "pillar_thickness";
            public const string Noodle = "noodle";
            public const string NoodleThickness = "noodle_thickness";
            public const string NoodleRidgeA = "noodle_ridge_a";
            public const string NoodleRidgeB = "noodle_ridge_b";
        }

        public static RandomState CreateRandomState(long seed) =>
            new((int)seed, CreateOverworldRouter(seed));

        public static NoiseRouter CreateOverworldRouter(long seed) => CreateOverworldRouter(seed, largeBiomes: false, amplified: false);

        public static NoiseRouter CreateOverworldRouter(long seed, bool largeBiomes, bool amplified)
        {
            var builder = new DensityFunctionGraph(seed);
            builder.RegisterSharedFunctions();
            return builder.BuildOverworldRouter(largeBiomes, amplified);
        }

        private sealed class DensityFunctionGraph
        {
            private readonly IPositionalRandomFactory _random;
            private readonly BlendedNoise _base3dNoiseOverworld;

            private IDensityFunction _shiftX;
            private IDensityFunction _shiftZ;
            private IDensityFunction _y;
            private IDensityFunction _continents;
            private IDensityFunction _continentsLarge;
            private IDensityFunction _erosion;
            private IDensityFunction _erosionLarge;
            private IDensityFunction _ridges;
            private IDensityFunction _ridgesFolded;
            private IDensityFunction _offset;
            private IDensityFunction _offsetLarge;
            private IDensityFunction _offsetAmplified;
            private IDensityFunction _factor;
            private IDensityFunction _factorLarge;
            private IDensityFunction _factorAmplified;
            private IDensityFunction _depth;
            private IDensityFunction _depthLarge;
            private IDensityFunction _depthAmplified;
            private IDensityFunction _slopedCheese;
            private IDensityFunction _slopedCheeseLarge;
            private IDensityFunction _slopedCheeseAmplified;
            private IDensityFunction _spaghettiRoughnessFunction;
            private IDensityFunction _spaghetti2dThicknessModulator;
            private IDensityFunction _spaghetti2d;
            private IDensityFunction _entrances;
            private IDensityFunction _noodle;
            private IDensityFunction _pillars;

            public DensityFunctionGraph(long seed)
            {
                var rootRandom = new XoroshiroRandomSource(seed);
                _random = rootRandom.ForkPositional();
                _base3dNoiseOverworld = new BlendedNoise(
                    _random.FromHashOf("minecraft:terrain"),
                    0.25,
                    0.125,
                    80.0,
                    160.0,
                    8.0);
            }

            private DensityFunctions.NoiseHolder CreateNoise(string name) =>
                new(NormalNoise.Create(_random.FromHashOf("minecraft:" + name), NoiseRegistry.GetParameters(name)));

            public void RegisterSharedFunctions()
            {
                _y = DensityFunctions.YClampedGradient(
                    DensityYAnchorBottom * 2,
                    DensityYAnchorTop * 2,
                    DensityYAnchorBottom * 2,
                    DensityYAnchorTop * 2);

                DensityFunctions.NoiseHolder shiftNoise = CreateNoise(NoiseNames.Shift);
                _shiftX = DensityFunctions.FlatCache(DensityFunctions.Cache2d(DensityFunctions.ShiftA(shiftNoise)));
                _shiftZ = DensityFunctions.FlatCache(DensityFunctions.Cache2d(DensityFunctions.ShiftB(shiftNoise)));

                _continents = DensityFunctions.FlatCache(
                    DensityFunctions.ShiftedNoise2d(_shiftX, _shiftZ, 0.25, CreateNoise(NoiseNames.Continentalness)));
                _continentsLarge = DensityFunctions.FlatCache(
                    DensityFunctions.ShiftedNoise2d(_shiftX, _shiftZ, 0.25, CreateNoise(NoiseNames.ContinentalnessLarge)));
                _erosion = DensityFunctions.FlatCache(
                    DensityFunctions.ShiftedNoise2d(_shiftX, _shiftZ, 0.25, CreateNoise(NoiseNames.Erosion)));
                _erosionLarge = DensityFunctions.FlatCache(
                    DensityFunctions.ShiftedNoise2d(_shiftX, _shiftZ, 0.25, CreateNoise(NoiseNames.ErosionLarge)));
                _ridges = DensityFunctions.FlatCache(
                    DensityFunctions.ShiftedNoise2d(_shiftX, _shiftZ, 0.25, CreateNoise(NoiseNames.Ridge)));
                _ridgesFolded = PeaksAndValleys(_ridges);

                DensityFunctions.NoiseHolder jaggedNoise = CreateNoise(NoiseNames.Jagged);
                IDensityFunction jaggedNoiseFunction = DensityFunctions.Noise(jaggedNoise, 1500.0, 0.0);

                RegisterTerrainNoisesInternal(jaggedNoiseFunction, _continents, _erosion, amplified: false, out _offset, out _factor, out _depth, out _, out _slopedCheese);
                RegisterTerrainNoisesInternal(jaggedNoiseFunction, _continentsLarge, _erosionLarge, amplified: false, out _offsetLarge, out _factorLarge, out _depthLarge, out _, out _slopedCheeseLarge);
                RegisterTerrainNoisesInternal(jaggedNoiseFunction, _continents, _erosion, amplified: true, out _offsetAmplified, out _factorAmplified, out _depthAmplified, out _, out _slopedCheeseAmplified);

                _spaghettiRoughnessFunction = SpaghettiRoughnessFunction();
                _spaghetti2dThicknessModulator = DensityFunctions.CacheOnce(
                    DensityFunctions.MappedNoise(CreateNoise(NoiseNames.Spaghetti2dThickness), 2.0, 1.0, -0.6, -1.3));
                _spaghetti2d = Spaghetti2d();
                _entrances = Entrances();
                _noodle = Noodle();
                _pillars = Pillars();
            }

            private void RegisterTerrainNoisesInternal(
                IDensityFunction jaggedNoise,
                IDensityFunction continentsFunction,
                IDensityFunction erosionFunction,
                bool amplified,
                out IDensityFunction offset,
                out IDensityFunction factor,
                out IDensityFunction depth,
                out IDensityFunction jaggedness,
                out IDensityFunction slopedCheese)
            {
                var continents = new DensityFunctions.SplineCoordinate(continentsFunction);
                var erosion = new DensityFunctions.SplineCoordinate(erosionFunction);
                var weirdness = new DensityFunctions.SplineCoordinate(_ridges);
                var ridges = new DensityFunctions.SplineCoordinate(_ridgesFolded);

                offset = SplineWithBlending(
                    DensityFunctions.Add(
                        DensityFunctions.Constant(GlobalOffset),
                        DensityFunctions.Spline(TerrainProvider.OverworldOffset(continents, erosion, ridges, amplified))),
                    DensityFunctions.BlendOffset());

                factor = SplineWithBlending(
                    DensityFunctions.Spline(TerrainProvider.OverworldFactor(continents, erosion, weirdness, ridges, amplified)),
                    DensityFunctions.Constant(BlendingFactor));

                depth = OffsetToDepth(offset);

                IDensityFunction unscaledJaggedness = SplineWithBlending(
                    DensityFunctions.Spline(TerrainProvider.OverworldJaggedness(continents, erosion, weirdness, ridges, amplified)),
                    DensityFunctions.Zero());
                jaggedness = DensityFunctions.FlatCache(DensityFunctions.Mul(unscaledJaggedness, jaggedNoise.HalfNegative()));

                IDensityFunction initialDensity = NoiseGradientDensity(factor, DensityFunctions.Add(depth, jaggedness));
                slopedCheese = DensityFunctions.Add(initialDensity, _base3dNoiseOverworld);
            }

            public NoiseRouter BuildOverworldRouter(bool largeBiomes, bool amplified)
            {
                IDensityFunction offset = largeBiomes ? _offsetLarge : amplified ? _offsetAmplified : _offset;
                IDensityFunction factor = largeBiomes ? _factorLarge : amplified ? _factorAmplified : _factor;
                IDensityFunction depth = largeBiomes ? _depthLarge : amplified ? _depthAmplified : _depth;
                IDensityFunction slopedCheeseSource = largeBiomes ? _slopedCheeseLarge : amplified ? _slopedCheeseAmplified : _slopedCheese;

                IDensityFunction barrierNoise = DensityFunctions.Noise(CreateNoise(NoiseNames.AquiferBarrier), 0.5);
                IDensityFunction fluidLevelFloodednessNoise = DensityFunctions.Noise(CreateNoise(NoiseNames.AquiferFluidLevelFloodedness), 0.67);
                IDensityFunction fluidLevelSpreadNoise = DensityFunctions.Noise(CreateNoise(NoiseNames.AquiferFluidLevelSpread), 0.7142857142857143);
                IDensityFunction lavaNoise = DensityFunctions.Noise(CreateNoise(NoiseNames.AquiferLava));
                IDensityFunction temperature = DensityFunctions.ShiftedNoise2d(
                    _shiftX,
                    _shiftZ,
                    0.25,
                    CreateNoise(largeBiomes ? NoiseNames.TemperatureLarge : NoiseNames.Temperature));
                IDensityFunction vegetation = DensityFunctions.ShiftedNoise2d(
                    _shiftX,
                    _shiftZ,
                    0.25,
                    CreateNoise(largeBiomes ? NoiseNames.VegetationLarge : NoiseNames.Vegetation));

                IDensityFunction preliminarySurfaceLevel = PreliminarySurfaceLevel(offset, factor, amplified);
                IDensityFunction slopedCheese = DensityFunctions.CacheOnce(slopedCheeseSource);
                IDensityFunction surfaceWithEntrances = DensityFunctions.Min(
                    slopedCheese,
                    DensityFunctions.Mul(DensityFunctions.Constant(5.0), _entrances));
                IDensityFunction caves = DensityFunctions.RangeChoice(
                    slopedCheese,
                    -1000000.0,
                    SurfaceDensityThreshold,
                    surfaceWithEntrances,
                    Underground(slopedCheese));
                IDensityFunction fullNoise = DensityFunctions.Min(PostProcess(SlideOverworld(amplified, caves)), _noodle);

                return new NoiseRouter
                {
                    Barrier = barrierNoise,
                    FluidLevelFloodedness = fluidLevelFloodednessNoise,
                    FluidLevelSpread = fluidLevelSpreadNoise,
                    Lava = lavaNoise,
                    Temperature = temperature,
                    Vegetation = vegetation,
                    Continents = largeBiomes ? _continentsLarge : _continents,
                    Erosion = largeBiomes ? _erosionLarge : _erosion,
                    Depth = depth,
                    Ridges = _ridges,
                    PreliminarySurfaceLevel = preliminarySurfaceLevel,
                    SlopedCheese = slopedCheese,
                    FinalDensity = fullNoise
                };
            }

            private static IDensityFunction PeaksAndValleys(IDensityFunction weirdness) =>
                DensityFunctions.Mul(
                    DensityFunctions.Add(
                        DensityFunctions.Add(weirdness.Abs(), DensityFunctions.Constant(-0.6666666666666666)).Abs(),
                        DensityFunctions.Constant(-0.3333333333333333)),
                    DensityFunctions.Constant(-3.0));

            private static IDensityFunction OffsetToDepth(IDensityFunction offset) =>
                DensityFunctions.Add(
                    DensityFunctions.YClampedGradient(DensityYAnchorBottom, DensityYAnchorTop, DensityYBottom, DensityYTop),
                    offset);

            private static IDensityFunction SplineWithBlending(IDensityFunction spline, IDensityFunction blendingTarget) =>
                DensityFunctions.FlatCache(
                    DensityFunctions.Cache2d(
                        DensityFunctions.Lerp(DensityFunctions.BlendAlpha(), blendingTarget, spline)));

            private static IDensityFunction NoiseGradientDensity(IDensityFunction factor, IDensityFunction depthWithJaggedness) =>
                DensityFunctions.Mul(
                    DensityFunctions.Constant(BaseDensityMultiplier),
                    DensityFunctions.Mul(depthWithJaggedness, factor).QuarterNegative());

            private IDensityFunction SpaghettiRoughnessFunction()
            {
                IDensityFunction spaghettiRoughnessNoise = DensityFunctions.Noise(CreateNoise(NoiseNames.SpaghettiRoughness));
                IDensityFunction spaghettiRoughnessModulator = DensityFunctions.MappedNoise(
                    CreateNoise(NoiseNames.SpaghettiRoughnessModulator), 0.0, -0.1);
                return DensityFunctions.CacheOnce(
                    DensityFunctions.Mul(
                        spaghettiRoughnessModulator,
                        DensityFunctions.Add(spaghettiRoughnessNoise.Abs(), DensityFunctions.Constant(-0.4))));
            }

            private IDensityFunction Entrances()
            {
                IDensityFunction spaghetti3dRarityModulator = DensityFunctions.CacheOnce(
                    DensityFunctions.Noise(CreateNoise(NoiseNames.Spaghetti3dRarity), 2.0, 1.0));
                IDensityFunction spaghetti3dThicknessModulator = DensityFunctions.MappedNoise(
                    CreateNoise(NoiseNames.Spaghetti3dThickness), -0.065, -0.088);
                IDensityFunction spaghetti3dFunction = DensityFunctions.Add(
                        DensityFunctions.Max(
                            QuantizedSpaghettiRarity.WrapRarity3d(spaghetti3dRarityModulator, CreateNoise(NoiseNames.Spaghetti3d1)),
                            QuantizedSpaghettiRarity.WrapRarity3d(spaghetti3dRarityModulator, CreateNoise(NoiseNames.Spaghetti3d2))),
                        spaghetti3dThicknessModulator)
                    .Clamp(-1.0, 1.0);
                IDensityFunction bigEntranceNoiseSource = DensityFunctions.Noise(CreateNoise(NoiseNames.CaveEntrance), 0.75, 0.5);
                IDensityFunction bigEntrancesFunction = DensityFunctions.Add(
                    DensityFunctions.Add(bigEntranceNoiseSource, DensityFunctions.Constant(0.37)),
                    DensityFunctions.YClampedGradient(-10, 30, 0.3, 0.0));
                return DensityFunctions.CacheOnce(
                    DensityFunctions.Min(
                        bigEntrancesFunction,
                        DensityFunctions.Add(_spaghettiRoughnessFunction, spaghetti3dFunction)));
            }

            private IDensityFunction Noodle()
            {
                const int noodleMinY = -60;
                const int noodleMaxY = 320;
                IDensityFunction noodleToggle = YLimitedInterpolatable(
                    _y, DensityFunctions.Noise(CreateNoise(NoiseNames.Noodle), 1.0, 1.0), noodleMinY, noodleMaxY, -1);
                IDensityFunction noodleThickness = YLimitedInterpolatable(
                    _y,
                    DensityFunctions.MappedNoise(CreateNoise(NoiseNames.NoodleThickness), 1.0, 1.0, -0.05, -0.1),
                    noodleMinY,
                    noodleMaxY,
                    0);
                IDensityFunction noodleRidgeA = YLimitedInterpolatable(
                    _y,
                    DensityFunctions.Noise(CreateNoise(NoiseNames.NoodleRidgeA), 2.6666666666666665, 2.6666666666666665),
                    noodleMinY,
                    noodleMaxY,
                    0);
                IDensityFunction noodleRidgeB = YLimitedInterpolatable(
                    _y,
                    DensityFunctions.Noise(CreateNoise(NoiseNames.NoodleRidgeB), 2.6666666666666665, 2.6666666666666665),
                    noodleMinY,
                    noodleMaxY,
                    0);
                IDensityFunction noodleRidged = DensityFunctions.Mul(
                    DensityFunctions.Constant(1.5),
                    DensityFunctions.Max(noodleRidgeA.Abs(), noodleRidgeB.Abs()));
                return DensityFunctions.RangeChoice(
                    noodleToggle,
                    -1000000.0,
                    0.0,
                    DensityFunctions.Constant(64.0),
                    DensityFunctions.Add(noodleThickness, noodleRidged));
            }

            private IDensityFunction Pillars()
            {
                IDensityFunction pillarNoiseSource = DensityFunctions.Noise(CreateNoise(NoiseNames.Pillar), 25.0, 0.3);
                IDensityFunction pillarRarenessModulator = DensityFunctions.MappedNoise(CreateNoise(NoiseNames.PillarRareness), 0.0, -2.0);
                IDensityFunction pillarThicknessModulator = DensityFunctions.MappedNoise(CreateNoise(NoiseNames.PillarThickness), 0.0, 1.1);
                IDensityFunction pillarsWithRareness = DensityFunctions.Add(
                    DensityFunctions.Mul(pillarNoiseSource, DensityFunctions.Constant(2.0)),
                    pillarRarenessModulator);
                return DensityFunctions.CacheOnce(DensityFunctions.Mul(pillarsWithRareness, pillarThicknessModulator.Cube()));
            }

            private IDensityFunction Spaghetti2d()
            {
                IDensityFunction spaghetti2dRarityModulator = DensityFunctions.Noise(CreateNoise(NoiseNames.Spaghetti2dModulator), 2.0, 1.0);
                IDensityFunction spaghetti2dCave = QuantizedSpaghettiRarity.WrapRarity2d(
                    spaghetti2dRarityModulator,
                    CreateNoise(NoiseNames.Spaghetti2d));
                IDensityFunction spaghetti2dElevationModulator = DensityFunctions.MappedNoise(
                    CreateNoise(NoiseNames.Spaghetti2dElevation),
                    0.0,
                    System.Math.Floor(-64 / 8.0),
                    8.0);
                IDensityFunction slopedSpaghetti = DensityFunctions.Add(
                        DensityFunctions.FlatCache(spaghetti2dElevationModulator),
                        DensityFunctions.YClampedGradient(-64, 320, 8.0, -40.0))
                    .Abs();
                IDensityFunction layerRidged = DensityFunctions.Add(slopedSpaghetti, _spaghetti2dThicknessModulator).Cube();
                IDensityFunction caveNoise = DensityFunctions.Add(
                    spaghetti2dCave,
                    DensityFunctions.Mul(DensityFunctions.Constant(0.083), _spaghetti2dThicknessModulator));
                return DensityFunctions.Max(caveNoise, layerRidged).Clamp(-1.0, 1.0);
            }

            private IDensityFunction Underground(IDensityFunction slopedCheese)
            {
                IDensityFunction layerNoiseSource = DensityFunctions.Noise(CreateNoise(NoiseNames.CaveLayer), 8.0);
                IDensityFunction layerizedCavernsFunction = DensityFunctions.Mul(DensityFunctions.Constant(4.0), layerNoiseSource.Square());
                IDensityFunction cheese = DensityFunctions.Noise(CreateNoise(NoiseNames.CaveCheese), 0.6666666666666666);
                IDensityFunction solidifiedCheeseWithTopSlide = DensityFunctions.Add(
                    DensityFunctions.Add(DensityFunctions.Constant(0.27), cheese).Clamp(-1.0, 1.0),
                    DensityFunctions.Add(
                        DensityFunctions.Constant(1.5),
                        DensityFunctions.Mul(DensityFunctions.Constant(-0.64), slopedCheese)).Clamp(0.0, 0.5));
                IDensityFunction baseCaveDensity = DensityFunctions.Add(layerizedCavernsFunction, solidifiedCheeseWithTopSlide);
                IDensityFunction undergroundSubtractions = DensityFunctions.Min(
                    DensityFunctions.Min(baseCaveDensity, _entrances),
                    DensityFunctions.Add(_spaghetti2d, _spaghettiRoughnessFunction));
                IDensityFunction pillars = DensityFunctions.RangeChoice(
                    _pillars,
                    -1000000.0,
                    0.03,
                    DensityFunctions.Constant(-1000000.0),
                    _pillars);
                return DensityFunctions.Max(undergroundSubtractions, pillars);
            }

            private static IDensityFunction PostProcess(IDensityFunction slide) =>
                DensityFunctions.Interpolated(
                    DensityFunctions.Mul(DensityFunctions.BlendDensity(slide), DensityFunctions.Constant(0.64))).Squeeze();

            private static IDensityFunction SlideOverworld(bool isAmplified, IDensityFunction caves) =>
                Slide(caves, -64, 384, isAmplified ? 16 : 80, isAmplified ? 0 : 64, -0.078125, 0, 24, isAmplified ? 0.4 : 0.1171875);

            private static IDensityFunction Slide(
                IDensityFunction caves,
                int minY,
                int height,
                int topStartY,
                int topEndY,
                double topTarget,
                int bottomStartY,
                int bottomEndY,
                double bottomTarget)
            {
                IDensityFunction topFactor = DensityFunctions.YClampedGradient(
                    minY + height - topStartY,
                    minY + height - topEndY,
                    1.0,
                    0.0);
                IDensityFunction noiseValue = DensityFunctions.Lerp(topFactor, topTarget, caves);
                IDensityFunction bottomFactor = DensityFunctions.YClampedGradient(
                    minY + bottomStartY,
                    minY + bottomEndY,
                    0.0,
                    1.0);
                return DensityFunctions.Lerp(bottomFactor, bottomTarget, noiseValue);
            }

            private static IDensityFunction PreliminarySurfaceLevel(IDensityFunction offset, IDensityFunction factor, bool amplified)
            {
                IDensityFunction cachedFactor = DensityFunctions.Cache2d(factor);
                IDensityFunction cachedOffset = DensityFunctions.Cache2d(offset);
                IDensityFunction upperBound = Remap(
                    DensityFunctions.Add(
                        DensityFunctions.Mul(DensityFunctions.Constant(0.2734375), cachedFactor.Invert()),
                        DensityFunctions.Mul(DensityFunctions.Constant(-1.0), cachedOffset)),
                    1.5,
                    -1.5,
                    -64.0,
                    320.0).Clamp(-40.0, 320.0);
                IDensityFunction density = DensityFunctions.Add(
                    SlideOverworld(
                        amplified,
                        DensityFunctions.Add(
                                NoiseGradientDensity(cachedFactor, OffsetToDepth(cachedOffset)),
                                DensityFunctions.Constant(CheeseNoiseTarget))
                            .Clamp(-64.0, 64.0)),
                    DensityFunctions.Constant(-NoiseZero));
                return DensityFunctions.FindTopSurface(density, upperBound, -64, OverworldCellHeight);
            }

            private static IDensityFunction Remap(
                IDensityFunction input,
                double fromMin,
                double fromMax,
                double toMin,
                double toMax)
            {
                double factor = (toMax - toMin) / (fromMax - fromMin);
                double offset = toMin - fromMin * factor;
                return DensityFunctions.Add(DensityFunctions.Mul(input, DensityFunctions.Constant(factor)), DensityFunctions.Constant(offset));
            }

            private static IDensityFunction YLimitedInterpolatable(
                IDensityFunction y,
                IDensityFunction whenInRange,
                int minYInclusive,
                int maxYInclusive,
                int whenOutOfRange) =>
                DensityFunctions.Interpolated(
                    DensityFunctions.RangeChoice(
                        y,
                        minYInclusive,
                        maxYInclusive + 1,
                        whenInRange,
                        DensityFunctions.Constant(whenOutOfRange)));
        }

        private static class QuantizedSpaghettiRarity
        {
            public static IDensityFunction WrapRarity2d(IDensityFunction input, DensityFunctions.NoiseHolder noise) =>
                DensityFunctions.IntervalSelect(
                        input,
                        new[] { -0.75, -0.5, 0.5, 0.75 },
                        new[]
                        {
                            NoiseFunctionForRarity(noise, 0.5),
                            NoiseFunctionForRarity(noise, 0.75),
                            NoiseFunctionForRarity(noise, 1.0),
                            NoiseFunctionForRarity(noise, 2.0),
                            NoiseFunctionForRarity(noise, 3.0)
                        })
                    .Abs();

            public static IDensityFunction WrapRarity3d(IDensityFunction input, DensityFunctions.NoiseHolder noise) =>
                DensityFunctions.IntervalSelect(
                        input,
                        new[] { -0.5, 0.0, 0.5 },
                        new[]
                        {
                            NoiseFunctionForRarity(noise, 0.75),
                            NoiseFunctionForRarity(noise, 1.0),
                            NoiseFunctionForRarity(noise, 1.5),
                            NoiseFunctionForRarity(noise, 2.0)
                        })
                    .Abs();

            private static IDensityFunction NoiseFunctionForRarity(DensityFunctions.NoiseHolder noise, double rarity) =>
                DensityFunctions.Mul(DensityFunctions.Constant(rarity), DensityFunctions.Noise(noise, 1.0 / rarity, 1.0 / rarity));
        }
    }
}
