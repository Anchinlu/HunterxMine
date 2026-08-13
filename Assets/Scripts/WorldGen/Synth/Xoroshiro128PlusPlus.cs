using System;

namespace MineCraftUnity.WorldGen.Synth
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.Xoroshiro128PlusPlus
    /// </summary>
    public sealed class Xoroshiro128PlusPlus
    {
        private long _seedLo;
        private long _seedHi;

        public Xoroshiro128PlusPlus(in RandomSupport.Seed128Bit seed)
            : this(seed.SeedLo, seed.SeedHi)
        {
        }

        public Xoroshiro128PlusPlus(long seedLo, long seedHi)
        {
            _seedLo = seedLo;
            _seedHi = seedHi;
            if ((_seedLo | _seedHi) == 0L)
            {
                _seedLo = RandomSupport.GoldenRatio64;
                _seedHi = RandomSupport.SilverRatio64;
            }
        }

        public long NextLong()
        {
            unchecked
            {
                long s0 = _seedLo;
                long s1 = _seedHi;
                long result = RotateLeft(s0 + s1, 17) + s0;
                s1 ^= s0;
                _seedLo = RotateLeft(s0, 49) ^ s1 ^ (s1 << 21);
                _seedHi = RotateLeft(s1, 28);
                return result;
            }
        }

        private static long RotateLeft(long value, int offset)
        {
            return (long)((ulong)value << offset | (ulong)value >> (64 - offset));
        }
    }
}
