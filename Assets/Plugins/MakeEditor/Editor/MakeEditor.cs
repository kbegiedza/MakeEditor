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
        private const string _overrideMessageFormat = "Are you sure you want to override {0} ?";

        private static readonly EditorDialog _overrideDialog = new EditorDialog("Override script?", "Yes", "No");
        private static readonly string _editorTemplatePath;

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
        public static bool ValidateCreateEditorScriptsForSelection()
        {
            return !EditorApplication.isCompiling 
                    && Selection.GetFiltered<MonoScript>(SelectionMode.Assets)
                                .Length > 0;
        }

        [MenuItem("Assets/Create/C# Editor script", priority = 80)]
        public static void CreateEditorScriptsForSelection()
        {
            var selectedScripts = Selection.GetFiltered<MonoScript>(SelectionMode.Assets);
            CreateEditorScripts(selectedScripts);
        }

        public static void CreateEditorScripts(params MonoScript[] selectedScripts)
        {
            selectedScripts.ThrowIfNull(nameof(selectedScripts));

            if (selectedScripts.Length <= 0)
            {
                return;
            }

            using (new ReloadAssembliesLock())
            {
                string[] editorScriptTemplate = File.ReadAllLines(_editorTemplatePath);
                string lastCreatedScriptPath = null;

                foreach (var script in selectedScripts)
                {
                    // deep copy to prevent template modification
                    var editorScriptCode = editorScriptTemplate.ToList();

                    lastCreatedScriptPath = CreateEditorScript(script, editorScriptCode);
                }

                AssetDatabase.Refresh();
                SelectLastCreatedAsset(lastCreatedScriptPath);
            }
        }

        private static string CreateEditorScript(MonoScript sourceScript, List<string> editorScriptCode)
        {
            var sourceScriptPath = AssetDatabase.GetAssetPath(sourceScript);
            var sourceAssemblyPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(sourceScriptPath);

            var scriptSavePath = PathUtility.GetEditorScriptPath(sourceAssemblyPath, sourceScriptPath);

            if (File.Exists(scriptSavePath) &&
                !IsOverrideAllowed(scriptSavePath))
            {
                return null;
            }

            EditorScriptGenerator.CreateEditorScript(editorScriptCode, scriptSavePath, sourceScript);

            bool isEditorAssemblyRequired = sourceAssemblyPath != null;
            if (isEditorAssemblyRequired)
            {
                AssemblyDefinitionGenerator.UpdateOrCreateAssemblyDefinitionAsset(sourceAssemblyPath, scriptSavePath);
            }

            return scriptSavePath;
        }

        private static void SelectLastCreatedAsset(string assetPath)
        {
            var lastCreatedObject = AssetDatabase.LoadAssetAtPath<UnityObject>(assetPath);
            if (lastCreatedObject)
            {
                Selection.activeObject = lastCreatedObject;
            }
        }

        private static bool IsOverrideAllowed(string scriptPath)
        {
            var message = string.Format(_overrideMessageFormat, scriptPath);
            var selectedOption = _overrideDialog.Show(message);

            return selectedOption == EditorDialog.Option.Accepted;
        }
    }
}