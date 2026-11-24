using System.Collections.Generic;
using UnityEngine;

namespace TrackSystem
{
    public class SandTileManager : MonoBehaviour
    {
        public static SandTileManager Instance { get; private set; }

        [Header("Tile Settings")]
        public float patchSize = 16f;
        public int resolution = 160;
        public int textureResolution = 512;
        public float thickness = 0.006f;

        [Header("Materials")]
        public Material stampMaterial;
        public Shader sandShader;

        [Header("Memory")]
        [Range(4, 64)] public int maxActivePatches = 25;

        private readonly Dictionary<Vector2Int, SandPatch> _patches = new();
        private Transform _player;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void SetPlayer(Transform target)
        {
            _player = target;
        }

        private SandPatch GetOrCreatePatch(Vector2Int coordinate)
        {
            if (_patches.TryGetValue(coordinate, out SandPatch existing)) return existing;
            if (_patches.Count >= maxActivePatches) RemoveFarthestPatch();

            GameObject go = new GameObject($"SandPatch_{coordinate.x}_{coordinate.y}");
            go.transform.SetParent(transform, false);

            SandPatch patch = go.AddComponent<SandPatch>();
            patch.patchSize = patchSize;
            patch.resolution = resolution;
            patch.thickness = thickness;
            patch.Initialize(coordinate, stampMaterial);

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
                Vector3 center = new Vector3((pair.Key.x + 0.5f) * patchSize, reference.y, (pair.Key.y + 0.5f) * patchSize);
                float distance = (center - reference).sqrMagnitude;
                if (distance <= maxDistance) continue;

                maxDistance = distance;
                farthest = pair.Key;
            }

            if (!_patches.Remove(farthest, out SandPatch patch)) return;
            if (patch != null) Destroy(patch.gameObject);
        }

        public void AddFootprint(Vector3 position, Vector3 forward, Vector3 surfaceNormal, float width, float length, float strength)
        {
            SandPatch patch = GetOrCreatePatch(WorldToTile(position));
            patch.AddFootprint(position, forward, surfaceNormal, width, length, strength);
        }

        private Vector2Int WorldToTile(Vector3 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / patchSize), Mathf.FloorToInt(position.z / patchSize));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
