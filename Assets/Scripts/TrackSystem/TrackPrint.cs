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
        [Min(0.01f)] public float minFootInterval = 0.12f;
        [Min(0f)] public float groundContactTolerance = 0.12f;
        [Range(0f, 1f)] public float strength = 1f;
        private float _lastLeftStampTime = -Mathf.Infinity;
        private float _lastRightStampTime = -Mathf.Infinity;

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
            SandTileManager.Instance.SetPlayer(transform);

            CheckFoot(leftFoot, ref _lastLeft, ref _lastLeftStampTime);
            CheckFoot(rightFoot, ref _lastRight, ref _lastRightStampTime);
        }

        private void CheckFoot(Transform foot, ref Vector3 lastPosition, ref float lastStampTime)
        {
            if (foot == null || Time.time - lastStampTime < minFootInterval) return;
            Vector3 current = foot.position;
            Vector3 planarDelta = current - lastPosition;
            planarDelta.y = 0f;

            if (planarDelta.magnitude < stepDistance) return;
            Vector3 origin = current + Vector3.up * raycastHeight;

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, layer, QueryTriggerInteraction.Ignore)) return;
            if (hit.distance > raycastHeight + groundContactTolerance) return;

            SandTileManager.Instance.AddFootprint(hit.point, foot.forward, hit.normal, footprintWidth, footprintLength, strength);

            lastPosition = current;
            lastStampTime = Time.time;
        }
    }
}
