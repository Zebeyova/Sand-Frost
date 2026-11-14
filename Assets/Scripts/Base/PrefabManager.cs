// 此脚本由 PrefabExport 自动生成，请勿手动修改。
using System.Collections.Generic;
using UnityEngine;

public static class PrefabManager
{
    public const string Box_100x100x100_Prefab = "Assets/Resources/Prefabs/Box_100x100x100_Prefab.prefab";
    public const string Box_350x250x200_Prefab = "Assets/Resources/Prefabs/Box_350x250x200_Prefab.prefab";
    public const string Box_350x250x300_Prefab = "Assets/Resources/Prefabs/Box_350x250x300_Prefab.prefab";
    public const string Environment_Prefab = "Assets/Resources/Prefabs/Environment_Prefab.prefab";
    public const string MainCamera = "Assets/Resources/Prefabs/MainCamera.prefab";
    public const string Player = "Assets/Resources/Prefabs/Player.prefab";
    public const string Player_Unpack = "Assets/Resources/Prefabs/Player_Unpack.prefab";
    public const string PlayerCapsule = "Assets/Resources/Prefabs/PlayerCapsule.prefab";
    public const string PlayerCapsule_Unpack = "Assets/Resources/Prefabs/PlayerCapsule_Unpack.prefab";
    public const string PlayerFollowCamera = "Assets/Resources/Prefabs/PlayerFollowCamera.prefab";
    public const string Ramp_Prefab = "Assets/Resources/Prefabs/Ramp_Prefab.prefab";
    public const string Stairs_200x100x200_Prefab = "Assets/Resources/Prefabs/Stairs_200x100x200_Prefab.prefab";
    public const string Stairs_650_400_300_Prefab = "Assets/Resources/Prefabs/Stairs_650_400_300_Prefab.prefab";
    public const string Structure_Prefab = "Assets/Resources/Prefabs/Structure_Prefab.prefab";
    public const string Tunnel_Prefab = "Assets/Resources/Prefabs/Tunnel_Prefab.prefab";
    public const string Wall_Prefab = "Assets/Resources/Prefabs/Wall_Prefab.prefab";

    private static readonly Dictionary<string, string> _map = new Dictionary<string, string>
    {
        { "Box_100x100x100_Prefab", Box_100x100x100_Prefab },
        { "Box_350x250x200_Prefab", Box_350x250x200_Prefab },
        { "Box_350x250x300_Prefab", Box_350x250x300_Prefab },
        { "Environment_Prefab", Environment_Prefab },
        { "MainCamera", MainCamera },
        { "Player", Player },
        { "Player_Unpack", Player_Unpack },
        { "PlayerCapsule", PlayerCapsule },
        { "PlayerCapsule_Unpack", PlayerCapsule_Unpack },
        { "PlayerFollowCamera", PlayerFollowCamera },
        { "Ramp_Prefab", Ramp_Prefab },
        { "Stairs_200x100x200_Prefab", Stairs_200x100x200_Prefab },
        { "Stairs_650_400_300_Prefab", Stairs_650_400_300_Prefab },
        { "Structure_Prefab", Structure_Prefab },
        { "Tunnel_Prefab", Tunnel_Prefab },
        { "Wall_Prefab", Wall_Prefab },
    };

    public static GameObject Instantiate(string assetPath, Transform parent = null)
        => Instantiate(assetPath, Vector3.zero, Quaternion.identity, parent);

    public static GameObject Instantiate(string assetPath, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("[PrefabPath] 路径为空");
            return null;
        }

        GameObject prefab = Resources.Load<GameObject>(ToResourcePath(assetPath));
        if (prefab == null)
        {
            Debug.LogError($"[PrefabPath] Resources 中未找到预制体: {assetPath}");
            return null;
        }

        return Object.Instantiate(prefab, position, rotation, parent);
    }

    public static GameObject InstantiateByName(string prefabName, Transform parent = null)
        => InstantiateByName(prefabName, Vector3.zero, Quaternion.identity, parent);

    public static GameObject InstantiateByName(string prefabName, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (string.IsNullOrEmpty(prefabName) || !_map.TryGetValue(prefabName, out var path))
        {
            Debug.LogError($"[PrefabPath] 未找到预制体名: {prefabName}");
            return null;
        }

        return Instantiate(path, position, rotation, parent);
    }

    public static void Destroy(GameObject instance)
    {
        if (instance == null) return;
        if (Application.isPlaying) Object.Destroy(instance);
        else Object.DestroyImmediate(instance);
    }

    public static void SetActive(GameObject instance, bool active)
    {
        if (instance != null) instance.SetActive(active);
    }

    private static string ToResourcePath(string assetPath)
    {
        const string resourcesFolder = "/Resources/";
        int idx = assetPath.IndexOf(resourcesFolder, System.StringComparison.Ordinal);
        if (idx < 0) return assetPath;
        string sub = assetPath.Substring(idx + resourcesFolder.Length);
        return System.IO.Path.ChangeExtension(sub, null).Replace('\\', '/');
    }
}
