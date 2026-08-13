using System;
using System.Security.Cryptography;
using System.Text;

namespace MineCraftUnity.WorldGen.Synth
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.levelgen.RandomSupport
    /// </summary>
    public static class RandomSupport
    {
        public const long GoldenRatio64 = -7046029254386353131L;
        public const long SilverRatio64 = 7640891576956012809L;

        public static long MixStafford13(long z)
        {
            unchecked
            {
                z = (z ^ (long)((ulong)z >> 30)) * -4658895280553007687L;
                z = (z ^ (long)((ulong)z >> 27)) * -7723592293110705685L;
                return z ^ (long)((ulong)z >> 31);
            }
        }

        public static Seed128Bit UpgradeSeedTo128BitUnmixed(long legacySeed)
        {
            unchecked
            {
                long lowBits = legacySeed ^ SilverRatio64;
                long highBits = lowBits + GoldenRatio64;
                return new Seed128Bit(lowBits, highBits);
            }
        }

        public static Seed128Bit UpgradeSeedTo128Bit(long legacySeed) => UpgradeSeedTo128BitUnmixed(legacySeed).Mixed();

        public static Seed128Bit SeedFromHashOf(string input)
        {
            byte[] hashCode;
            using (var md5 = MD5.Create())
            {
                hashCode = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            }

            long hashLo = FromBytes(
                hashCode[0], hashCode[1], hashCode[2], hashCode[3],
                hashCode[4], hashCode[5], hashCode[6], hashCode[7]);
            long hashHi = FromBytes(
                hashCode[8], hashCode[9], hashCode[10], hashCode[11],
                hashCode[12], hashCode[13], hashCode[14], hashCode[15]);
            return new Seed128Bit(hashLo, hashHi);
        }

        private static long FromBytes(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8)
        {
            return (long)b1 << 56
                   | (long)b2 << 48
                   | (long)b3 << 40
                   | (long)b4 << 32
                   | (long)b5 << 24
                   | (long)b6 << 16
                   | (long)b7 << 8
                   | b8;
        }

        public readonly struct Seed128Bit
        {
            public long SeedLo { get; }
            public long SeedHi { get; }

            public Seed128Bit(long seedLo, long seedHi)
            {
                SeedLo = seedLo;
                SeedHi = seedHi;
            }

            public Seed128Bit Xor(long lo, long hi)
            {
                unchecked
                {
                    return new Seed128Bit(SeedLo ^ lo, SeedHi ^ hi);
                }
            }

            public Seed128Bit Xor(in Seed128Bit other) => Xor(other.SeedLo, other.SeedHi);

            public Seed128Bit Mixed() => new(MixStafford13(SeedLo), MixStafford13(SeedHi));
        }
    }
}
