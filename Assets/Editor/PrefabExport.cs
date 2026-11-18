using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class PrefabExporter : EditorWindow
    {
        private readonly List<string> _prefabPaths = new();
        private Vector2 _scrollPos;
        private string _scanFolder;
        private string _exportFilePath;

        private const string ScanFolderPrefKey = "PrefabExporter_ScanFolder";
        private const string ExportPathPrefKey = "PrefabExporter_ExportFilePath";

        private const string DefaultScanFolder = "Assets";
        private const string DefaultExportPath = "Assets/Scripts/Base/PrefabManager.cs";

        // C# 关键字，用于给非法变量名加 @ 前缀
        private static readonly HashSet<string> Keywords = new()
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit",
            "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in",
            "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator",
            "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte",
            "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
            "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
            "volatile", "while"
        };

        [MenuItem("Tools/Prefab Exporter")]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefabExporter>("Prefab Exporter");
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        private void OnEnable()
        {
            _scanFolder = EditorPrefs.GetString(ScanFolderPrefKey, DefaultScanFolder);
            _exportFilePath = EditorPrefs.GetString(ExportPathPrefKey, DefaultExportPath);
            RefreshPrefabList();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("设置", EditorStyles.boldLabel);

            // ---- 扫描目录 ----
            EditorGUILayout.BeginHorizontal();
            _scanFolder = EditorGUILayout.TextField("扫描目录:", _scanFolder);
            if (GUILayout.Button("选择", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFolderPanel("选择要扫描的目录", _scanFolder, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    string relative = ToProjectRelativePath(selected);
                    if (relative != null)
                    {
                        _scanFolder = relative;
                        EditorPrefs.SetString(ScanFolderPrefKey, _scanFolder);
                        RefreshPrefabList();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            // ---- 导出路径 ----
            EditorGUILayout.BeginHorizontal();
            _exportFilePath = EditorGUILayout.TextField("导出路径:", _exportFilePath);
            if (GUILayout.Button("选择", GUILayout.Width(60)))
            {
                string dir = Path.GetDirectoryName(_exportFilePath);
                string fileName = Path.GetFileNameWithoutExtension(_exportFilePath);
                string selected = EditorUtility.SaveFilePanelInProject(
                    "选择导出位置",
                    string.IsNullOrEmpty(fileName) ? "PrefabPath" : fileName,
                    "cs",
                    "选择生成的脚本保存位置",
                    dir
                );
                if (!string.IsNullOrEmpty(selected))
                {
                    _exportFilePath = selected;
                    EditorPrefs.SetString(ExportPathPrefKey, _exportFilePath);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "生成的 Instantiate 方法基于 Resources.Load，预制体必须位于 Assets/.../Resources/... 下才能在运行时加载。\n" +
                "若使用 Addressables，请自行修改生成脚本中的 Instantiate 方法。",
                MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"预制体数量: {_prefabPaths.Count}", EditorStyles.boldLabel);

            if (GUILayout.Button("刷新列表", GUILayout.Height(25)))
            {
                EditorPrefs.SetString(ScanFolderPrefKey, _scanFolder);
                RefreshPrefabList();
            }

            EditorGUILayout.Space(5);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
            foreach (var path in _prefabPaths)
            {
                EditorGUILayout.LabelField(path);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("导出 PrefabManager.cs", GUILayout.Height(40)))
            {
                ExportScript();
            }

            GUI.backgroundColor = Color.white;
        }

        /// <summary>绝对路径转项目相对路径（Assets/...）</summary>
        private string ToProjectRelativePath(string absolutePath)
        {
            absolutePath = absolutePath.Replace("\\", "/");
            string dataPath = Application.dataPath.Replace("\\", "/");
            if (absolutePath.StartsWith(dataPath))
                return "Assets" + absolutePath.Substring(dataPath.Length);

            EditorUtility.DisplayDialog("错误", "请选择项目 Assets 文件夹内的目录。", "OK");
            return null;
        }

        private void RefreshPrefabList()
        {
            _prefabPaths.Clear();

            if (!AssetDatabase.IsValidFolder(_scanFolder))
            {
                Repaint();
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { _scanFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && !path.StartsWith("Packages/"))
                {
                    _prefabPaths.Add(path);
                }
            }

            _prefabPaths.Sort();
            Repaint();
        }

        /// <summary>把预制体名清洗成合法 C# 标识符（空格/符号→下划线，数字开头加 _）</summary>
        private static string SanitizeIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unnamed";

            var sb = new StringBuilder(name.Length + 1);
            if (!char.IsLetter(name[0]) && name[0] != '_')
                sb.Append('_');

            foreach (char c in name)
            {
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }

            return sb.ToString();
        }

        private void ExportScript()
        {
            if (_prefabPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有可导出的预制体。", "OK");
                return;
            }

            string exportPath = _exportFilePath.Replace("\\", "/");
            if (!exportPath.StartsWith("Assets/"))
            {
                EditorUtility.DisplayDialog("错误", "导出路径必须以 Assets/ 开头。", "OK");
                return;
            }

            if (!exportPath.EndsWith(".cs"))
                exportPath += ".cs";

            // 目录不存在则自动创建
            string dir = Path.GetDirectoryName(exportPath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            _exportFilePath = exportPath;
            EditorPrefs.SetString(ExportPathPrefKey, exportPath);

            // ---- 先计算每个预制体的唯一字段名 ----
            var identifiers = new List<string>(_prefabPaths.Count); // 带 @ 的最终标识符
            var rawNames = new List<string>(_prefabPaths.Count); // 不带 @ 的名字（用于 _map 的 key）
            var used = new HashSet<string>(StringComparer.Ordinal);

            foreach (var path in _prefabPaths)
            {
                string raw = Path.GetFileNameWithoutExtension(path);
                string id = SanitizeIdentifier(raw);
                string unique = id;
                int i = 2;
                while (used.Contains(unique))
                {
                    unique = id + "_" + i;
                    i++;
                }

                used.Add(unique);

                identifiers.Add(Keywords.Contains(unique) ? "@" + unique : unique);
                rawNames.Add(unique);
            }

            // ---- 生成脚本 ----
            var sb = new StringBuilder();
            sb.AppendLine("// 此脚本由 PrefabExport 自动生成，请勿手动修改。");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("public static class PrefabManager");
            sb.AppendLine("{");
            for (int i = 0; i < _prefabPaths.Count; i++)
            {
                sb.AppendLine($"    public const string {identifiers[i]} = \"{_prefabPaths[i]}\";");
            }

            sb.AppendLine();
            sb.AppendLine(
                "    private static readonly Dictionary<string, string> _map = new Dictionary<string, string>");
            sb.AppendLine("    {");
            for (int i = 0; i < _prefabPaths.Count; i++)
            {
                sb.AppendLine($"        {{ \"{rawNames[i]}\", {identifiers[i]} }},");
            }

            sb.AppendLine("    };");
            sb.AppendLine();
            sb.AppendLine("    public static GameObject Instantiate(string assetPath, Transform parent = null)");
            sb.AppendLine("        => Instantiate(assetPath, Vector3.zero, Quaternion.identity, parent);");
            sb.AppendLine();
            sb.AppendLine(
                "    public static GameObject Instantiate(string assetPath, Vector3 position, Quaternion rotation, Transform parent = null)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (string.IsNullOrEmpty(assetPath))");
            sb.AppendLine("        {");
            sb.AppendLine("            Debug.LogError(\"[PrefabPath] 路径为空\");");
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        GameObject prefab = Resources.Load<GameObject>(ToResourcePath(assetPath));");
            sb.AppendLine("        if (prefab == null)");
            sb.AppendLine("        {");
            sb.AppendLine("            Debug.LogError($\"[PrefabPath] Resources 中未找到预制体: {assetPath}\");");
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        return Object.Instantiate(prefab, position, rotation, parent);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public static GameObject InstantiateByName(string prefabName, Transform parent = null)");
            sb.AppendLine("        => InstantiateByName(prefabName, Vector3.zero, Quaternion.identity, parent);");
            sb.AppendLine();
            sb.AppendLine(
                "    public static GameObject InstantiateByName(string prefabName, Vector3 position, Quaternion rotation, Transform parent = null)");
            sb.AppendLine("    {");
            sb.AppendLine(
                "        if (string.IsNullOrEmpty(prefabName) || !_map.TryGetValue(prefabName, out var path))");
            sb.AppendLine("        {");
            sb.AppendLine("            Debug.LogError($\"[PrefabPath] 未找到预制体名: {prefabName}\");");
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        return Instantiate(path, position, rotation, parent);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public static void Destroy(GameObject instance)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (instance == null) return;");
            sb.AppendLine("        if (Application.isPlaying) Object.Destroy(instance);");
            sb.AppendLine("        else Object.DestroyImmediate(instance);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public static void SetActive(GameObject instance, bool active)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (instance != null) instance.SetActive(active);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    private static string ToResourcePath(string assetPath)");
            sb.AppendLine("    {");
            sb.AppendLine("        const string resourcesFolder = \"/Resources/\";");
            sb.AppendLine("        int idx = assetPath.IndexOf(resourcesFolder, System.StringComparison.Ordinal);");
            sb.AppendLine("        if (idx < 0) return assetPath;");
            sb.AppendLine("        string sub = assetPath.Substring(idx + resourcesFolder.Length);");
            sb.AppendLine("        return System.IO.Path.ChangeExtension(sub, null).Replace('\\\\', '/');");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("导出成功", $"脚本已生成：\n{exportPath}", "OK");
        }
    }
}
