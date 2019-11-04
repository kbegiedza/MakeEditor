using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    public static class AssemblyDefinitionGenerator
    {
        public static void UpdateOrCreateAssemblyDefinitionAsset(string sourceScriptAssemblyPath, string newEditorScriptPath)
        {
            sourceScriptAssemblyPath.ThrowIfNull(nameof(sourceScriptAssemblyPath));
            newEditorScriptPath.ThrowIfNull(nameof(sourceScriptAssemblyPath));

            var existingAssemblyPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(newEditorScriptPath);
            if (existingAssemblyPath == null)
            {
                throw new NotSupportedException($"Cannot create editor assembly without runtime assembly to reference");
            }

            var editorAssemblyGuid = AssetDatabase.AssetPathToGUID(existingAssemblyPath);
            var sourceAssemblyGuid = AssetDatabase.AssetPathToGUID(sourceScriptAssemblyPath);
            var sourceAssemblyReference = GetAssemblyGuidReference(sourceAssemblyGuid);

            AssemblyDefinition existingAssembly = LoadAssemblyFromPath(existingAssemblyPath);

            var editorAssemblyCreationRequired = !existingAssembly.includePlatforms.Contains(PathUtility.EditorSuffix);
            if (editorAssemblyCreationRequired)
            {
                CreateNewAssemblyDefinition(sourceScriptAssemblyPath, sourceAssemblyReference);
            }
            else if (sourceAssemblyGuid != editorAssemblyGuid
                    && !existingAssembly.references.Contains(sourceAssemblyReference)
                    && !existingAssembly.references.Contains(existingAssembly.name))
            {
                AddReferenceToAssemblyDefinition(existingAssemblyPath, sourceAssemblyReference, existingAssembly);
            }
        }

        private static AssemblyDefinition LoadAssemblyFromPath(string existingAssemblyPath)
        {
            var serializedEditorAssembly = File.ReadAllText(existingAssemblyPath);

            return JsonUtility.FromJson<AssemblyDefinition>(serializedEditorAssembly);
        }

        private static string GetAssemblyGuidReference(string assemblyGuid)
        {
            const string guidPrefix = "GUID:";

            return string.Concat(guidPrefix, assemblyGuid);
        }

        private static void CreateNewAssemblyDefinition(string sourceScriptAssemblyPath, string sourceAssemblyReference)
        {
            var editorAssemblyPath = PathUtility.GetEditorAssemblyDefinitionPath(sourceScriptAssemblyPath);
            var assemblyDefinition = CreateNewEditorAssemblyDefinition(editorAssemblyPath, sourceAssemblyReference);

            FileWriter.WriteAssemblyDefinition(editorAssemblyPath, assemblyDefinition);
        }

        private static void AddReferenceToAssemblyDefinition(string existingAssemblyPath, string sourceAssemblyReference, AssemblyDefinition existingAssembly)
        {
            existingAssembly.references.Add(sourceAssemblyReference);

            FileWriter.WriteAssemblyDefinition(existingAssemblyPath, existingAssembly);
        }

        private static AssemblyDefinition CreateNewEditorAssemblyDefinition(string editorAssemblyPath, string requiredReference)
        {
            var newAssemblyName = Path.GetFileNameWithoutExtension(editorAssemblyPath);

            return new AssemblyDefinition(newAssemblyName)
            {
                references = new List<string> { requiredReference },
                includePlatforms = new List<string> { PathUtility.EditorSuffix }
            };
        }
    }
}