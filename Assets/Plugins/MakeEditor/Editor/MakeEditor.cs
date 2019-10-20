using System;
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
                string[] editorScriptTemplate = File.ReadAllLines(_editorTemplatePath);
                string lastCreatedScriptPath = null;

                foreach (var script in selectedScripts)
                {
                    var scriptPath = AssetDatabase.GetAssetPath(script);
                    var assemblyPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(scriptPath);

                    var scriptSavePath = PathUtility.GetEditorScriptPath(assemblyPath, scriptPath);
                    if (!ShowDialogIsOverrideAllowed(scriptSavePath))
                    {
                        continue;
                    }

                    //deep copy to prevent template modifications
                    var editorScriptCode = editorScriptTemplate.ToList();
                    var relatedScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                    EditorScriptGenerator.CreateEditorScript(editorScriptCode, scriptSavePath, relatedScript);

                    bool isEditorAssemblyRequired = assemblyPath != null;
                    if (isEditorAssemblyRequired)
                    {
                        AssemblyDefinitionGenerator.UpdateOrCreateAssemblyDefinitionAsset(assemblyPath, scriptSavePath);
                    }

                    lastCreatedScriptPath = scriptSavePath;
                }

                AssetDatabase.Refresh();
                SelectLastCreatedAsset(lastCreatedScriptPath);
            }
        }

        private static void SelectLastCreatedAsset(string assetPath)
        {
            var lastCreatedObject = AssetDatabase.LoadAssetAtPath<UnityObject>(assetPath);
            if (lastCreatedObject)
            {
                Selection.activeObject = lastCreatedObject;
            }
        }

        private static bool ShowDialogIsOverrideAllowed(string scriptPath)
        {
            if (!File.Exists(scriptPath))
            {
                return true;
            }

            var message = string.Format(_overrideMessageFormat, scriptPath);
            var selectedOption = _overrideDialog.Show(message);

            return selectedOption == EditorDialog.Option.Accepted;
        }
    }
}