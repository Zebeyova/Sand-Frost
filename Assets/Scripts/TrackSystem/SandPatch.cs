using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.TrackSystem
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SandPatch : MonoBehaviour
    {
        private const string FootprintTexture = "_FootprintTex";
        private const string FootprintDepth = "_FootprintDepth";
        private const string RidgeHeight = "_RidgeHeight";

        private float _patchSize;
        private float _patchLift;
        private float _depressionDepth;
        private float _ridgeHeight;
        private float _ridgeStrength;
        private LayerMask _groundMask;
        private Mesh _mesh;
        private MeshRenderer _meshRenderer;
        private Material _stampMaterial;
        private Material _surfaceMaterial;
        private RenderTexture[] _buffers;
        private int _readIndex;

        public void Initialize(Vector2Int coordinate, float patchSize, int meshResolution, int textureResolution,
         Material stampMaterial, Shader surfaceShader, LayerMask groundMask,
          float patchLift, float depressionDepth, float ridgeHeight, float ridgeStrength)
        {
            _patchSize = patchSize;
            _groundMask = groundMask;
            _patchLift = patchLift;
            _depressionDepth = depressionDepth;
            _ridgeHeight = ridgeHeight;
            _ridgeStrength = ridgeStrength;

            _meshRenderer = GetComponent<MeshRenderer>();
            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = true;

            _mesh = new Mesh
            {
                name = $"SandPatchMesh_{coordinate.x}_{coordinate.y}",
                indexFormat = IndexFormat.UInt32
            };

            _mesh.MarkDynamic();

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.sharedMesh = _mesh;

            _stampMaterial = new Material(stampMaterial)
            {
                name = $"SandStamp_{coordinate.x}_{coordinate.y}"
            };

            _surfaceMaterial = new Material(surfaceShader)
            {
                name = $"SandSurface_{coordinate.x}_{coordinate.y}"
            };

            BuildGridMesh(meshResolution);
            CreateRenderTextures(textureResolution);
            ClearRenderTextures();

            if (_surfaceMaterial.HasProperty(FootprintDepth)) _surfaceMaterial.SetFloat(FootprintDepth, _depressionDepth);
            if (_surfaceMaterial.HasProperty(RidgeHeight)) _surfaceMaterial.SetFloat(RidgeHeight, _ridgeHeight);

            Texture baseMap = null;

            if (stampMaterial.HasProperty("_BaseMap")) baseMap = stampMaterial.GetTexture("_BaseMap");
            if (baseMap != null && _surfaceMaterial.HasProperty("_BaseMap")) _surfaceMaterial.SetTexture("_BaseMap", baseMap);

            _meshRenderer.sharedMaterial = _surfaceMaterial;
            BindCurrentTexture();
        }

        private void BuildGridMesh(int resolution)
        {
            resolution = Mathf.Max(2, resolution);

            int vertexCount = (resolution + 1) * (resolution + 1);

            List<Vector3> vertices = new(vertexCount);

            List<Vector3> normals = new(vertexCount);

            List<Vector2> uvs = new(vertexCount);

            List<int> triangles = new(resolution * resolution * 6);

            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    float u = x / (float)resolution;
                    float v = z / (float)resolution;

                    Vector3 localHorizontal = new(u * _patchSize, 0f, v * _patchSize);
                    Vector3 worldPosition = transform.TransformPoint(localHorizontal);
                    worldPosition.y = 10000f;

                    TrySampleSurface(worldPosition, out float surfaceHeight, out Vector3 surfaceNormal);

                    Vector3 finalWorldPosition = new(worldPosition.x, surfaceHeight + _patchLift, worldPosition.z);
                    vertices.Add(transform.InverseTransformPoint(finalWorldPosition));
                    normals.Add(transform.InverseTransformDirection(surfaceNormal).normalized);

                    uvs.Add(new Vector2(u, v));
                }
            }

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int a = z * (resolution + 1) + x;
                    int b = a + 1;
                    int c = a + resolution + 1;
                    int d = c + 1;

                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);

                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            _mesh.Clear(false);
            _mesh.SetVertices(vertices);
            _mesh.SetNormals(normals);
            _mesh.SetUVs(0, uvs);
            _mesh.SetTriangles(triangles, 0, false);
            _mesh.RecalculateBounds();
        }

        private bool TrySampleSurface(
            Vector3 worldPosition,
            out float height,
            out Vector3 normal)
        {
            Terrain[] terrains = Terrain.activeTerrains;

            foreach (Terrain terrain in terrains)
            {
                Vector3 local = terrain.transform.InverseTransformPoint(worldPosition);
                Vector3 size = terrain.terrainData.size;

                if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z) continue;

                float u = Mathf.Clamp01(local.x / size.x);
                float v = Mathf.Clamp01(local.z / size.z);

                height = terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
                normal = terrain.transform.TransformDirection(terrain.terrainData.GetInterpolatedNormal(u, v)).normalized;

                return true;
            }

            int mask = _groundMask.value != 0 ? _groundMask.value : Physics.DefaultRaycastLayers;
            if (Physics.Raycast(new Vector3(worldPosition.x, 10000f, worldPosition.z), Vector3.down,
            out RaycastHit hit, 20000f, mask, QueryTriggerInteraction.Ignore))
            {
                height = hit.point.y;
                normal = hit.normal.normalized;
                return true;
            }

            height = transform.position.y;
            normal = Vector3.up;
            return false;
        }

        private void CreateRenderTextures(int resolution)
        {
            resolution = Mathf.Max(64, resolution);
            _buffers = new RenderTexture[2];

            RenderTextureDescriptor descriptor = new(resolution, resolution, RenderTextureFormat.ARGB32, 0)
            {
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false,
                sRGB = false
            };

            for (int i = 0; i < _buffers.Length; i++)
            {
                _buffers[i] = new RenderTexture(descriptor)
                {
                    name = $"SandFootprintRT_{i}",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                _buffers[i].Create();
            }
        }

        private void ClearRenderTextures()
        {
            if (_buffers == null) return;

            RenderTexture previous = RenderTexture.active;

            foreach (RenderTexture buffer in _buffers)
            {
                RenderTexture.active = buffer;
                GL.Clear(false, true, Color.clear);
            }

            RenderTexture.active = previous;
            _readIndex = 0;
        }

        public void AddFootprint(Vector3 worldPosition, Vector3 forward, Vector3 surfaceNormal, float width, float length, float strength)
        {
            if (_buffers == null || _stampMaterial == null || _surfaceMaterial == null) return;

            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

            Vector2 uv = new Vector2(localPosition.x / _patchSize, localPosition.z / _patchSize);
            if (uv.x < -0.05f || uv.x > 1.05f || uv.y < -0.05f || uv.y > 1.05f) return;

            Vector3 projectedForward = Vector3.ProjectOnPlane(forward, surfaceNormal.sqrMagnitude > 0.001f ? surfaceNormal.normalized : Vector3.up);

            if (projectedForward.sqrMagnitude < 0.001f) projectedForward = transform.forward;
            projectedForward.Normalize();

            Vector3 localForward = transform.InverseTransformDirection(projectedForward);

            float angle = Mathf.Atan2(localForward.x, localForward.z);
            float halfWidth = Mathf.Max(0.005f, width * 0.5f / _patchSize);
            float halfLength = Mathf.Max(0.005f, length * 0.5f / _patchSize);

            _stampMaterial.SetVector("_StampUV", new Vector4(uv.x, uv.y, halfWidth, halfLength));
            _stampMaterial.SetFloat("_StampAngle", angle);
            _stampMaterial.SetFloat("_StampStrength", Mathf.Clamp01(strength));
            _stampMaterial.SetFloat("_RidgeStrength", Mathf.Clamp01(_ridgeStrength));
            _stampMaterial.SetFloat("_Fade", 1f);

            BlitStamp();
        }

        public void Fade(float fadeFactor)
        {
            if (_buffers == null || _stampMaterial == null) return;

            _stampMaterial.SetFloat("_StampStrength", 0f);
            _stampMaterial.SetFloat("_RidgeStrength", 0f);
            _stampMaterial.SetFloat("_Fade", Mathf.Clamp01(fadeFactor));

            BlitStamp();
            _stampMaterial.SetFloat("_Fade", 1f);
        }

        private void BlitStamp()
        {
            int writeIndex = 1 - _readIndex;

            Graphics.Blit(_buffers[_readIndex], _buffers[writeIndex], _stampMaterial, 0);

            _readIndex = writeIndex;
            BindCurrentTexture();
        }

        private void BindCurrentTexture()
        {
            if (_surfaceMaterial == null || _buffers == null) return;
            _surfaceMaterial.SetTexture(FootprintTexture, _buffers[_readIndex]);
        }

        private void OnDestroy()
        {
            if (_buffers != null)
            {
                foreach (RenderTexture buffer in _buffers)
                {
                    if (buffer == null) continue;

                    buffer.Release();
                    Destroy(buffer);
                }
            }

            if (_mesh != null) Destroy(_mesh);
            if (_stampMaterial != null) Destroy(_stampMaterial);
            if (_surfaceMaterial != null) Destroy(_surfaceMaterial);
        }
    }
}