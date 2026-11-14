using Terrain;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    static class SandTerrainMenu
    {
        [MenuItem("Tools/Sand & Frost/Create Sand Terrain", priority = 0)]
        static void CreateSandTerrain()
        {
            var go = new GameObject("SandTerrain");
            Undo.RegisterCreatedObjectUndo(go, "Create Sand Terrain");

            var generator = go.AddComponent<SandTerrainGenerator>();
            generator.Rebuild();

            Selection.activeGameObject = go;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
    }
}
