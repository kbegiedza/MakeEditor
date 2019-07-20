﻿using System;
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

        private static readonly EditorDialog _overrideDialog;
        private static readonly string _editorTemplatePath;

        static MakeEditor()
        {
            _overrideDialog = new EditorDialog("Override script?", "Yes", "No");

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
            return !EditorApplication.isCompiling 
                    && Selection.GetFiltered<MonoScript>(SelectionMode.Assets)
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
                string[] editorScriptTemplate = File.ReadAllLines(_editorTemplatePath);
                string lastCreatedScriptPath = null;

                foreach (var selectedScript in selectedScripts)
                {
                    //deep copy to prevent template modifications
                    var newScriptCode = editorScriptTemplate.ToList();
                    var selectedScriptPath = AssetDatabase.GetAssetPath(selectedScript);

                    var subjectAssemblyDefinitionPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(selectedScriptPath);
                    bool isEditorAssemblyRequired = subjectAssemblyDefinitionPath != null;

                    var rootPath = isEditorAssemblyRequired 
                        ? Path.GetDirectoryName(subjectAssemblyDefinitionPath)
                        : PathUtility.AssetsFolder;
                    var scriptSavePath = PathUtility.GetEditorScriptPath(rootPath, selectedScriptPath);

                    if (!IsOverrideAllowed(scriptSavePath))
                    {
                        continue;
                    }

                    CodeGenerator.CreateEditorScriptAsset(newScriptCode, selectedScriptPath, scriptSavePath);
                    lastCreatedScriptPath = scriptSavePath;

                    if (isEditorAssemblyRequired)
                    {
                        AssemblyDefinitionGenerator.UpdateOrCreateAssemblyDefinitionAsset(subjectAssemblyDefinitionPath, scriptSavePath);
                    }
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

        private static bool IsOverrideAllowed(string scriptPath)
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