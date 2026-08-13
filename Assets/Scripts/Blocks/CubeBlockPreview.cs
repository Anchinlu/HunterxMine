using MineCraftUnity.Blocks;
using UnityEngine;

namespace MineCraftUnity.Rendering
{
    /// <summary>
    /// Preview one cube block with the same materials used in chunk terrain.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class CubeBlockPreview : MonoBehaviour
    {
        [SerializeField] private BlockId blockId = BlockId.Stone;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        public BlockId BlockType
        {
            get => blockId;
            set
            {
                blockId = value;
                Rebuild();
            }
        }

        private void OnEnable() => Rebuild();

        private void OnValidate() => Rebuild();

        public void Rebuild()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            if (blockId is BlockId.Air or BlockId.GrassBlock)
            {
                return;
            }

            BlockMaterialLibrary.EnsureInitialized();
            var layer = BlockIdToLayer(blockId);
            if (layer == null)
            {
                return;
            }

            _meshFilter.sharedMesh = TexturedBlockMeshBuilder.BuildCube();
            _meshRenderer.sharedMaterials = new[] { BlockMaterialLibrary.GetMaterial(layer.Value) };
            name = $"Preview_{blockId}";

            var collider = GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<MeshCollider>();
            }

            collider.sharedMesh = _meshFilter.sharedMesh;
        }

        private static ChunkMeshLayer? BlockIdToLayer(BlockId id) => id switch
        {
            BlockId.Stone => ChunkMeshLayer.Stone,
            BlockId.Dirt => ChunkMeshLayer.Dirt,
            BlockId.Sand => ChunkMeshLayer.Sand,
            BlockId.Water => ChunkMeshLayer.Water,
            BlockId.Bedrock => ChunkMeshLayer.Bedrock,
            BlockId.Gravel => ChunkMeshLayer.Gravel,
            _ => null
        };
    }
}
