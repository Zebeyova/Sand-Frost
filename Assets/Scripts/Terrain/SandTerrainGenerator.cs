using UnityEngine;
using UnityEngine.Rendering;

namespace Terrain
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
        [Tooltip("地形边长（米）。单块探索场景建议 1500~2000 米。")]
        [SerializeField] float size = 2000f;

        [Header("无缝拼合")]
        [Tooltip("勾选后按世界网格坐标采样高度场：相邻地块共用同一张无限噪声，接缝天然对齐。\n" +
                 "必须和相邻地块的 Seed / 风速 / 波长等参数完全一致。")]
        [SerializeField] bool tileable = true;

        [Tooltip("本块在地形网格中的坐标（自动由物体世界坐标推算，也可手动指定）。\n" +
                 "网格坐标 * Size 就是本块中心的世界坐标。")]
        [SerializeField] Vector2Int tileCoord = Vector2Int.zero;

        [Tooltip("勾选后每次重建都根据物体 Transform 的世界位置自动推算 tileCoord，避免手动摆位对不上。")]
        [SerializeField] bool autoTileFromTransform = true;

        [Tooltip("视觉网格每边顶点数。513 时 2000 米地图每格约 3.9 米；需要更细近景时可升到 1025。")]
        [SerializeField, Min(2)] int resolution = 513;

        [Tooltip("视觉法线使用的高度采样半径（以网格格子为单位）。更大的值会让沙脊明暗更平滑。")]
        [SerializeField, Range(1, 4)] int normalSampleRadius = 2;

        [Tooltip("碰撞网格每边顶点数。129 对角色移动已经足够，视觉网格和碰撞网格可以保持不同精度。")]
        [SerializeField, Min(2)] int colliderResolution = 129;

        [Header("高度场")]
        [SerializeField] HeightFieldSettings height = new HeightFieldSettings
        {
            version = 3,
            seed = 2024,
            octaves = 4,
            frequency = 4f,
            amplitude = 20f,
            persistence = 0.42f,
            lacunarity = 1.9f,
            ridgeStrength = 0.25f,
            duneSharpness = 1f,
            warpStrength = 0.16f,
            warpScale = 1.25f,
            macroAmplitude = 0.3f,
            macroFrequency = 0.55f,
            macroOctaves = 2,
            windStretch = 6f,
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
            Migrate();
            if (_dirty || _visualMesh == null)
                Rebuild();
        }

        /// <summary>
        /// Keep legacy serialized fields valid while marking them as migrated to the current model.
        /// </summary>
        void Migrate()
        {
            if (height.version >= 3)
                return;

            // Version 2 used a fixed 1000 m noise span and much sharper 50 m dunes.
            // Replace that serialized preset as a whole so existing scenes actually use
            // the new 2000 m desert profile instead of retaining the old terrain shape.
            int seed = height.seed == 0 ? 2024 : height.seed;
            height = new HeightFieldSettings
            {
                version = 3,
                seed = seed,
                octaves = 4,
                frequency = 4f,
                amplitude = 20f,
                persistence = 0.42f,
                lacunarity = 1.9f,
                ridgeStrength = 0.25f,
                duneSharpness = 1f,
                warpStrength = 0.16f,
                warpScale = 1.25f,
                macroAmplitude = 0.3f,
                macroFrequency = 0.55f,
                macroOctaves = 2,
                windStretch = 6f,
                windDirection = new Vector2(1f, 0.35f),
            };

            if (Mathf.Approximately(size, 1000f)) size = 2000f;
            if (resolution == 255) resolution = 513;
            if (colliderResolution == 256) colliderResolution = 129;
            if (normalSampleRadius <= 0) normalSampleRadius = 2;

            _dirty = true;
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

            if (tileable && autoTileFromTransform)
                SyncTileCoordFromTransform();

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
        /// 根据物体的世界位置反推它属于哪个网格单元。
        /// 这样你只要把地块摆到 size 的整数倍位置，重建时就会自动认领正确的世界坐标，
        /// 不用手动填 tileCoord（填错一格就是 120 米的地貌错位）。
        /// </summary>
        void SyncTileCoordFromTransform()
        {
            if (size <= 1e-4f)
                return;

            Vector3 p = transform.position;
            tileCoord = new Vector2Int(
                Mathf.RoundToInt(p.x / size),
                Mathf.RoundToInt(p.z / size));
        }

        /// <summary>
        /// 把物体吸附到本块网格单元的中心。摆位时容易差几米，吸一下保证接缝严丝合缝。
        /// </summary>
        [ContextMenu("Snap To Tile Grid")]
        public void SnapToTileGrid()
        {
            if (size <= 1e-4f)
                return;

            SyncTileCoordFromTransform();
            var p = transform.position;
            transform.position = new Vector3(tileCoord.x * size, p.y, tileCoord.y * size);
            Rebuild();
        }

        /// <summary>
        /// 生成一张网格。visual = true 时附带归一化 UV 与世界 XZ UV，并算出解析法线。
        /// </summary>
        void BuildMesh(out Mesh mesh, int side, bool visual)
        {
            side = Mathf.Max(2, side);

            int count = side * side;
            var vertices = new Vector3[count];
            var heights = new float[count];
            var normals = new Vector3[count];
            var uv0 = new Vector2[count];
            var uv1 = new Vector2[count];

            float cell = size / (side - 1);
            float halfSize = size * 0.5f;

            // 本块中心的世界坐标。拼合时由 tileCoord * size 决定，
            // 相邻地块因此共享同一套全局坐标系，接缝处的采样点完全一致。
            Vector2 tileCenter = tileable
                ? new Vector2(tileCoord.x * size, tileCoord.y * size)
                : Vector2.zero;

            for (int z = 0; z < side; z++)
            {
                float v = (float)z / (side - 1);
                float localZ = -halfSize + z * cell;
                float worldZ = tileCenter.y + localZ;

                for (int x = 0; x < side; x++)
                {
                    float u = (float)x / (side - 1);
                    float localX = -halfSize + x * cell;
                    float worldX = tileCenter.x + localX;
                    int i = z * side + x;

                    // 拼合时按世界坐标采样：相邻地块的边界点算出来的是同一个高度。
                    // 非拼合时按本块归一化坐标采样，并把 size 传进去，
                    // 保证 Frequency 的语义（整块地形上几个沙丘）在两种模式下一致。
                    float h = tileable
                        ? SandHeightField.SampleWorld(height, worldX, worldZ, size)
                        : SandHeightField.Sample(height, u, v, size);

                    heights[i] = h;
                    vertices[i] = new Vector3(localX, h, localZ);
                    uv0[i] = new Vector2(u, v);
                    uv1[i] = new Vector2(worldX, worldZ);
                }
            }

            // Use a wider finite-difference window for the visual mesh. At tile
            // borders, sample the continuous height field outside the local mesh
            // so adjacent tiles receive the same normal at their shared edge.
            int radius = visual ? Mathf.Clamp(normalSampleRadius, 1, 4) : 1;
            float normalStep = cell * radius;
            float uvStep = (float)radius / (side - 1);
            for (int z = 0; z < side; z++)
            {
                for (int x = 0; x < side; x++)
                {
                    int i = z * side + x;
                    float u = (float)x / (side - 1);
                    float v = (float)z / (side - 1);
                    float localX = -halfSize + x * cell;
                    float localZ = -halfSize + z * cell;
                    float worldX = tileCenter.x + localX;
                    float worldZ = tileCenter.y + localZ;

                    float hL = SampleNeighborHeight(heights, side, x - radius, z,
                        worldX - normalStep, worldZ, u - uvStep, v);
                    float hR = SampleNeighborHeight(heights, side, x + radius, z,
                        worldX + normalStep, worldZ, u + uvStep, v);
                    float hD = SampleNeighborHeight(heights, side, x, z - radius,
                        worldX, worldZ - normalStep, u, v - uvStep);
                    float hU = SampleNeighborHeight(heights, side, x, z + radius,
                        worldX, worldZ + normalStep, u, v + uvStep);

                    normals[i] = new Vector3(
                        (hL - hR) / (2f * normalStep),
                        1f,
                        (hD - hU) / (2f * normalStep)
                    ).normalized;
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
            if (visual)
                mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        float SampleNeighborHeight(float[] heights, int side, int x, int z,
            float worldX, float worldZ, float u, float v)
        {
            if (x >= 0 && x < side && z >= 0 && z < side)
                return heights[z * side + x];

            return tileable
                ? SandHeightField.SampleWorld(height, worldX, worldZ, size)
                : SandHeightField.Sample(height, u, v, size);
        }

        /// <summary>
        /// 采样世界坐标处的地面高度（假定本物体无旋转缩放）。
        /// 给角色贴地、放置道具、判断足迹落点用。
        ///
        /// 拼合模式下直接按世界坐标查噪声场，所以跨地块查询也返回正确高度，
        /// 且和相邻地块渲染出来的网格完全一致（不存在接缝处贴地抖动）。
        /// </summary>
        public float SampleHeightWorld(float worldX, float worldZ)
        {
            if (tileable)
                return SandHeightField.SampleWorld(height, worldX, worldZ, size);

            // 单块模式：物体原地不动时，把世界坐标转回本块归一化坐标。
            Vector3 origin = transform.position;
            float u = (worldX - origin.x) / size + 0.5f;
            float v = (worldZ - origin.z) / size + 0.5f;
            return SandHeightField.Sample(height, u, v, size);
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
