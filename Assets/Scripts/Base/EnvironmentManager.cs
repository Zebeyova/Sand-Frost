using Unity.Cinemachine;
using UnityEngine;
using System.Collections;
using Character;

namespace Base
{
    [ExecuteInEditMode]
    public class EnvironmentManager : MonoBehaviour
    {
        [Header("Player Spawn")]
        [Tooltip("Optional spawn marker. Without one, use the world origin and find the ground there.")]
        public Transform playerSpawnPoint;
        public bool waitForSpawnGround = true;
        [Tooltip("Layers containing solid ground. The terrain currently uses Default.")]
        public LayerMask spawnGroundLayers = 1;
        [Min(1f)] public float spawnGroundProbeHeight = 2000f;
        [Min(0.01f)] public float spawnGroundClearance = 0.1f;

        public Light directionalLight;

        public Gradient sunColorGradient;
        public Gradient fogColorGradient;
        public Gradient cloudColorGradient;
        public Gradient scatteringColorGradient;
        public Gradient ambientColorGradient;

        [Header("Color Gradients Enable Flags")]
        public bool overrideSunColor = true;
        public bool overrideFogColor = true;
        public bool overrideCloudColor = true;
        public bool overrideAmbientColor = true;

        [Header("Base Wind")] [Tooltip("Base wind animate the trunks")] [Range(0f, 5f)]
        public float baseWindPower = 3f;
        [Tooltip("Base wind animate the trunks")]
        public float baseWindSpeed = 1f;

        [Header("Wind Burst")] [Tooltip("Bursts are managed by a moving World-Space noise that multiply the base wind speed and power")] [Range(0f, 10f)]
        public float burstsPower = 0.5f;
        [Tooltip("Speed of the Bursts noise")] public float burstsSpeed = 5f;
        [Tooltip("Size of the Bursts noise in Word-Space")]
        public float burstsScale = 10f;

        [Header("Micro Wind")] [Tooltip("Micro wind animate the leaves")] [Range(0f, 1f)]
        public float microPower = 0.1f;
        [Tooltip("Micro wind animate the leaves")]
        public float microSpeed = 1f;
        [Tooltip("Micro wind animate the leaves")]
        public float microFrequency = 3f;

        [Space(10)] public float renderDistance = 30f;

        [Space(10)] public float Altitude = 1000f;
        public float volumeSize = 500f;
        public int volumeSamples = 25;

        private float _volumeOffset;
        private Mesh _quadMesh;
        private Matrix4x4[] _matrices;

        [Space(10)] [Tooltip("Material for the clouds")]
        public Material cloudsMaterial;
        private bool _hasIssuedMaterialWarning;

        private Camera _mainCam;
        private GameObject _runtimePlayer;
        private GameObject _runtimeFollowCamera;
        private bool _disabledMainCamera;

        void Awake()
        {
            _quadMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            _matrices = new Matrix4x4[volumeSamples];
            _mainCam = Camera.main;
        }

        private void Start()
        {
            // Keep environment previews in Edit Mode, but only create gameplay objects in Play Mode.
            if (!Application.isPlaying) return;

            // Disable the preview camera before the player's Awake resolves its gameplay camera.
            if (_mainCam != null)
            {
                _disabledMainCamera = _mainCam.gameObject.activeSelf;
                _mainCam.gameObject.SetActive(false);
            }

            bool isFinished = CreatePlayer();
            if (!isFinished)
            {
                CleanupRuntimePlayer();
                Debug.LogError("[WorldManager] 玩家初始化失败");
            }
        }

        private bool CreatePlayer()
        {
            Vector3 spawnPosition = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
            Quaternion spawnRotation = playerSpawnPoint != null
                ? Quaternion.Euler(0f, playerSpawnPoint.eulerAngles.y, 0f)
                : Quaternion.identity;
            GameObject player = _runtimePlayer = PrefabManager.Instantiate(
                PrefabManager.Player_Unpack, spawnPosition, spawnRotation);
            if (player == null) return false;

            ThirdPersonController movement = player.GetComponentInChildren<ThirdPersonController>();
            CharacterController controller = movement != null ? movement.GetComponent<CharacterController>() : null;
            if (movement == null || controller == null) return false;

            bool movementWasEnabled = movement.enabled;
            bool controllerWasEnabled = controller.enabled;
            if (waitForSpawnGround)
            {
                // The tagged player stays present so World Streamer can load around its spawn position.
                movement.enabled = false;
                controller.enabled = false;
            }

            Transform followTarget = FindCameraTarget(player.transform);
            if (followTarget == null) return false;

            GameObject cameraGo = _runtimeFollowCamera = PrefabManager.Instantiate(PrefabManager.PlayerFollowCamera);
            if (cameraGo == null) return false;

            var virtualCamera = cameraGo.GetComponentInChildren<CinemachineVirtualCameraBase>(true);
            if (virtualCamera == null) return false;

            virtualCamera.Follow = followTarget;
            virtualCamera.LookAt = followTarget;

            if (waitForSpawnGround)
                StartCoroutine(WaitForSpawnGround(movement, controller, movementWasEnabled, controllerWasEnabled));

            return true;
        }

        private IEnumerator WaitForSpawnGround(ThirdPersonController movement, CharacterController controller,
            bool movementWasEnabled, bool controllerWasEnabled)
        {
            float warningTime = Time.realtimeSinceStartup + 30f;
            bool warned = false;
            var pollDelay = new WaitForSecondsRealtime(0.1f);

            while (_runtimePlayer != null && movement != null && controller != null)
            {
                float probeHeight = Mathf.Max(1f, spawnGroundProbeHeight);
                Vector3 origin = controller.transform.position + Vector3.up * probeHeight;
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, probeHeight * 2f,
                        spawnGroundLayers, QueryTriggerInteraction.Ignore) &&
                    !hit.transform.IsChildOf(_runtimePlayer.transform))
                {
                    // Player_Unpack contains an offset child controller: align its feet, not the wrapper pivot.
                    float feetY = controller.transform.TransformPoint(controller.center).y -
                                  controller.height * Mathf.Abs(controller.transform.lossyScale.y) * 0.5f;
                    _runtimePlayer.transform.position += Vector3.up *
                        (hit.point.y + Mathf.Max(0.01f, spawnGroundClearance) - feetY);
                    Physics.SyncTransforms();
                    controller.enabled = controllerWasEnabled;
                    movement.enabled = movementWasEnabled;
                    yield break;
                }

                if (!warned && Time.realtimeSinceStartup >= warningTime)
                {
                    Debug.LogWarning("[WorldManager] 出生点地面仍未就绪，角色保持暂停。请检查出生点、地形碰撞、Spawn Ground Layers，以及 Streamer 的 Spawned Player / Player Tag 设置。", this);
                    warned = true;
                }

                yield return pollDelay;
            }
        }

        private void OnDestroy()
        {
            // Unity itself tears down Play Mode objects when leaving Play Mode.
            if (Application.isPlaying) CleanupRuntimePlayer();
        }

        private void CleanupRuntimePlayer()
        {
            StopAllCoroutines();
            PrefabManager.Destroy(_runtimeFollowCamera);
            PrefabManager.Destroy(_runtimePlayer);
            _runtimeFollowCamera = null;
            _runtimePlayer = null;
            if (_disabledMainCamera && _mainCam != null)
                _mainCam.gameObject.SetActive(true);
            _disabledMainCamera = false;
        }

        private static Transform FindCameraTarget(Transform root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.CompareTag("CinemachineTarget"))
                    return child;
            return null;
        }

        void Update()
        {
            UpdateEnvironment();
            UpdateCloudsVolume();
            UpdateLighting();
        }

        private void UpdateEnvironment()
        {
            Shader.SetGlobalFloat("WindPower", baseWindPower);
            Shader.SetGlobalFloat("WindSpeed", baseWindSpeed);
            Shader.SetGlobalFloat("WindBurstsPower", burstsPower);
            Shader.SetGlobalFloat("WindBurstsSpeed", burstsSpeed);
            Shader.SetGlobalFloat("WindBurstsScale", burstsScale);
            Shader.SetGlobalFloat("MicroPower", microPower);
            Shader.SetGlobalFloat("MicroSpeed", microSpeed);
            Shader.SetGlobalFloat("MicroFrequency", microFrequency);
            Shader.SetGlobalFloat("GrassRenderDist", renderDistance);
        }

        private void UpdateCloudsVolume()
        {
            volumeSamples = Mathf.Max(1, volumeSamples);
            volumeSize = Mathf.Max(0, volumeSize);

            if (!cloudsMaterial) return;
            if (_matrices.Length != volumeSamples)
            {
                _matrices = new Matrix4x4[volumeSamples];
            }

            if (!cloudsMaterial.HasProperty("_ScatteringColor"))
            {
                if (_hasIssuedMaterialWarning) return;

                print("The assigned material in the Cloud material slot of the EnvironmentManager isn't supported.");
                _hasIssuedMaterialWarning = true;
                return;
            }
            _hasIssuedMaterialWarning = false;

            cloudsMaterial.SetFloat("_cloudsPosition", Altitude);
            cloudsMaterial.SetFloat("_cloudsHeight", volumeSize);

            _volumeOffset = volumeSize / volumeSamples / 2f;
            Vector3 cloudsStartPosition = new Vector3(0, Altitude, 0) + (Vector3.up * (_volumeOffset * volumeSamples / 2f));

            for (int i = 0; i < volumeSamples; i++)
                _matrices[i] = Matrix4x4.TRS(cloudsStartPosition - (Vector3.up * (_volumeOffset * i)),
                    Quaternion.Euler(-90, 0, 0), new Vector3(10000, 10000, 10000));

            Graphics.DrawMeshInstanced(_quadMesh, 0, cloudsMaterial, _matrices, volumeSamples);
        }

        private void UpdateLighting()
        {
            if (!directionalLight) return;

            float dot = Vector3.Dot(directionalLight.transform.forward, Vector3.up);
            float time = (dot + 1f) / 2f;

            if (overrideFogColor)
                RenderSettings.fogColor = fogColorGradient.Evaluate(time);
            if (overrideSunColor)
                directionalLight.color = sunColorGradient.Evaluate(time);
            if (overrideAmbientColor)
                RenderSettings.ambientLight = ambientColorGradient.Evaluate(time);

            if (cloudsMaterial && cloudsMaterial.HasProperty("_ScatteringColor") && overrideCloudColor)
                cloudsMaterial.SetColor("_ScatteringColor", scatteringColorGradient.Evaluate(time));
            else if (!cloudsMaterial)
                print("cloudsMaterial is null. Please assign a material.");
        }
    }
}
