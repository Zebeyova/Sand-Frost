// 此脚本由 PrefabExport 自动生成，请勿手动修改。
using System.Collections.Generic;
using UnityEngine;

public static class PrefabManager
{
    public const string MainCamera = "Assets/Resources/Prefabs/Camera/MainCamera.prefab";
    public const string PlayerFollowCamera = "Assets/Resources/Prefabs/Camera/PlayerFollowCamera.prefab";
    public const string Player = "Assets/Resources/Prefabs/Character/Player.prefab";
    public const string Player_Unpack = "Assets/Resources/Prefabs/Character/Player_Unpack.prefab";
    public const string Environment_Manager = "Assets/Resources/Prefabs/Environment/Environment Manager.prefab";
    public const string Dust = "Assets/Resources/Prefabs/Fx/Dust.prefab";
    public const string GodRays = "Assets/Resources/Prefabs/Fx/GodRays.prefab";
    public const string HeatDistortion = "Assets/Resources/Prefabs/Fx/HeatDistortion.prefab";
    public const string Stars = "Assets/Resources/Prefabs/Fx/Stars.prefab";
    public const string Terrain = "Assets/Resources/Prefabs/Fx/Terrain.prefab";
    public const string WaterSplash = "Assets/Resources/Prefabs/Fx/WaterSplash.prefab";
    public const string Branches = "Assets/Resources/Prefabs/Plants/Branches.prefab";
    public const string Buttercup1 = "Assets/Resources/Prefabs/Plants/Buttercup1.prefab";
    public const string Buttercup2 = "Assets/Resources/Prefabs/Plants/Buttercup2.prefab";
    public const string Buttercup3 = "Assets/Resources/Prefabs/Plants/Buttercup3.prefab";
    public const string Buttercup4 = "Assets/Resources/Prefabs/Plants/Buttercup4.prefab";
    public const string Lillypads1 = "Assets/Resources/Prefabs/Plants/Lillypads1.prefab";
    public const string Lillypads2 = "Assets/Resources/Prefabs/Plants/Lillypads2.prefab";
    public const string Lillypads3 = "Assets/Resources/Prefabs/Plants/Lillypads3.prefab";
    public const string Malcomia_grass = "Assets/Resources/Prefabs/Plants/Malcomia_grass.prefab";
    public const string Malcomia_white1 = "Assets/Resources/Prefabs/Plants/Malcomia_white1.prefab";
    public const string Malcomia_white3 = "Assets/Resources/Prefabs/Plants/Malcomia_white3.prefab";
    public const string Malcomia_white4 = "Assets/Resources/Prefabs/Plants/Malcomia_white4.prefab";
    public const string Malcomia_white5 = "Assets/Resources/Prefabs/Plants/Malcomia_white5.prefab";
    public const string Malcomia1 = "Assets/Resources/Prefabs/Plants/Malcomia1.prefab";
    public const string Malcomia3 = "Assets/Resources/Prefabs/Plants/Malcomia3.prefab";
    public const string Malcomia4 = "Assets/Resources/Prefabs/Plants/Malcomia4.prefab";
    public const string Malcomia5 = "Assets/Resources/Prefabs/Plants/Malcomia5.prefab";
    public const string Perennial1 = "Assets/Resources/Prefabs/Plants/Perennial1.prefab";
    public const string Perennial2 = "Assets/Resources/Prefabs/Plants/Perennial2.prefab";
    public const string Perennial3 = "Assets/Resources/Prefabs/Plants/Perennial3.prefab";
    public const string Reeds1 = "Assets/Resources/Prefabs/Plants/Reeds1.prefab";
    public const string Reeds2 = "Assets/Resources/Prefabs/Plants/Reeds2.prefab";
    public const string Reeds3 = "Assets/Resources/Prefabs/Plants/Reeds3.prefab";
    public const string Reeds4 = "Assets/Resources/Prefabs/Plants/Reeds4.prefab";
    public const string Reeds5 = "Assets/Resources/Prefabs/Plants/Reeds5.prefab";
    public const string DesertArch_0 = "Assets/Resources/Prefabs/Rocks/DesertArch_0.prefab";
    public const string DesertArch_1 = "Assets/Resources/Prefabs/Rocks/DesertArch_1.prefab";
    public const string DesertArch_2 = "Assets/Resources/Prefabs/Rocks/DesertArch_2.prefab";
    public const string DesertBoulder_0 = "Assets/Resources/Prefabs/Rocks/DesertBoulder_0.prefab";
    public const string DesertBoulder_1 = "Assets/Resources/Prefabs/Rocks/DesertBoulder_1.prefab";
    public const string DesertBoulder_2 = "Assets/Resources/Prefabs/Rocks/DesertBoulder_2.prefab";
    public const string DesertBoulder_3 = "Assets/Resources/Prefabs/Rocks/DesertBoulder_3.prefab";
    public const string DesertBoulder_4 = "Assets/Resources/Prefabs/Rocks/DesertBoulder_4.prefab";
    public const string DesertBoulder_5 = "Assets/Resources/Prefabs/Rocks/DesertBoulder_5.prefab";
    public const string DesertBoulder_6 = "Assets/Resources/Prefabs/Rocks/DesertBoulder_6.prefab";
    public const string DesertBoulder_7 = "Assets/Resources/Prefabs/Rocks/DesertBoulder_7.prefab";
    public const string DesertBoulder_8 = "Assets/Resources/Prefabs/Rocks/DesertBoulder_8.prefab";
    public const string DesertBoulder_9 = "Assets/Resources/Prefabs/Rocks/DesertBoulder_9.prefab";
    public const string DesertCliff_0 = "Assets/Resources/Prefabs/Rocks/DesertCliff_0.prefab";
    public const string DesertCliff_1 = "Assets/Resources/Prefabs/Rocks/DesertCliff_1.prefab";
    public const string DesertCliff_2 = "Assets/Resources/Prefabs/Rocks/DesertCliff_2.prefab";
    public const string DesertCliff_3 = "Assets/Resources/Prefabs/Rocks/DesertCliff_3.prefab";
    public const string DesertCliff_4 = "Assets/Resources/Prefabs/Rocks/DesertCliff_4.prefab";
    public const string DesertHighCliff_0 = "Assets/Resources/Prefabs/Rocks/DesertHighCliff_0.prefab";
    public const string DesertHighCliff_1 = "Assets/Resources/Prefabs/Rocks/DesertHighCliff_1.prefab";
    public const string DesertHighCliff_2 = "Assets/Resources/Prefabs/Rocks/DesertHighCliff_2.prefab";
    public const string DesertRock_1 = "Assets/Resources/Prefabs/Rocks/DesertRock_1.prefab";
    public const string DesertRock_2 = "Assets/Resources/Prefabs/Rocks/DesertRock_2.prefab";
    public const string DesertRock_3 = "Assets/Resources/Prefabs/Rocks/DesertRock_3.prefab";
    public const string DesertRock_4 = "Assets/Resources/Prefabs/Rocks/DesertRock_4.prefab";
    public const string DesertRock_5 = "Assets/Resources/Prefabs/Rocks/DesertRock_5.prefab";
    public const string DesertRock_6 = "Assets/Resources/Prefabs/Rocks/DesertRock_6.prefab";
    public const string DesertRock_7 = "Assets/Resources/Prefabs/Rocks/DesertRock_7.prefab";
    public const string DesertRock_8 = "Assets/Resources/Prefabs/Rocks/DesertRock_8.prefab";
    public const string DesertRock_9 = "Assets/Resources/Prefabs/Rocks/DesertRock_9.prefab";
    public const string RubbleDense_1 = "Assets/Resources/Prefabs/Rocks/RubbleDense_1.prefab";
    public const string RubbleDense_2 = "Assets/Resources/Prefabs/Rocks/RubbleDense_2.prefab";
    public const string RubbleDense_3 = "Assets/Resources/Prefabs/Rocks/RubbleDense_3.prefab";
    public const string RubbleSparse_1 = "Assets/Resources/Prefabs/Rocks/RubbleSparse_1.prefab";
    public const string RubbleSparse_2 = "Assets/Resources/Prefabs/Rocks/RubbleSparse_2.prefab";
    public const string RubbleSparse_3 = "Assets/Resources/Prefabs/Rocks/RubbleSparse_3.prefab";
    public const string Datepalm1 = "Assets/Resources/Prefabs/Trees/Datepalm1.prefab";
    public const string Datepalm2 = "Assets/Resources/Prefabs/Trees/Datepalm2.prefab";
    public const string Datepalm3 = "Assets/Resources/Prefabs/Trees/Datepalm3.prefab";
    public const string Datepalm4 = "Assets/Resources/Prefabs/Trees/Datepalm4.prefab";
    public const string Datepalm5 = "Assets/Resources/Prefabs/Trees/Datepalm5.prefab";
    public const string Datepalm6 = "Assets/Resources/Prefabs/Trees/Datepalm6.prefab";
    public const string Deadtree1 = "Assets/Resources/Prefabs/Trees/Deadtree1.prefab";
    public const string Deadtree10 = "Assets/Resources/Prefabs/Trees/Deadtree10.prefab";
    public const string Deadtree11 = "Assets/Resources/Prefabs/Trees/Deadtree11.prefab";
    public const string Deadtree12 = "Assets/Resources/Prefabs/Trees/Deadtree12.prefab";
    public const string Deadtree2 = "Assets/Resources/Prefabs/Trees/Deadtree2.prefab";
    public const string Deadtree3 = "Assets/Resources/Prefabs/Trees/Deadtree3.prefab";
    public const string Deadtree4 = "Assets/Resources/Prefabs/Trees/Deadtree4.prefab";
    public const string Deadtree5 = "Assets/Resources/Prefabs/Trees/Deadtree5.prefab";
    public const string Deadtree6 = "Assets/Resources/Prefabs/Trees/Deadtree6.prefab";
    public const string Deadtree7 = "Assets/Resources/Prefabs/Trees/Deadtree7.prefab";
    public const string Deadtree8 = "Assets/Resources/Prefabs/Trees/Deadtree8.prefab";
    public const string Deadtree9 = "Assets/Resources/Prefabs/Trees/Deadtree9.prefab";
    public const string Olivebush1 = "Assets/Resources/Prefabs/Trees/Olivebush1.prefab";
    public const string Olivebush2 = "Assets/Resources/Prefabs/Trees/Olivebush2.prefab";
    public const string Olivebush3 = "Assets/Resources/Prefabs/Trees/Olivebush3.prefab";
    public const string Olivetree1 = "Assets/Resources/Prefabs/Trees/Olivetree1.prefab";
    public const string Olivetree2 = "Assets/Resources/Prefabs/Trees/Olivetree2.prefab";
    public const string Olivetree3 = "Assets/Resources/Prefabs/Trees/Olivetree3.prefab";
    public const string Olivetree4 = "Assets/Resources/Prefabs/Trees/Olivetree4.prefab";
    public const string Olivetree5 = "Assets/Resources/Prefabs/Trees/Olivetree5.prefab";
    public const string Palm1 = "Assets/Resources/Prefabs/Trees/Palm1.prefab";
    public const string Palm2 = "Assets/Resources/Prefabs/Trees/Palm2.prefab";
    public const string Palm3 = "Assets/Resources/Prefabs/Trees/Palm3.prefab";
    public const string Palm4 = "Assets/Resources/Prefabs/Trees/Palm4.prefab";
    public const string Palm5 = "Assets/Resources/Prefabs/Trees/Palm5.prefab";
    public const string Palm6 = "Assets/Resources/Prefabs/Trees/Palm6.prefab";
    public const string Water = "Assets/Resources/Prefabs/Water/Water.prefab";
    public const string WaterFall01 = "Assets/Resources/Prefabs/Water/WaterFall01.prefab";
    public const string WaterFall02 = "Assets/Resources/Prefabs/Water/WaterFall02.prefab";

    private static readonly Dictionary<string, string> _map = new Dictionary<string, string>
    {
        { "MainCamera", MainCamera },
        { "PlayerFollowCamera", PlayerFollowCamera },
        { "Player", Player },
        { "Player_Unpack", Player_Unpack },
        { "Environment_Manager", Environment_Manager },
        { "Dust", Dust },
        { "GodRays", GodRays },
        { "HeatDistortion", HeatDistortion },
        { "Stars", Stars },
        { "Terrain", Terrain },
        { "WaterSplash", WaterSplash },
        { "Branches", Branches },
        { "Buttercup1", Buttercup1 },
        { "Buttercup2", Buttercup2 },
        { "Buttercup3", Buttercup3 },
        { "Buttercup4", Buttercup4 },
        { "Lillypads1", Lillypads1 },
        { "Lillypads2", Lillypads2 },
        { "Lillypads3", Lillypads3 },
        { "Malcomia_grass", Malcomia_grass },
        { "Malcomia_white1", Malcomia_white1 },
        { "Malcomia_white3", Malcomia_white3 },
        { "Malcomia_white4", Malcomia_white4 },
        { "Malcomia_white5", Malcomia_white5 },
        { "Malcomia1", Malcomia1 },
        { "Malcomia3", Malcomia3 },
        { "Malcomia4", Malcomia4 },
        { "Malcomia5", Malcomia5 },
        { "Perennial1", Perennial1 },
        { "Perennial2", Perennial2 },
        { "Perennial3", Perennial3 },
        { "Reeds1", Reeds1 },
        { "Reeds2", Reeds2 },
        { "Reeds3", Reeds3 },
        { "Reeds4", Reeds4 },
        { "Reeds5", Reeds5 },
        { "DesertArch_0", DesertArch_0 },
        { "DesertArch_1", DesertArch_1 },
        { "DesertArch_2", DesertArch_2 },
        { "DesertBoulder_0", DesertBoulder_0 },
        { "DesertBoulder_1", DesertBoulder_1 },
        { "DesertBoulder_2", DesertBoulder_2 },
        { "DesertBoulder_3", DesertBoulder_3 },
        { "DesertBoulder_4", DesertBoulder_4 },
        { "DesertBoulder_5", DesertBoulder_5 },
        { "DesertBoulder_6", DesertBoulder_6 },
        { "DesertBoulder_7", DesertBoulder_7 },
        { "DesertBoulder_8", DesertBoulder_8 },
        { "DesertBoulder_9", DesertBoulder_9 },
        { "DesertCliff_0", DesertCliff_0 },
        { "DesertCliff_1", DesertCliff_1 },
        { "DesertCliff_2", DesertCliff_2 },
        { "DesertCliff_3", DesertCliff_3 },
        { "DesertCliff_4", DesertCliff_4 },
        { "DesertHighCliff_0", DesertHighCliff_0 },
        { "DesertHighCliff_1", DesertHighCliff_1 },
        { "DesertHighCliff_2", DesertHighCliff_2 },
        { "DesertRock_1", DesertRock_1 },
        { "DesertRock_2", DesertRock_2 },
        { "DesertRock_3", DesertRock_3 },
        { "DesertRock_4", DesertRock_4 },
        { "DesertRock_5", DesertRock_5 },
        { "DesertRock_6", DesertRock_6 },
        { "DesertRock_7", DesertRock_7 },
        { "DesertRock_8", DesertRock_8 },
        { "DesertRock_9", DesertRock_9 },
        { "RubbleDense_1", RubbleDense_1 },
        { "RubbleDense_2", RubbleDense_2 },
        { "RubbleDense_3", RubbleDense_3 },
        { "RubbleSparse_1", RubbleSparse_1 },
        { "RubbleSparse_2", RubbleSparse_2 },
        { "RubbleSparse_3", RubbleSparse_3 },
        { "Datepalm1", Datepalm1 },
        { "Datepalm2", Datepalm2 },
        { "Datepalm3", Datepalm3 },
        { "Datepalm4", Datepalm4 },
        { "Datepalm5", Datepalm5 },
        { "Datepalm6", Datepalm6 },
        { "Deadtree1", Deadtree1 },
        { "Deadtree10", Deadtree10 },
        { "Deadtree11", Deadtree11 },
        { "Deadtree12", Deadtree12 },
        { "Deadtree2", Deadtree2 },
        { "Deadtree3", Deadtree3 },
        { "Deadtree4", Deadtree4 },
        { "Deadtree5", Deadtree5 },
        { "Deadtree6", Deadtree6 },
        { "Deadtree7", Deadtree7 },
        { "Deadtree8", Deadtree8 },
        { "Deadtree9", Deadtree9 },
        { "Olivebush1", Olivebush1 },
        { "Olivebush2", Olivebush2 },
        { "Olivebush3", Olivebush3 },
        { "Olivetree1", Olivetree1 },
        { "Olivetree2", Olivetree2 },
        { "Olivetree3", Olivetree3 },
        { "Olivetree4", Olivetree4 },
        { "Olivetree5", Olivetree5 },
        { "Palm1", Palm1 },
        { "Palm2", Palm2 },
        { "Palm3", Palm3 },
        { "Palm4", Palm4 },
        { "Palm5", Palm5 },
        { "Palm6", Palm6 },
        { "Water", Water },
        { "WaterFall01", WaterFall01 },
        { "WaterFall02", WaterFall02 },
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
