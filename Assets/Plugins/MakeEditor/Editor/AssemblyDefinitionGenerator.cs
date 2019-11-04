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
        public static void UpdateOrCreateAssemblyDefinitionAsset(string sourceAssemblyPath, string newEditorScriptPath)
        {
            sourceAssemblyPath.ThrowIfNull(nameof(sourceAssemblyPath));
            newEditorScriptPath.ThrowIfNull(nameof(sourceAssemblyPath));

            var existingAssemblyPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(newEditorScriptPath);
            if (existingAssemblyPath == null)
            {
                throw new NotSupportedException($"Cannot create editor assembly without runtime assembly to reference");
            }

            var editorAssemblyGuid = AssetDatabase.AssetPathToGUID(existingAssemblyPath);
            var sourceAssemblyGuid = AssetDatabase.AssetPathToGUID(sourceAssemblyPath);
            var sourceAssemblyReference = GetAssemblyGuidReference(sourceAssemblyGuid);

            AssemblyDefinition existingAssembly = LoadAssemblyFromPath(existingAssemblyPath);

            var editorAssemblyCreationRequired = !existingAssembly.includePlatforms.Contains(PathUtility.EditorSuffix);
            if (editorAssemblyCreationRequired)
            {
                CreateNewAssemblyDefinition(sourceAssemblyPath, sourceAssemblyReference);
            }
            else if (IsAddReferenceRequired())
            {
                AddReferenceToAssemblyDefinition(existingAssemblyPath, sourceAssemblyReference, existingAssembly);
            }

            bool IsAddReferenceRequired()
            {
                return sourceAssemblyGuid != editorAssemblyGuid
                        && !existingAssembly.references.Contains(sourceAssemblyReference)
                        && !existingAssembly.references.Contains(existingAssembly.name);
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