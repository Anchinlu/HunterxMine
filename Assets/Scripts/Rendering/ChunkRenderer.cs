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
        private Mesh _collisionMesh;

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

        public void ApplyMeshData(ChunkMeshData data)
        {
            BlockMaterialLibrary.EnsureInitialized();
            _sharedMaterials ??= BlockMaterialLibrary.GetAllMaterials();

            if (_mesh == null)
            {
                _mesh = new Mesh { name = $"ChunkMesh_{ChunkPosition}" };
            }

            ChunkMeshBuilder.ApplyMeshData(_mesh, data);
            _meshFilter.sharedMesh = _mesh;
            _meshRenderer.sharedMaterials = _sharedMaterials;
            ApplyCollisionMesh(data);
        }

        public void Rebuild(Chunk chunk, Level level)
        {
            BlockMaterialLibrary.EnsureInitialized();
            _sharedMaterials ??= BlockMaterialLibrary.GetAllMaterials();

            if (_mesh == null)
            {
                _mesh = new Mesh { name = $"ChunkMesh_{chunk.Position}" };
            }

            var data = ChunkMeshBuilder.ComputeMeshData(new ChunkMeshSnapshot(level, chunk));
            ApplyMeshData(data);
            chunk.IsMeshDirty = false;
        }

        private void ApplyCollisionMesh(ChunkMeshData data)
        {
            if (_meshCollider == null)
            {
                return;
            }

            if (data.CollisionVertices.Length == 0 || data.CollisionTriangles.Length == 0)
            {
                _meshCollider.sharedMesh = null;
                return;
            }

            if (_collisionMesh == null)
            {
                _collisionMesh = new Mesh { name = $"ChunkCollision_{ChunkPosition}" };
            }

            _collisionMesh.Clear(false);
            _collisionMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _collisionMesh.SetVertices(data.CollisionVertices);
            _collisionMesh.SetTriangles(data.CollisionTriangles, 0);
            _collisionMesh.RecalculateBounds();
        }

        private void ApplyCollision(bool enableCollision)
        {
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
        }

        public void SetCollisionEnabled(bool enabled)
        {
            if (_meshCollider == null)
            {
                return;
            }

            if (enabled && _collisionMesh != null && _collisionMesh.vertexCount > 0)
            {
                _meshCollider.sharedMesh = _collisionMesh;
                _meshCollider.enabled = true;
                return;
            }

            if (enabled && _mesh != null && _mesh.vertexCount > 0 && (_collisionMesh == null || _collisionMesh.vertexCount == 0))
            {
                _meshCollider.sharedMesh = _mesh;
                _meshCollider.enabled = true;
                return;
            }

            _meshCollider.sharedMesh = null;
            _meshCollider.enabled = false;
        }
    }
}

