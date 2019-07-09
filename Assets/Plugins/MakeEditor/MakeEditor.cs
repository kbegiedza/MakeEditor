using System;
using System.Collections.Generic;
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
        private readonly static HashSet<string> _buildInAssemblyDefinitions = new HashSet<string>
        {
            "Assembly-CSharp-firstpass",
            "Assembly-CSharp-Editor-firstpass",
            "Assembly-CSharp",
            "Assembly-CSharp-Editor",
        };

        private static string _editorTemplate;

        static MakeEditor()
        {
            //todo: add error handling
            var pluginAsmdef = AssetDatabase.FindAssets($"t:asmdef Bloodstone.MakeEditor")[0];
            var pluginPath = AssetDatabase.GUIDToAssetPath(pluginAsmdef);

            //todo: add error handling
            _editorTemplate = Path.Combine(Path.GetDirectoryName(pluginPath), "Templates", "editor_template.txt");
        }

        [MenuItem("Assets/Create/C# Editor script", priority = 80)]
        public static void CreateEditorScript()
        {
            var selection = Selection.GetFiltered<MonoScript>(SelectionMode.Assets);

            UnityEngine.Object lastCreatedObject = null;

            foreach (var selected in selection)
            {
                var selectedPath = AssetDatabase.GetAssetPath(selected);

                var dirPath = Path.GetDirectoryName(selectedPath);
                var path = Path.Combine(dirPath, "Editor", $"{selected.name}Editor.cs");

                if (File.Exists(path))
                {
                    path = GenerateNotExistingName(path);
                }

                var requiredDirectory = Path.GetDirectoryName(path);
                Directory.CreateDirectory(requiredDirectory);

                lastCreatedObject = CreateScriptAsset(selectedPath, path);
            }

            if(lastCreatedObject != null)
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

        private static UnityEngine.Object CreateScriptAsset(string subjectPath, string outputPath)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(subjectPath);
            if (script == null)
            {
                Debug.LogError("Select valid MonoScript object");

                return null;
            }

            var scriptContent = PrepareScriptContent(_editorTemplate, subjectPath, script, outputPath);

            File.WriteAllText(outputPath, scriptContent);

            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath(outputPath, typeof(UnityEngine.Object));
        }

        private static string PrepareScriptContent(string template, string targetPath , MonoScript s, string outpath)
        {
            DoShitWithAssembly(s, targetPath, outpath);

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

        private static void DoShitWithAssembly(MonoScript s, string sPath, string outPath)
        {
            var asmPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(sPath);

            //script is in default asmdef
            if(asmPath == null)
            {
                return;
            }

            Debug.Log("Src asm: " + asmPath);

            var outasmPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(outPath);
            if(outasmPath != null)
            {
                // asmdef creation required
            }

            // check is asmdef valid
            // * is editor
            // * is asmPath referenced
        }
    }
}