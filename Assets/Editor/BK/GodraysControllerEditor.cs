using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GodraysController))]
public class GodraysControllerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        var script = (GodraysController)target;
        serializedObject.Update();

        script.directionalLight = (Light)EditorGUILayout.ObjectField(
            "Directional Light", script.directionalLight, typeof(Light), true);
        script.affectAllChildren = EditorGUILayout.Toggle("Affect All Children", script.affectAllChildren);

        using (new EditorGUI.DisabledScope(script.affectAllChildren))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("particleSystems"), true);

        serializedObject.ApplyModifiedProperties();
    }
}
