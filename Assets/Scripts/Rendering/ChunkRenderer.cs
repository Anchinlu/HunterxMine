using MineCraftUnity.Core;
using MineCraftUnity.World;
using UnityEngine;

namespace MineCraftUnity.Rendering
{
    /// <summary>
    /// MC ref: net.minecraft.client.renderer.chunk.SectionRenderDispatcher.RenderSection
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ChunkRenderer : MonoBehaviour
    {
        private static Material[] _sharedMaterials;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private Mesh _mesh;

        public ChunkPos ChunkPosition { get; private set; }

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
            if (_meshCollider == null)
            {
                _meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            _meshCollider.enabled = false;
        }

        public void Initialize(ChunkPos position)
        {
            ChunkPosition = position;
            transform.position = Vector3.zero;
            name = $"Chunk_{position.X}_{position.Z}";
        }

        public void Rebuild(Chunk chunk, Level level, bool enableCollision)
        {
            BlockMaterialLibrary.EnsureInitialized();
            _sharedMaterials ??= BlockMaterialLibrary.GetAllMaterials();

            if (_mesh == null)
            {
                _mesh = new Mesh { name = $"ChunkMesh_{chunk.Position}" };
            }

            ChunkMeshBuilder.BuildInto(_mesh, chunk, level);
            _meshFilter.sharedMesh = _mesh;
            _meshRenderer.sharedMaterials = _sharedMaterials;

            if (enableCollision && _mesh.vertexCount > 0)
            {
                _meshCollider.sharedMesh = _mesh;
                _meshCollider.enabled = true;
            }
            else
            {
                _meshCollider.sharedMesh = null;
                _meshCollider.enabled = false;
            }

            chunk.IsMeshDirty = false;
        }

        public void SetCollisionEnabled(bool enabled)
        {
            if (_meshCollider == null || _mesh == null || _mesh.vertexCount == 0)
            {
                if (_meshCollider != null)
                {
                    _meshCollider.enabled = false;
                }

                return;
            }

            if (enabled)
            {
                _meshCollider.sharedMesh = _mesh;
                _meshCollider.enabled = true;
            }
            else
            {
                _meshCollider.sharedMesh = null;
                _meshCollider.enabled = false;
            }
        }
    }
}
