using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    public static class AssemblyDefinitionGenerator
    {
        public static void UpdateOrCreateAssemblyDefinitionAsset(string relatedAssemblyPath, string newEditorScriptPath)
        {
            var existingAssemblyPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(newEditorScriptPath);
            if (existingAssemblyPath == null)
            {
                throw new NotSupportedException($"Cannot create editor assembly without runtime assembly to reference");
            }

            var serializedEditorAssembly = File.ReadAllText(existingAssemblyPath);
            AssemblyDefinition existingAssembly = JsonUtility.FromJson<AssemblyDefinition>(serializedEditorAssembly);

            var editorAssemblyGuid = AssetDatabase.AssetPathToGUID(existingAssemblyPath);
            var relatedAssemblyGuid = AssetDatabase.AssetPathToGUID(relatedAssemblyPath);
            var relatedAssemblyReference = GetAssemblyGuidReference(relatedAssemblyGuid);

            bool isExisingRuntimeAssembly = !existingAssembly.IncludePlatforms.Contains(PathUtility.EditorSuffix);
            if (isExisingRuntimeAssembly)
            {
                var editorAssemblyPath = PathUtility.GetEditorAssemblyDefinitionPath(relatedAssemblyPath);
                var newAssemblyName = Path.GetFileNameWithoutExtension(editorAssemblyPath);

                AssemblyDefinition editorAssembly = CreateEditorAssemblyDefinition(newAssemblyName, relatedAssemblyReference);
                FileWriter.WriteAssemblyDefinition(editorAssemblyPath, editorAssembly);
            }
            else if (relatedAssemblyGuid != editorAssemblyGuid 
                    && !existingAssembly.References.Contains(relatedAssemblyReference) 
                    && !existingAssembly.References.Contains(existingAssembly.Name))
            {
                existingAssembly.References.Add(relatedAssemblyReference);
                FileWriter.WriteAssemblyDefinition(existingAssemblyPath, existingAssembly);
            }
        }

        private static string GetAssemblyGuidReference(string assemblyGuid)
        {
            const string guidPrefix = "GUID:";

            return string.Concat(guidPrefix, assemblyGuid);
        }

        private static AssemblyDefinition CreateEditorAssemblyDefinition(string name, string requiredGuid)
        {
            return new AssemblyDefinition(name)
            {
                References = new List<string> { requiredGuid },
                IncludePlatforms = new List<string> { PathUtility.EditorSuffix }
            };
        }
    }
}