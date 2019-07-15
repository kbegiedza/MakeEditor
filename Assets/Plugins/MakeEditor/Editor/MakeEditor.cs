using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    [InitializeOnLoad]
    public static class MakeEditor
    {
        private static string _editorTemplatePath;

        static MakeEditor()
        {
            //todo: add error handling
            var pluginAsmdef = AssetDatabase.FindAssets($"t:asmdef Bloodstone.MakeEditor")[0];
            var pluginPath = AssetDatabase.GUIDToAssetPath(pluginAsmdef);

            //todo: add error handling
            _editorTemplatePath = Path.Combine(Path.GetDirectoryName(pluginPath), "Templates", "editor_template.txt");
        }

        [MenuItem("Assets/Create/C# Editor script", priority = 80)]
        public static void CreateEditorScript()
        {
            var selection = Selection.GetFiltered<MonoScript>(SelectionMode.Assets);

            Object lastCreatedObject = null;

            if(selection.Length > 0)
            {
                var codeTemplate = File.ReadAllLines(_editorTemplatePath).ToList();

                foreach (var selected in selection)
                {
                    var selectedPath = AssetDatabase.GetAssetPath(selected);

                    //deep copy to prevent tempalte modifications
                    var codeTemp = new List<string>(codeTemplate.Capacity);
                    foreach(var line in codeTemplate)
                    {
                        codeTemp.Add((string)line.Clone());
                    }

                    lastCreatedObject = CreateScriptAsset(codeTemplate, selectedPath);
                }

                if (lastCreatedObject != null)
                {
                    Selection.activeObject = lastCreatedObject;
                }
            }
        }

        [MenuItem("Assets/Create/C# Editor script", priority = 80, validate = true)]
        public static bool ValidateCreateEditorScript()
        {
            return Selection
                    .GetFiltered<MonoScript>(SelectionMode.Assets)
                    .Length > 0;
        }

        private static string GenerateNotExistingName(in string path)
        {
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileNameWithoutExtension(path);
            var extension = ".cs";

            var newFileName = fileName + extension;

            return Path.Combine(directory, newFileName);
        }

        private static Object CreateScriptAsset(List<string> codeGen, string subjectPath)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(subjectPath);
            if (script == null)
            {
                Debug.LogError("Select valid MonoScript object");

                return null;
            }

            var scriptContent = PrepareScriptContent(codeGen, script);

            var asmPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(subjectPath);
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
                var guidRef = AssetDatabase.AssetPathToGUID(subjectAsmDefPath);
                var requiredGuid = $"GUID:{guidRef}";

                if (!asmdef.IncludePlatforms.Contains("Editor"))
                {
                    var refName = Path.GetFileNameWithoutExtension(subjectAsmDefPath);

                    var rootPath = Path.GetDirectoryName(subjectAsmDefPath);
                    var editorPath = Path.Combine(rootPath, "Editor");

                    var editorAsmDefPath = Path.Combine(editorPath, $"{refName}.Editor.asmdef");

                    AssemblyDefinition editorAsmDef = new AssemblyDefinition
                    {
                        References = new List<string>(1) { requiredGuid },
                        Name = $"{refName}.Editor"
                    };

                    Directory.CreateDirectory(Path.GetDirectoryName(editorAsmDefPath));
                    var serializedAsmDef = JsonUtility.ToJson(editorAsmDef, true);
                    File.WriteAllText(editorAsmDefPath, serializedAsmDef);
                }
                else if(!asmdef.References.Contains(requiredGuid) && !asmdef.References.Contains(asmdef.Name))
                {
                    asmdef.References.Add(requiredGuid);
                    var serializedAsmDef = JsonUtility.ToJson(asmdef, true);
                    File.WriteAllText(outasmPath, serializedAsmDef);
                }
            }
            else
            {
                throw new System.NotSupportedException($"Cannot create editor assembly without runtime assembly to reference");
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

        private static string PrepareScriptContent(List<string> codeGen, MonoScript s)
        {
            //bench / try StringBuilder
            var type = s.GetClass();
            int namespaceIndex = codeGen.FindIndex(str => str.Contains("#NAMESPACE#"));
            if (type.Namespace != null)
            {
                for (int i = namespaceIndex + 1; i < codeGen.Count; ++i)
                {
                    if (codeGen[i].Length > 0)
                    {
                        codeGen[i] = codeGen[i].Insert(0, "\t");
                    }
                }

                string usedNamespace = codeGen[namespaceIndex].Replace("#NAMESPACE#", $"namespace {type.Namespace}");
                codeGen[namespaceIndex] = "{";
                codeGen.Insert(namespaceIndex, usedNamespace);
                codeGen.Add("}");
            }
            else
            {
                codeGen.RemoveAt(namespaceIndex);
            }

            for (int i = 0; i < codeGen.Count; ++i)
            {
                codeGen[i] = codeGen[i].Replace("#CLASS_NAME#", type.Name);
            }

            var finalCode = string.Join("\n", codeGen.ToArray());

            return finalCode;
        }
    }
}