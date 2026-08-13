using System;

namespace MineCraftUnity.WorldGen.Synth
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.XoroshiroRandomSource
    /// </summary>
    public sealed class XoroshiroRandomSource : IRandomSource
    {
        private const float FloatUnit = 5.9604645E-8F;
        private const double DoubleUnit = 1.110223E-16F;

        private Xoroshiro128PlusPlus _randomNumberGenerator;

        public XoroshiroRandomSource(long seed)
        {
            _randomNumberGenerator = new Xoroshiro128PlusPlus(RandomSupport.UpgradeSeedTo128Bit(seed));
        }

        public XoroshiroRandomSource(in RandomSupport.Seed128Bit seed)
        {
            _randomNumberGenerator = new Xoroshiro128PlusPlus(seed);
        }

        public XoroshiroRandomSource(long seedLo, long seedHi)
        {
            _randomNumberGenerator = new Xoroshiro128PlusPlus(seedLo, seedHi);
        }

        private XoroshiroRandomSource(Xoroshiro128PlusPlus randomNumberGenerator)
        {
            _randomNumberGenerator = randomNumberGenerator;
        }

        public IRandomSource Fork() =>
            new XoroshiroRandomSource(_randomNumberGenerator.NextLong(), _randomNumberGenerator.NextLong());

        public IPositionalRandomFactory ForkPositional()
        {
            return new XoroshiroPositionalRandomFactory(
                _randomNumberGenerator.NextLong(),
                _randomNumberGenerator.NextLong());
        }

        public int NextInt() => (int)_randomNumberGenerator.NextLong();

        public int NextInt(int bound)
        {
            if (bound <= 0)
            {
                throw new ArgumentException("Bound must be positive", nameof(bound));
            }

            unchecked
            {
                long randomBits = (uint)NextInt();
                long multipliedRandomBits = randomBits * bound;
                long fractionalPart = multipliedRandomBits & 4294967295L;
                if (fractionalPart < bound)
                {
                    long unbiasedBucketsStartIndex = (uint)(~bound + 1) % (uint)bound;
                    while (fractionalPart < unbiasedBucketsStartIndex)
                    {
                        randomBits = (uint)NextInt();
                        multipliedRandomBits = randomBits * bound;
                        fractionalPart = multipliedRandomBits & 4294967295L;
                    }
                }

                long integerPart = multipliedRandomBits >> 32;
                return (int)integerPart;
            }
        }

        public long NextLong() => _randomNumberGenerator.NextLong();

        public bool NextBoolean() => (_randomNumberGenerator.NextLong() & 1L) != 0L;

        public float NextFloat() => (float)NextBits(24) * FloatUnit;

        public double NextDouble() => NextBits(53) * DoubleUnit;

        public void ConsumeCount(int rounds)
        {
            for (var i = 0; i < rounds; i++)
            {
                _randomNumberGenerator.NextLong();
            }
        }

        private long NextBits(int bits) => (long)((ulong)_randomNumberGenerator.NextLong() >> (64 - bits));

        private sealed class XoroshiroPositionalRandomFactory : IPositionalRandomFactory
        {
            private readonly long _seedLo;
            private readonly long _seedHi;

            public XoroshiroPositionalRandomFactory(long seedLo, long seedHi)
            {
                _seedLo = seedLo;
                _seedHi = seedHi;
            }

            public IRandomSource At(int x, int y, int z)
            {
                long positionalSeed = Mth.GetSeed(x, y, z);
                long randomSeed = positionalSeed ^ _seedLo;
                return new XoroshiroRandomSource(randomSeed, _seedHi);
            }

            public IRandomSource FromHashOf(string name)
            {
                RandomSupport.Seed128Bit seed = RandomSupport.SeedFromHashOf(name);
                return new XoroshiroRandomSource(seed.Xor(_seedLo, _seedHi));
            }

            public IRandomSource FromSeed(long seed) => new XoroshiroRandomSource(seed ^ _seedLo, seed ^ _seedHi);
        }
    }
}
