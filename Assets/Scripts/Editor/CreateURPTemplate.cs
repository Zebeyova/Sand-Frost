using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class CreateURPTemplate
    {
        private const string ShaderTemplatePath = "Assets/Shaders/ShaderTemplates";

        [MenuItem("Assets/Create/shader/URP Shader Template")]
        public static void CreateURPShader()
        {
            CreateShaderFromTemplate("URPTemplate.shader.txt", "New URP Shader");
        }

        private static void CreateShaderFromTemplate(string templateName, string defaultShaderName)
        {
            string templatePath = Path.Combine(ShaderTemplatePath, templateName);
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"Template not found: {templatePath}");
                return;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (projectRoot != null)
            {
                string defaultDir = Path.Combine(projectRoot, "Assets/Shaders");
                string selectedPath = EditorUtility.SaveFilePanel("Save Shader As", defaultDir,
                    defaultShaderName, "shader");

                if (string.IsNullOrEmpty(selectedPath))
                    return;

                if (!selectedPath.EndsWith(".shader", System.StringComparison.OrdinalIgnoreCase))
                    selectedPath += ".shader";

                string relativePath = selectedPath.Substring(projectRoot.Length + 1).Replace('\\', '/');
                if (!relativePath.StartsWith("Assets/"))
                {
                    Debug.LogError("Shader 必须保存在 Assets 目录下。");
                    return;
                }

                string shaderName = Path.GetFileNameWithoutExtension(selectedPath);
                string templateContent = File.ReadAllText(templatePath);
                string shaderContent = templateContent.Replace("#SCRIPTNAME#", shaderName);

                // 6. 确保目录存在并写入
                string outputDir = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');
                if (!Directory.Exists(outputDir))
                    if (outputDir != null)
                        Directory.CreateDirectory(outputDir);

                File.WriteAllText(relativePath, shaderContent);
                AssetDatabase.Refresh();

                var asset = AssetDatabase.LoadAssetAtPath<Shader>(relativePath);
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }
    }
}