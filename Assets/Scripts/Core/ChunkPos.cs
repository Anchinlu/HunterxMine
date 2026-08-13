using System;

namespace MineCraftUnity.Core
{
    /// <summary>
    /// MC ref: net.minecraft.world.level.ChunkPos
    /// </summary>
    public readonly struct ChunkPos : IEquatable<ChunkPos>
    {
        public readonly int X;
        public readonly int Z;

        public ChunkPos(int x, int z)
        {
            X = x;
            Z = z;
        }

        public int GetMinBlockX() => X * WorldConstants.ChunkSize;
        public int GetMinBlockZ() => Z * WorldConstants.ChunkSize;

        public bool Equals(ChunkPos other) => X == other.X && Z == other.Z;

        public override bool Equals(object obj) => obj is ChunkPos other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Z);

        public override string ToString() => $"[{X}, {Z}]";
    }
}
