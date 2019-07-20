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
        public static void UpdateOrCreateAssemblyDefinitionAsset(string subjectAsmDefPath, string newEditorScriptPath)
        {
            var existingAssemblyDefinition = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(newEditorScriptPath);
            if (existingAssemblyDefinition == null)
            {
                throw new NotSupportedException($"Cannot create editor assembly without runtime assembly to reference");
            }

            var json = File.ReadAllText(existingAssemblyDefinition);
            AssemblyDefinition asmdef = JsonUtility.FromJson<AssemblyDefinition>(json);

            var guidRef = AssetDatabase.AssetPathToGUID(existingAssemblyDefinition);
            var guidReq = AssetDatabase.AssetPathToGUID(subjectAsmDefPath);
            var requiredGuid = GetAssemblyGuidReference(guidReq);

            if (!asmdef.IncludePlatforms.Contains(PathUtility.EditorSuffix))
            {
                var refName = Path.GetFileNameWithoutExtension(subjectAsmDefPath);

                var rootPath = Path.GetDirectoryName(subjectAsmDefPath);
                var editorPath = Path.Combine(rootPath, PathUtility.EditorSuffix);

                var editorAsmDefPath = Path.Combine(editorPath, $"{refName}.{PathUtility.EditorSuffix}.{PathUtility.Extensions.AssemblyDefinition}");
                var newAssemblyName = $"{refName}.{PathUtility.EditorSuffix}";

                AssemblyDefinition editorAsmDef = CreateEditorAssemblyDefinition(newAssemblyName, requiredGuid);
                FileWriter.WriteAssemblyDefinition(editorAsmDefPath, editorAsmDef);
            }
            else if (!asmdef.References.Contains(requiredGuid) && !asmdef.References.Contains(asmdef.Name) && guidReq != guidRef)
            {
                asmdef.References.Add(requiredGuid);
                FileWriter.WriteAssemblyDefinition(existingAssemblyDefinition, asmdef);
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