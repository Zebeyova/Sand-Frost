using UnityEngine;

namespace Assets.Scripts.TrackSystem
{
    public class TrackPrint : MonoBehaviour
    {
        [Header("Foot Bones")] public Transform leftFoot;
        public Transform rightFoot;

        [Header("Detection")] public LayerMask layer;
        public float raycastHeight = 0.25f;
        public float raycastDistance = 0.7f;

        [Header("Footprint Shape")] public float footprintWidth = 0.18f;
        public float footprintLength = 0.30f;

        [Header("Step Settings")] public float stepDistance = 0.35f;
        [Range(0f, 1f)] public float strength = 0.8f;

        private Vector3 _lastLeft;
        private Vector3 _lastRight;

        private void Start()
        {
            if (leftFoot != null) _lastLeft = leftFoot.position;
            if (rightFoot != null) _lastRight = rightFoot.position;
            if (SandTileManager.Instance != null) SandTileManager.Instance.SetPlayer(transform);
        }

        private void Update()
        {
            if (SandTileManager.Instance == null) return;

            CheckFoot(leftFoot, ref _lastLeft);
            CheckFoot(rightFoot, ref _lastRight);
        }

        private void CheckFoot(Transform foot, ref Vector3 lastPosition)
        {
            if (foot == null) return;
            Vector3 current = foot.position;
            if (Vector3.Distance(current, lastPosition) < stepDistance) return;

            Vector3 origin = current + Vector3.up * raycastHeight;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, layer, QueryTriggerInteraction.Ignore)) return;

            SandTileManager.Instance.AddFootprint(hit.point, foot.forward, hit.normal, footprintWidth, footprintLength, strength);
            lastPosition = current;
        }
    }
}
