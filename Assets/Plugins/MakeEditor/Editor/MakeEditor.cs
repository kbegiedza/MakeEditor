using System;
using System.IO;
using System.Linq;
using UnityEditor;
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
                string lastCreatedAssetPath = null;

                foreach (var selectedScript in selectedScripts)
                {
                    //deep copy to prevent template modifications
                    var newScriptCode = editorScriptTemplate.ToList();
                    var selectedScriptPath = AssetDatabase.GetAssetPath(selectedScript);

                    lastCreatedAssetPath = CodeGenerator.CreateEditorScriptAsset(newScriptCode, selectedScriptPath);
                }

                AssetDatabase.Refresh();
                SelectLastCreatedAsset(lastCreatedAssetPath);
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
    }
}