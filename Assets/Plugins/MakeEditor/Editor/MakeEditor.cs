using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Bloodstone.MakeEditor
{
    [InitializeOnLoad]
    public static class MakeEditor
    {
        private static string _editorTemplatePath;

        static MakeEditor()
        {
            try
            {
                _editorTemplatePath = PathUtility.FindEditorTemplatePath();
            }
            catch (FileNotFoundException e)
            {
                Debug.LogError(e.Message);
            }
        }

        [MenuItem("Assets/Create/C# Editor script", priority = 80, validate = true)]
        public static bool ValidateCreateEditorScriptForSelection()
        {
            return Selection
                    .GetFiltered<MonoScript>(SelectionMode.Assets)
                    .Length > 0;
        }

        [MenuItem("Assets/Create/C# Editor script", priority = 80)]
        public static void CreateEditorScriptForSelection()
        {
            var selectedScripts = Selection.GetFiltered<MonoScript>(SelectionMode.Assets);
            CreateEditorScript(selectedScripts);
        }

        public static void CreateEditorScript(params MonoScript[] selectedScripts)
        {
            if (selectedScripts == null)
            {
                throw new ArgumentNullException(nameof(selectedScripts));
            }

            if (selectedScripts.Length <= 0)
            {
                return;
            }

            using (new ReloadAssembliesLock())
            {
                UnityObject lastCreatedObject = null;
                var editorScriptTemplate = File.ReadAllLines(_editorTemplatePath);

                foreach (var selectedScript in selectedScripts)
                {
                    //deep copy to prevent template modifications
                    var newScriptCode = editorScriptTemplate.ToList();
                    var selectedScriptPath = AssetDatabase.GetAssetPath(selectedScript);

                    lastCreatedObject = CreateScriptAsset(newScriptCode, selectedScriptPath);
                }

                if (lastCreatedObject)
                {
                    Selection.activeObject = lastCreatedObject;
                }
            }
        }

        private static UnityObject CreateScriptAsset(List<string> scriptCode, string subjectPath)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(subjectPath);
            var scriptContent = CodeGenerator.PrepareScriptContent(scriptCode, script);

            var asmPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(subjectPath);
            if (asmPath != null)
            {
                var rootPath = Path.GetDirectoryName(asmPath);
                string outputPath = PathUtility.GetScriptPath(rootPath, subjectPath);

                var requiredDirectory = Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(requiredDirectory);

                File.WriteAllText(outputPath, scriptContent);
                CodeGenerator.CreateAssembly(asmPath, outputPath);

                AssetDatabase.Refresh();

                return AssetDatabase.LoadAssetAtPath(outputPath, typeof(UnityObject));
            }
            else
            {
                var rootPath = "Assets";
                string outputPath = PathUtility.GetScriptPath(rootPath, subjectPath);

                var requiredDirectory = Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(requiredDirectory);

                File.WriteAllText(outputPath, scriptContent);
                AssetDatabase.Refresh();

                return AssetDatabase.LoadAssetAtPath(outputPath, typeof(UnityObject));
            }
        }
    }
}