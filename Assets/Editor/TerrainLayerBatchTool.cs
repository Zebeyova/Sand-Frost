using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    public class TerrainLayerBatchTool : EditorWindow
    {
        [SerializeField] private DefaultAsset targetFolder;
        [SerializeField] private string targetLayer = "SandFrostGround";
        private Vector2 _scrollPosition;

        [MenuItem("Tools/ZLibTools/Terrain Layer Batch Tool")]
        public static void OpenWindow()
        {
            GetWindow<TerrainLayerBatchTool>("Terrain Layer Batch");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Terrain Layer Batch Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Batch modify Unity Terrain GameObject Layers " + "in WorldStreamer2 generated scenes.", MessageType.Info);
            EditorGUILayout.Space(10);

            targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Scene Folder", targetFolder, typeof(DefaultAsset), false);
            targetLayer = EditorGUILayout.TextField("Target Layer", targetLayer);

            EditorGUILayout.Space(10);

            string folderPath = targetFolder ? AssetDatabase.GetAssetPath(targetFolder) : "";
            bool validFolder = !string.IsNullOrEmpty(folderPath) && AssetDatabase.IsValidFolder(folderPath);

            if (!validFolder)
            {
                EditorGUILayout.HelpBox("Please select the folder containing " + "your generated Terrain scenes.", MessageType.Warning);
            }

            int layerIndex = LayerMask.NameToLayer(targetLayer);
            if (!string.IsNullOrEmpty(targetLayer) && layerIndex == -1)
            {
                EditorGUILayout.HelpBox("Layer '" + targetLayer + "' does not exist. Create it in Tags and Layers first.", MessageType.Error);
            }
            EditorGUILayout.Space(10);

            GUI.enabled = validFolder;
            if (GUILayout.Button("Scan Scenes", GUILayout.Height(32)))
            {
                ScanScenes(folderPath);
            }
            GUI.enabled = validFolder && layerIndex >= 0;

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.65f, 0.9f, 0.7f);
            if (GUILayout.Button("Apply Layer To All Terrains", GUILayout.Height(40)))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Confirm Batch Modification",
                    "This will modify and save all Terrain " +
                    "GameObject Layers in the selected scene folder.\n\n" +
                    "Target Layer: " + targetLayer +
                    "\n\nPlease make sure your scenes are backed up.",
                    "Apply",
                    "Cancel");

                if (confirmed) ApplyToScenes(folderPath, layerIndex);
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Only Terrain GameObject Layers are changed. " +
                "TerrainData, heightmaps, textures, trees and " +
                "WorldStreamer2 settings are not intentionally modified.",
                MessageType.None);
        }


        private void ScanScenes(string folderPath)
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { folderPath });

                int terrainCount = 0;
                foreach (string guid in sceneGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                    terrainCount += GetTerrainsInScene(scene).Length;
                }

                EditorUtility.DisplayDialog("Scan Completed", "Scenes found: " + sceneGuids.Length +
                                                              "\nTerrain objects found: " + terrainCount, "OK");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(
                    originalSetup
                );
            }
        }


        private void ApplyToScenes(string folderPath, int layerIndex)
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { folderPath });

            if (sceneGuids.Length == 0)
            {
                EditorUtility.DisplayDialog("No Scenes Found", "No Unity scenes were found in the selected folder.", "OK");
                return;
            }

            SceneSetup[] originalSetup =
                EditorSceneManager.GetSceneManagerSetup();

            int modifiedScenes = 0;
            int modifiedTerrains = 0;
            int unchangedTerrains = 0;
            int failedScenes = 0;

            List<string> errors = new List<string>();

            try
            {
                for (int i = 0; i < sceneGuids.Length; i++)
                {
                    string path =
                        AssetDatabase.GUIDToAssetPath(sceneGuids[i]);

                    EditorUtility.DisplayProgressBar("Updating Terrain Layers", path, (float)i / sceneGuids.Length);

                    try
                    {
                        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                        Terrain[] terrains = GetTerrainsInScene(scene);
                        bool sceneChanged = false;

                        foreach (Terrain terrain in terrains)
                        {
                            if (terrain.gameObject.layer == layerIndex)
                            {
                                unchangedTerrains++;
                                continue;
                            }

                            Undo.RecordObject(terrain.gameObject, "Set Terrain Layer");

                            terrain.gameObject.layer = layerIndex;
                            EditorUtility.SetDirty(terrain.gameObject);

                            modifiedTerrains++;
                            sceneChanged = true;
                        }

                        if (sceneChanged)
                        {
                            EditorSceneManager.MarkSceneDirty(scene);

                            if (!EditorSceneManager.SaveScene(scene))
                            {
                                throw new Exception("Failed to save scene.");
                            }

                            modifiedScenes++;
                        }
                    }
                    catch (Exception e)
                    {
                        failedScenes++;

                        errors.Add(path + "\n" + e.Message);
                        Debug.LogError("[Terrain Layer Batch] " + path + "\n" + e);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                AssetDatabase.Refresh();
            }

            string report =
                "Completed!\n\n" +
                "Scenes scanned: " + sceneGuids.Length + "\n" +
                "Scenes modified: " + modifiedScenes + "\n" +
                "Terrains modified: " + modifiedTerrains + "\n" +
                "Terrains unchanged: " + unchangedTerrains + "\n" +
                "Failed scenes: " + failedScenes;

            Debug.Log("[Terrain Layer Batch]\n" + report + "\n" + string.Join("\n\n", errors));
            EditorUtility.DisplayDialog("Batch Result", report + (errors.Count > 0 ? "\n\nSee Console for error details." : ""), "OK");
        }

        private Terrain[] GetTerrainsInScene(Scene scene)
        {
            List<Terrain> result = new List<Terrain>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                result.AddRange(root.GetComponentsInChildren<Terrain>(true));
            }

            return result.ToArray();
        }
    }
}
