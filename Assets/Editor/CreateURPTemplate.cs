using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Editor
{
    public static class CreateURPTemplate
    {
        private const string ShaderTemplatePath = "Assets/Shaders/ShaderTemplates";

        [MenuItem("Assets/Create/ShaderTemplate/URP Unlit Shader", false, 80)]
        private static void CreateUnlitShader()
        {
            CreateShaderFromTemplate("URPTemplate_UnLit.template", "New UnLit Shader.shader");
        }

        [MenuItem("Assets/Create/ShaderTemplate/URP Lit Shader", false, 81)]
        private static void CreateLitShader()
        {
            CreateShaderFromTemplate("URPTemplate_Lit.template", "New Lit Shader.shader");
        }

        private static void CreateShaderFromTemplate(string templateName, string defaultFileName)
        {
            string templatePath = Path.Combine(ShaderTemplatePath, templateName).Replace('\\', '/');
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"Template not found: {templatePath}");
                return;
            }

            string folderPath = GetSelectedFolderPath();
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{defaultFileName}");
            Texture2D icon = EditorGUIUtility.IconContent("Shader Icon").image as Texture2D;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0, CreateShaderAssetAction.Create(templatePath),
                assetPath, icon, null);
        }

        private static string GetSelectedFolderPath()
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (AssetDatabase.IsValidFolder(selectedPath))
                return selectedPath;

            string parentPath = Path.GetDirectoryName(selectedPath)?.Replace('\\', '/');
            return !string.IsNullOrEmpty(parentPath) && AssetDatabase.IsValidFolder(parentPath) ? parentPath : "Assets";
        }

        public class CreateShaderAssetAction : EndNameEditAction
        {
            [SerializeField] private string templatePath;

            public static CreateShaderAssetAction Create(string templatePath)
            {
                var action = CreateInstance<CreateShaderAssetAction>();
                action.templatePath = templatePath;
                return action;
            }

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                string shaderName = Path.GetFileNameWithoutExtension(pathName);
                string shaderContent = File.ReadAllText(templatePath).Replace("#SHADERNAME#", shaderName);

                File.WriteAllText(pathName, shaderContent);
                AssetDatabase.ImportAsset(pathName);

                Object asset = AssetDatabase.LoadAssetAtPath<Shader>(pathName);
                ProjectWindowUtil.ShowCreatedAsset(asset);
            }
        }
    }
}
