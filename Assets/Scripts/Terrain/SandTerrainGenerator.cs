using UnityEngine;
using UnityEngine.Rendering;

namespace SandAndFrost.World
{
    /// <summary>
    /// 程序化沙丘地形：用 <see cref="SandHeightField"/> 采样出一张网格，
    /// 同时生成一颗低模碰撞体。视觉网格和碰撞体共用同一份高度场，因此永远不会对不上。
    ///
    /// 用法：空物体挂上本组件（会自动补 MeshFilter / MeshRenderer / MeshCollider），
    /// 在 Inspector 里调参数，或右键组件选 "Regenerate Terrain"。
    /// 顶点数据约定：uv0 = 地形归一化坐标 (0~1)，uv1 = 世界 XZ（给沙粒 / 噪声贴图做无缝平铺）。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    [AddComponentMenu("Sand & Frost/Sand Terrain Generator")]
    public sealed class SandTerrainGenerator : MonoBehaviour
    {
        [Header("尺寸")]
        [Tooltip("地形边长（米）。")]
        [SerializeField] float size = 120f;

        [Tooltip("视觉网格每边顶点数。必须 <= 256 才能压进 16 位索引；(2^n)+1 的手感最好。")]
        [SerializeField, Min(2)] int resolution = 241;

        [Tooltip("碰撞网格每边顶点数。物理不需要视觉精度，65 足够，且 cooking 只要几毫秒。")]
        [SerializeField, Min(2)] int colliderResolution = 65;

        [Header("高度场")]
        [SerializeField] HeightFieldSettings height = new HeightFieldSettings
        {
            seed = 2024,
            octaves = 6,
            frequency = 3.8f,
            amplitude = 12f,
            persistence = 0.48f,
            lacunarity = 2.1f,
            ridgeStrength = 0.95f,
            duneSharpness = 2.6f,
            windStretch = 5.5f,
            windDirection = new Vector2(1f, 0.35f),
        };

        [Header("生成")]
        [SerializeField] bool generateCollider = true;

        [Tooltip("Inspector 改参数时自动重建。关掉可以在大分辨率下避免卡顿。")]
        [SerializeField] bool rebuildOnValidate = true;

        Mesh _visualMesh;
        Mesh _colliderMesh;
        MeshCollider _collider;
        bool _dirty = true;

        /// <summary>地形边长（米）。</summary>
        public float Size => size;

        /// <summary>高度场参数（只读）。</summary>
        public HeightFieldSettings HeightSettings => height;

        void OnEnable()
        {
            if (_dirty || _visualMesh == null)
                Rebuild();
        }

        void OnValidate()
        {
            size = Mathf.Max(1f, size);
            resolution = Mathf.Clamp(resolution, 2, 2049);
            colliderResolution = Mathf.Clamp(colliderResolution, 2, 513);
            _dirty = true;

#if UNITY_EDITOR
            if (!rebuildOnValidate || !isActiveAndEnabled)
                return;

            // Inspecter 里拖滑块会每帧触发 OnValidate，用 delayCall 合并成一次重建。
            if (_rebuildQueued)
                return;
            _rebuildQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                _rebuildQueued = false;
                if (this != null && _dirty)
                    Rebuild();
            };
#endif
        }

#if UNITY_EDITOR
        bool _rebuildQueued;
#endif

        [ContextMenu("Regenerate Terrain")]
        public void Rebuild()
        {
            _dirty = false;

            BuildMesh(out _visualMesh, resolution, true);

            var filter = GetComponent<MeshFilter>();
            filter.sharedMesh = _visualMesh;

            if (generateCollider)
            {
                BuildMesh(out _colliderMesh, colliderResolution, false);
                _collider = GetComponent<MeshCollider>();
                _collider.sharedMesh = null;   // 先清空，否则重设同一 Mesh 时 Unity 会跳过 re-cook
                _collider.sharedMesh = _colliderMesh;
            }
            else if (_collider != null)
            {
                _collider.sharedMesh = null;
            }
        }

        /// <summary>
        /// 生成一张网格。visual = true 时附带归一化 UV 与世界 XZ UV，并算出解析法线。
        /// </summary>
        void BuildMesh(out Mesh mesh, int side, bool visual)
        {
            side = Mathf.Max(2, side);

            int count = side * side;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var uv0 = new Vector2[count];
            var uv1 = new Vector2[count];

            float cell = size / (side - 1);
            float halfSize = size * 0.5f;

            for (int z = 0; z < side; z++)
            {
                float v = (float)z / (side - 1);
                float localZ = -halfSize + z * cell;

                for (int x = 0; x < side; x++)
                {
                    float u = (float)x / (side - 1);
                    float localX = -halfSize + x * cell;
                    int i = z * side + x;

                    vertices[i] = new Vector3(localX, SandHeightField.Sample(height, u, v), localZ);
                    uv0[i] = new Vector2(u, v);
                    uv1[i] = new Vector2(localX, localZ);
                }
            }

            // 法线直接用相邻格点的高差算，和实际渲染出来的几何完全一致。
            // 比 RecalculateNormals() 快，也不会被后面 shader 里的顶点位移带偏。
            for (int z = 0; z < side; z++)
            {
                int zm = Mathf.Max(z - 1, 0);
                int zp = Mathf.Min(z + 1, side - 1);
                float dz = (zp - zm) * cell;

                for (int x = 0; x < side; x++)
                {
                    int xm = Mathf.Max(x - 1, 0);
                    int xp = Mathf.Min(x + 1, side - 1);
                    float dx = (xp - xm) * cell;

                    float hL = vertices[z * side + xm].y;
                    float hR = vertices[z * side + xp].y;
                    float hD = vertices[zm * side + x].y;
                    float hU = vertices[zp * side + x].y;

                    normals[z * side + x] = new Vector3((hL - hR) / dx, 1f, (hD - hU) / dz).normalized;
                }
            }

            var triangles = new int[(side - 1) * (side - 1) * 6];
            int t = 0;
            for (int z = 0; z < side - 1; z++)
            {
                for (int x = 0; x < side - 1; x++)
                {
                    int i = z * side + x;

                    // 逆时针（从上方看），Unity 默认正面。
                    triangles[t++] = i;
                    triangles[t++] = i + side;
                    triangles[t++] = i + 1;

                    triangles[t++] = i + 1;
                    triangles[t++] = i + side;
                    triangles[t++] = i + side + 1;
                }
            }

            mesh = new Mesh
            {
                name = visual ? "SandTerrain_Visual" : "SandTerrain_Collider",
                indexFormat = count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);

            if (visual)
            {
                mesh.SetUVs(0, uv0);
                mesh.SetUVs(1, uv1);
            }

            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// 采样世界坐标处的地面高度（假定本物体位于原点、无旋转缩放）。
        /// 给角色贴地、放置道具、判断足迹落点用。
        /// </summary>
        public float SampleHeightWorld(float worldX, float worldZ)
        {
            float u = worldX / size + 0.5f;
            float v = worldZ / size + 0.5f;
            return SandHeightField.Sample(height, u, v);
        }

        void OnDestroy()
        {
            DestroyImmediateSafe(_visualMesh);
            DestroyImmediateSafe(_colliderMesh);
        }

        static void DestroyImmediateSafe(Object mesh)
        {
            if (mesh == null)
                return;

            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
        }
    }
}
