using System;
using MineCraftUnity.Blocks;
using UnityEngine;

namespace MineCraftUnity.Core
{
    /// <summary>
    /// MC ref: net.minecraft.core.BlockPos
    /// </summary>
    public readonly struct BlockPos : IEquatable<BlockPos>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public BlockPos(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public BlockPos Offset(int dx, int dy, int dz) => new(X + dx, Y + dy, Z + dz);

        public BlockPos Offset(BlockFace face) => face switch
        {
            BlockFace.Up => Offset(0, 1, 0),
            BlockFace.Down => Offset(0, -1, 0),
            BlockFace.North => Offset(0, 0, -1),
            BlockFace.South => Offset(0, 0, 1),
            BlockFace.West => Offset(-1, 0, 0),
            BlockFace.East => Offset(1, 0, 0),
            _ => this
        };

        public ChunkPos ToChunkPos() =>
            new(Mathf.FloorToInt(X / (float)WorldConstants.ChunkSize),
                Mathf.FloorToInt(Z / (float)WorldConstants.ChunkSize));

        public Vector3 ToWorldCenter() => new(X + 0.5f, Y + 0.5f, Z + 0.5f);

        public bool Equals(BlockPos other) => X == other.X && Y == other.Y && Z == other.Z;

        public override bool Equals(object obj) => obj is BlockPos other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
