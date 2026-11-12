using UnityEditor;
using UnityEngine;

namespace SandAndFrost.World.EditorTools
{
    static class SandTerrainMenu
    {
        [MenuItem("Tools/Sand & Frost/Create Sand Terrain", priority = 0)]
        static void CreateSandTerrain()
        {
            var go = new GameObject("SandTerrain");
            Undo.RegisterCreatedObjectUndo(go, "Create Sand Terrain");

            // RequireComponent 会顺带补上 MeshFilter / MeshRenderer / MeshCollider。
            var generator = go.AddComponent<SandTerrainGenerator>();
            generator.Rebuild();

            Selection.activeGameObject = go;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
    }
}
