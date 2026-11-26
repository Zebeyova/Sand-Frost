using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.TrackSystem
{
    public class SandTileManager : MonoBehaviour
    {
        public static SandTileManager Instance { get; private set; }

        [Header("Tile Settings")]
        public float patchSize = 16f;
        [Min(0f)] public float thickness = 0.125f;
        public int meshResolution = 64;
        public int texResolution = 128;

        [Header("Surface")]
        public Shader sandSurfaceShader;
        public LayerMask groundMask;

        [Header("Displacement")]
        [Min(0f)] public float depressionHeight = 0.06f;
        [Min(0f)] public float ridgeHeight = 0.05f;
        [Range(0f, 1f)] public float ridgeStrength = 0.8f;

        [Header("Fade")]
        [Min(0.01f)] public float fadeHalfLife = 20f;
        [Min(0.01f)] public float fadeInterval = 0.1f;

        [Header("Materials")]
        public Material stampMaterial;

        [Header("Memory")]
        [Range(4, 64)] public int maxActivePatches = 25;

        private readonly Dictionary<Vector2Int, SandPatch> _patches = new();
        private Transform _player;
        private float _fadeTimer;

        public void SetPlayer(Transform target)
        {
            _player = target;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Update()
        {
            if (_patches == null || _patches.Count == 0 || fadeHalfLife <= 0f) return;

            _fadeTimer += Time.deltaTime;
            if (_fadeTimer < fadeInterval) return;

            float elapsed = _fadeTimer;
            _fadeTimer = 0f;

            float fadeFactor = Mathf.Pow(0.5f, elapsed / fadeHalfLife);
            foreach (SandPatch patch in _patches.Values) if (patch != null) patch.Fade(fadeFactor);
        }

        public void AddFootprint(Vector3 position, Vector3 forward, Vector3 surfaceNormal, float width, float length, float strength)
        {
            SandPatch patch = GetOrCreatePatch(new Vector2Int(Mathf.FloorToInt(position.x / patchSize), Mathf.FloorToInt(position.z / patchSize)));

            if (patch == null) return;
            patch.AddFootprint(position, forward, surfaceNormal, width, length, strength);
        }

        private SandPatch GetOrCreatePatch(Vector2Int coordinate)
        {
            if (stampMaterial == null || sandSurfaceShader == null) return null;
            if (_patches.TryGetValue(coordinate, out SandPatch sandPatch)) return sandPatch;
            if (_patches.Count >= maxActivePatches) RemoveFarthestPatch();

            GameObject go = new($"SandPatch_{coordinate.x}_{coordinate.y}");
            go.transform.SetParent(transform, false);
            go.transform.SetLocalPositionAndRotation(new(coordinate.x * patchSize, 0f, coordinate.y * patchSize), Quaternion.identity);
            go.transform.localScale = Vector3.one;

            SandPatch patch = go.AddComponent<SandPatch>();
            float maxDepression = Mathf.Max(0f, thickness - 0.002f);
            float effectiveDepression = Mathf.Min(depressionHeight, maxDepression);
            patch.Initialize(coordinate, patchSize, meshResolution, texResolution, stampMaterial, sandSurfaceShader, groundMask, thickness, effectiveDepression, ridgeHeight, ridgeStrength);

            _patches.Add(coordinate, patch);
            return patch;
        }

        private void RemoveFarthestPatch()
        {
            if (_patches.Count == 0) return;

            Vector3 reference = _player != null ? _player.position : Vector3.zero;
            Vector2Int farthest = default;
            float maxDistance = -1f;

            foreach (KeyValuePair<Vector2Int, SandPatch> pair in _patches)
            {
                Vector3 center = new((pair.Key.x + 0.5f) * patchSize, reference.y, (pair.Key.y + 0.5f) * patchSize);
                float distance = (center - reference).sqrMagnitude;
                if (distance <= maxDistance) continue;

                maxDistance = distance;
                farthest = pair.Key;
            }

            if (!_patches.Remove(farthest, out SandPatch patch)) return;
            if (patch != null) Destroy(patch.gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
