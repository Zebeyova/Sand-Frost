using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.TrackSystem
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SandPatch : MonoBehaviour
    {
        // public float patchSize = 16f;
        // public int resolution = 160;//分辨率
        public float thickness = 0.006f;
        public Material footStampMaterial;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;
        private MaterialPropertyBlock _propertyBlock;
        private readonly List<Vector3> _vertices = new List<Vector3>(256);//顶点列表
        private readonly List<Vector3> _normals = new List<Vector3>(256);
        private readonly List<Vector2> _uvs = new List<Vector2>(256);
        private readonly List<int> _triangles = new List<int>(384);//三角形列表
        // public Vector2Int TileCoordinate { get; private set; }

        public void Initialize(Material overlayMaterial)
        {
            // TileCoordinate = coordinate;
            footStampMaterial = overlayMaterial;

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = footStampMaterial;
            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
            _propertyBlock = new MaterialPropertyBlock();

            _mesh = new Mesh { name = "Dynamic Footprint Overlay" };
            _mesh.indexFormat = IndexFormat.UInt32;
            _mesh.MarkDynamic();
            meshFilter.sharedMesh = _mesh;
        }

        public void AddFootprint(Vector3 worldPosition, Vector3 forward, Vector3 surfaceNormal, float width, float length, float strength)
        {
            if (_mesh == null || footStampMaterial == null) return;

            Vector3 normal = surfaceNormal.sqrMagnitude > 0.01f ? surfaceNormal.normalized : Vector3.up;
            Vector3 projectedForward = Vector3.ProjectOnPlane(forward, normal);
            if (projectedForward.sqrMagnitude < 0.001f) projectedForward = Vector3.Cross(normal, Vector3.right);
            projectedForward.Normalize();

            Vector3 right = Vector3.Cross(normal, projectedForward).normalized;
            Vector3 center = worldPosition + normal * Mathf.Max(0.001f, thickness);
            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat("_StampStrength", Mathf.Clamp01(strength));
            _meshRenderer.SetPropertyBlock(_propertyBlock);
            float halfWidth = Mathf.Max(0.01f, width * 0.5f);
            float halfLength = Mathf.Max(0.01f, length * 0.5f);
            int start = _vertices.Count;

            AddVertex(center - right * halfWidth - projectedForward * halfLength, normal, right, projectedForward, new Vector2(0f, 0f));
            AddVertex(center + right * halfWidth - projectedForward * halfLength, normal, right, projectedForward, new Vector2(1f, 0f));
            AddVertex(center + right * halfWidth + projectedForward * halfLength, normal, right, projectedForward, new Vector2(1f, 1f));
            AddVertex(center - right * halfWidth + projectedForward * halfLength, normal, right, projectedForward, new Vector2(0f, 1f));

            _triangles.Add(start);
            _triangles.Add(start + 2);
            _triangles.Add(start + 1);
            _triangles.Add(start);
            _triangles.Add(start + 3);
            _triangles.Add(start + 2);

            _mesh.Clear(false);
            _mesh.SetVertices(_vertices);
            _mesh.SetNormals(_normals);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetTriangles(_triangles, 0, false);
            _mesh.RecalculateBounds();
        }

        private void AddVertex(Vector3 worldPosition, Vector3 normal, Vector3 right, Vector3 forward, Vector2 uv)
        {
            Vector3 depressionNormal = normal - (uv.x * 2f - 1f) * 0.45f * right - (uv.y * 2f - 1f) * 0.45f * forward;
            _vertices.Add(transform.InverseTransformPoint(worldPosition));
            _normals.Add(transform.InverseTransformDirection(depressionNormal.normalized));
            _uvs.Add(uv);
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
