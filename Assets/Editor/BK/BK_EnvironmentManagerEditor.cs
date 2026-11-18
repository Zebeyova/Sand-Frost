using Base;
using UnityEditor;
using UnityEngine;

namespace Editor.BK
{
    [CustomEditor(typeof(EnvironmentManager))]
    public class BK_EnvironmentManagerEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor _materialEditor;

        private SerializedProperty _overrideSunColorProp;
        private SerializedProperty _overrideFogColorProp;
        private SerializedProperty _overrideCloudColorProp;
        private SerializedProperty _overrideAmbientColorProp;

        private bool _lightingFoldout = true;
        private bool _windFoldout = true;
        private bool _grassFoldout = true;
        private bool _cloudsFoldout = true;

        private void OnEnable()
        {
            _overrideSunColorProp = serializedObject.FindProperty("overrideSunColor");
            _overrideFogColorProp = serializedObject.FindProperty("overrideFogColor");
            _overrideCloudColorProp = serializedObject.FindProperty("overrideCloudColor");
            _overrideAmbientColorProp = serializedObject.FindProperty("overrideAmbientColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(5, 5, 5, 5)
            };

            EnvironmentManager EMscript = (EnvironmentManager)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Player Spawn / 角色出生", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playerSpawnPoint"), new GUIContent("Spawn Point / 出生点"));
            SerializedProperty waitForGround = serializedObject.FindProperty("waitForSpawnGround");
            EditorGUILayout.PropertyField(waitForGround, new GUIContent("Wait For Ground / 等待地面"));
            if (waitForGround.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnGroundLayers"), new GUIContent("Spawn Ground Layers"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnGroundProbeHeight"), new GUIContent("Ground Probe Height"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnGroundClearance"), new GUIContent("Ground Clearance"));
            }
            EditorGUILayout.HelpBox("出生点请放在常驻游戏场景中。角色会等到脚下的地面碰撞加载完成后再移动；未指定出生点时使用世界原点的 X/Z", MessageType.Info);
            EditorGUILayout.EndVertical();

            // Lighting Foldout
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(boxStyle);
            _lightingFoldout = EditorGUILayout.Foldout(_lightingFoldout, "Global Lighting", foldoutStyle);
            if (_lightingFoldout)
            {
                // Directional Light property
                EditorGUILayout.PropertyField(serializedObject.FindProperty("directionalLight"), new GUIContent("Directional Light"));

                // Sun Gradient
                EditorGUILayout.BeginHorizontal();
                _overrideSunColorProp.boolValue = EditorGUILayout.ToggleLeft("Sun", _overrideSunColorProp.boolValue, GUILayout.Width(70));
                using (new EditorGUI.DisabledScope(!_overrideSunColorProp.boolValue))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sunColorGradient"), GUIContent.none);
                }
                EditorGUILayout.EndHorizontal();

                // Fog Gradient
                EditorGUILayout.BeginHorizontal();
                _overrideFogColorProp.boolValue = EditorGUILayout.ToggleLeft("Fog", _overrideFogColorProp.boolValue, GUILayout.Width(70));
                using (new EditorGUI.DisabledScope(!_overrideFogColorProp.boolValue))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColorGradient"), GUIContent.none);
                }
                EditorGUILayout.EndHorizontal();

                // Clouds Gradient
                EditorGUILayout.BeginHorizontal();
                _overrideCloudColorProp.boolValue = EditorGUILayout.ToggleLeft("Clouds", _overrideCloudColorProp.boolValue, GUILayout.Width(70));
                using (new EditorGUI.DisabledScope(!_overrideCloudColorProp.boolValue))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("scatteringColorGradient"), GUIContent.none);
                }
                Rect gradientRect = GUILayoutUtility.GetLastRect();
                EditorGUILayout.EndHorizontal();

                // Ambient Gradient
                EditorGUILayout.BeginHorizontal();
                _overrideAmbientColorProp.boolValue = EditorGUILayout.ToggleLeft("Ambient", _overrideAmbientColorProp.boolValue, GUILayout.Width(70));
                using (new EditorGUI.DisabledScope(!_overrideAmbientColorProp.boolValue))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientColorGradient"), GUIContent.none);
                }
                EditorGUILayout.EndHorizontal();

                // Day & Night icons
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(gradientRect.x + 77);
                GUILayout.Label("☼", GUILayout.Width(20));
                GUILayout.FlexibleSpace();
                GUILayout.Label("☽", GUILayout.Width(20));
                GUILayout.Space(5);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();


            // Wind Foldout
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(boxStyle);
            _windFoldout = EditorGUILayout.Foldout(_windFoldout, "Wind", foldoutStyle);
            if (_windFoldout)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("baseWindPower"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("baseWindSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("burstsPower"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("burstsSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("burstsScale"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("microPower"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("microSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("microFrequency"));
            }
            EditorGUILayout.EndVertical();

            // Grass Foldout
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(boxStyle);
            _grassFoldout = EditorGUILayout.Foldout(_grassFoldout, "Grass", foldoutStyle);
            if (_grassFoldout)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("renderDistance"));
            }
            EditorGUILayout.EndVertical();

            // Clouds Foldout
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(boxStyle);
            _cloudsFoldout = EditorGUILayout.Foldout(_cloudsFoldout, "Clouds", foldoutStyle);
            if (_cloudsFoldout)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Altitude"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("volumeSamples"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("volumeSize"));

                // Material
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudsMaterial"));

                // Add a space
                EditorGUILayout.Space(10);

                if (EMscript.cloudsMaterial != null)
                {
                    if (_materialEditor == null || _materialEditor.target != EMscript.cloudsMaterial)
                    {
                        _materialEditor = CreateEditor(EMscript.cloudsMaterial);
                    }
                    _materialEditor.DrawHeader();
                    _materialEditor.OnInspectorGUI();
                }
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
