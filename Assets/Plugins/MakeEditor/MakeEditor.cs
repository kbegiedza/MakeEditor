using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    [InitializeOnLoad]
    public class MakeEditor
    {
        //private readonly static HashSet<string> _buildInAssemblyDefinitions = new HashSet<string>
        //{
        //    "Assembly-CSharp-firstpass",
        //    "Assembly-CSharp-Editor-firstpass",
        //    "Assembly-CSharp",
        //    "Assembly-CSharp-Editor",
        //};

        private static string _editorTemplatePath;
        private static string _asmdefTemplatePath;

        static MakeEditor()
        {
            //todo: add error handling
            var pluginAsmdef = AssetDatabase.FindAssets($"t:asmdef Bloodstone.MakeEditor")[0];
            var pluginPath = AssetDatabase.GUIDToAssetPath(pluginAsmdef);

            //todo: add error handling
            _editorTemplatePath = Path.Combine(Path.GetDirectoryName(pluginPath), "Templates", "editor_template.txt");
            _asmdefTemplatePath = Path.Combine(Path.GetDirectoryName(pluginPath), "Templates", "asmdef_template.txt");
        }

        [MenuItem("Assets/Create/C# Editor script", priority = 80)]
        public static void CreateEditorScript()
        {
            var selection = Selection.GetFiltered<MonoScript>(SelectionMode.Assets);

            UnityEngine.Object lastCreatedObject = null;

            foreach (var selected in selection)
            {
                var selectedPath = AssetDatabase.GetAssetPath(selected);

                //old
                //var dirPath = Path.GetDirectoryName(selectedPath);
                //var path = Path.Combine(dirPath, "Editor", $"{selected.name}Editor.cs");

                //if (File.Exists(path))
                //{
                //    path = GenerateNotExistingName(path);
                //}

                //var requiredDirectory = Path.GetDirectoryName(path);
                //Directory.CreateDirectory(requiredDirectory);

                lastCreatedObject = CreateScriptAsset(selectedPath);
            }

            if (lastCreatedObject != null)
            {
                Selection.activeObject = lastCreatedObject;
            }
        }

        private static string GenerateNotExistingName(in string path)
        {
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileNameWithoutExtension(path);
            var extension = ".cs";

            var newFileName = fileName + extension;

            return Path.Combine(directory, newFileName);
        }

        [MenuItem("Assets/Create/C# Editor script", priority = 80, validate = true)]
        public static bool ValidateCreateEditorScript()
        {
            if (Selection.objects.Length > 0)
            {
                return Selection.objects[0].GetType() == typeof(MonoScript);
            }

            return false;
        }

        private static UnityEngine.Object CreateScriptAsset(string subjectPath)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(subjectPath);
            if (script == null)
            {
                Debug.LogError("Select valid MonoScript object");

                return null;
            }

            var scriptContent = PrepareScriptContent(_editorTemplatePath, script);

            //todo: create in parent asmdef directory - similar hierarchy to scripts
            //todo: sub asm def support
            var asmPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(subjectPath);
            //if is under asmdef
            if (asmPath != null)
            {
                var rootPath = Path.GetDirectoryName(asmPath);
                string outputPath = GetScriptPath(rootPath, subjectPath);

                var requiredDirectory = Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(requiredDirectory);

                File.WriteAllText(outputPath, scriptContent);
                AssetDatabase.Refresh();

                CreateAssembly(asmPath, outputPath);
                AssetDatabase.Refresh();

                return AssetDatabase.LoadAssetAtPath(outputPath, typeof(UnityEngine.Object));
            }
            else
            {
                var rootPath = "Assets";
                string outputPath = GetScriptPath(rootPath, subjectPath);

                var requiredDirectory = Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(requiredDirectory);

                File.WriteAllText(outputPath, scriptContent);
                AssetDatabase.Refresh();

                return AssetDatabase.LoadAssetAtPath(outputPath, typeof(UnityEngine.Object));
            }
        }

        private static void CreateAssembly(string subjectAsmDefPath, string newEditorScriptPath)
        {
            Debug.Log($"Subject assembly: {subjectAsmDefPath} for created editor script {newEditorScriptPath}");

            var outasmPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(newEditorScriptPath);
            if (outasmPath != null)
            {
                var str = File.ReadAllText(outasmPath);
                AssemblyDefinition asmdef = JsonUtility.FromJson<AssemblyDefinition>(str);
                if (!asmdef.IncludePlatforms.Contains("Editor"))
                {
                    var refName = Path.GetFileNameWithoutExtension(subjectAsmDefPath);
                    var guidRef = AssetDatabase.AssetPathToGUID(subjectAsmDefPath);

                    var rootPath = Path.GetDirectoryName(subjectAsmDefPath);
                    var editorPath = Path.Combine(rootPath, "Editor");

                    var editorAsmDef = Path.Combine(editorPath, $"{refName}.Editor.asmdef");
                    Debug.Log($"editor asmdef path: {editorAsmDef }");

                    Directory.CreateDirectory(Path.GetDirectoryName(editorAsmDef));

                    var code = File.ReadAllLines(_asmdefTemplatePath).ToList();

                    for (int i = 0; i < code.Count; ++i)
                    {
                        code[i] = code[i].Replace("#ASM_NAME#", refName);
                        code[i] = code[i].Replace("#REFERENCES#", $"\"GUID:{guidRef}\"");
                    }

                    var finalCode = string.Join("\n", code.ToArray());
                    File.WriteAllText(editorAsmDef, finalCode);
                }

                Debug.Log($"{JsonUtility.ToJson(asmdef, true)}");

                // check is asmdef valid
                // * is editor
                // * is asmPath referenced
            }
            else
            {
                // ayyy lmao wtf just happened?
            }
        }

        private static string GetScriptPath(string rootPath, string subjectPath)
        {
            var editorPath = Path.Combine(rootPath, "Editor");
            var pathMod = Path.GetDirectoryName(subjectPath.Substring(rootPath.Length + 1)); //+1 to remove '/'

            var dirPath = Path.Combine(editorPath, pathMod);
            Debug.Log($"dir path: {dirPath}");
            var name = Path.GetFileNameWithoutExtension(subjectPath);
            var outputPath = Path.Combine(dirPath, $"{name}Editor.cs");

            if (File.Exists(outputPath))
            {
                outputPath = GenerateNotExistingName(outputPath);
            }

            return outputPath;
        }

        private static string PrepareScriptContent(string template, MonoScript s)
        {
            var code = File.ReadAllLines(template).ToList();

            var type = s.GetClass();
            int namespaceIndex = code.FindIndex(str => str.Contains("#NAMESPACE#"));
            if (type.Namespace != null)
            {
                for (int i = namespaceIndex + 1; i < code.Count; ++i)
                {
                    if (code[i].Length > 0)
                    {
                        code[i] = code[i].Insert(0, "\t");
                    }
                }

                string usedNamespace = code[namespaceIndex].Replace("#NAMESPACE#", $"namespace {type.Namespace}");
                code[namespaceIndex] = "{";
                code.Insert(namespaceIndex, usedNamespace);
                code.Add("}");
            }
            else
            {
                code.RemoveAt(namespaceIndex);
            }

            for (int i = 0; i < code.Count; ++i)
            {
                code[i] = code[i].Replace("#CLASS_NAME#", type.Name);
            }

            var finalCode = string.Join("\n", code.ToArray());

            return finalCode;
        }
    }
}