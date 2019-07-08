using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    [InitializeOnLoad]
    public class MakeEditor
    {
        private static string _editorTemplate;

        static MakeEditor()
        {
            var pluginAsmdef = AssetDatabase.FindAssets($"t:asmdef Bloodstone.MakeEditor")[0];
            var pluginPath = AssetDatabase.GUIDToAssetPath(pluginAsmdef);

            _editorTemplate = Path.Combine(Path.GetDirectoryName(pluginPath), "Templates", "editor_template.txt");
            Debug.Log(_editorTemplate);
        }

        [MenuItem("Assets/Create/C# Editor script", priority = 80)]
        public static void CreateEditorScript()
        {
            var selected = Selection.objects;
            if (selected.Length > 1)
            {
                //use Selection Get
                throw new NotImplementedException();
                //filter-out non MonoScript stuff
                //create standard editor or for each selected?
            }
            else if (selected.Length == 1)
            {
                var firstSelected = selected[0];
                var firstSelectedPath = AssetDatabase.GetAssetPath(firstSelected);
                Debug.Log($"first Selected path: {firstSelectedPath}");

                var dirPath = Path.GetDirectoryName(firstSelectedPath);
                var path = Path.Combine(dirPath, "Editor", $"{firstSelected.name}Editor.cs");


                if (File.Exists(path))
                {
                    GenerateNotExistingName(path);
                }


                Directory.CreateDirectory(Path.GetDirectoryName(path));

                var created = CreateScriptAsset(firstSelectedPath, path);
                if (created)
                {
                    Selection.activeObject = created;
                }
            }
        }

        private static void GenerateNotExistingName(string path)
        {
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileNameWithoutExtension(path);
            var extension = ".cs";

            var newFileName = fileName + extension;

            path = Path.Combine(directory, newFileName);
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
                Debug.LogWarning("Select target script");

                return null;
            }

            var content = PrepareContent(_editorTemplate, subjectPath, script);

            File.WriteAllText(outputPath, content);

            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath(outputPath, typeof(UnityEngine.Object));
        }

        private static string PrepareContent(string template, string target, MonoScript s)
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

            //namespace
            for (int i = 0; i < code.Count; ++i)
            {
                code[i] = code[i].Replace("#CLASS_NAME#", type.Name);
            }

            var finalCode = string.Join("\n", code.ToArray());

            return finalCode;
        }
    }
}