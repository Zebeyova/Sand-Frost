using Unity.Cinemachine;
using UnityEngine;

namespace Base
{
    public class WorldManager : MonoBehaviour
    {
        private Camera _mainCam;

        public void Start()
        {
            _mainCam = Camera.main;
            if (_mainCam != null) _mainCam.gameObject.SetActive(false);
            bool isFinished = CreatePlayer();
            if (!isFinished)
            {
                Debug.LogError("[WorldManager] 玩家初始化失败");
            }
        }

        /// <summary>
        /// 在游戏开始时加载玩家与第三人称跟随相机，并完成二者绑定。
        /// </summary>
        private bool CreatePlayer()
        {
            // 实例化玩家
            GameObject player = PrefabManager.Instantiate(PrefabManager.Player_Unpack);
            if (player == null)
            {
                Debug.LogError("[WorldManager] 玩家实例化失败");
                return false;
            }

            Transform followTarget = FindCameraTarget(player.transform);
            if (followTarget == null)
            {
                Debug.LogError("[WorldManager] 玩家身上未找到 tag=CinemachineTarget 的跟随目标");
                return false;
            }

            // 实例化第三人称跟随相机（相机在独立的 PlayerFollowCamera 预制体中）
            GameObject cameraGo = PrefabManager.Instantiate(PrefabManager.PlayerFollowCamera);
            if (cameraGo == null)
            {
                Debug.LogError("[WorldManager] 跟随相机实例化失败");
                return false;
            }

            // 用基类 CinemachineVirtualCameraBase 解析相机，兼容旧组件(CinemachineVirtualCamera)
            var virtualCamera = cameraGo.GetComponentInChildren<CinemachineVirtualCameraBase>(true);
            if (virtualCamera == null)
            {
                Debug.LogError("[WorldManager] PlayerFollowCamera 上未找到 Cinemachine 相机组件");
                return false;
            }

            // 绑定虚拟相机的 Follow / LookAt
            virtualCamera.Follow = followTarget;
            virtualCamera.LookAt = followTarget;
            return true;
        }

        private static Transform FindCameraTarget(Transform root)
        {
            // 优先在当前层级下查找 tag=CinemachineTarget 的子物体
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.CompareTag("CinemachineTarget"))
                    return child;
            }

            return null;
        }
    }
}