using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Bloodstone.MakeEditor
{
    public static class CodeGenerator
    {
        public static UnityObject CreateEditorScriptAsset(List<string> scriptCode, string subjectPath)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(subjectPath);
            var scriptContent = PrepareScriptContent(scriptCode, script);

            var asmPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(subjectPath);
            bool isNewAssemblyRequired = asmPath != null;

            var rootPath = isNewAssemblyRequired ? Path.GetDirectoryName(asmPath) : "Assets";
            var outputPath = PathUtility.GetScriptPath(rootPath, subjectPath);
            FileWriter.WriteText(outputPath, scriptContent);

            if (isNewAssemblyRequired)
            {
                CreateEditorAssembly(asmPath, outputPath);
            }

            return AssetDatabase.LoadAssetAtPath(outputPath, typeof(UnityObject));
        }

        public static string PrepareScriptContent(List<string> scriptCode, MonoScript s)
        {
            //bench / try StringBuilder
            var type = s.GetClass();
            int namespaceIndex = scriptCode.FindIndex(str => str.Contains("#NAMESPACE#"));
            if (type.Namespace != null)
            {
                for (int i = namespaceIndex + 1; i < scriptCode.Count; ++i)
                {
                    if (scriptCode[i].Length > 0)
                    {
                        scriptCode[i] = scriptCode[i].Insert(0, "\t");
                    }
                }

                string usedNamespace = scriptCode[namespaceIndex].Replace("#NAMESPACE#", $"namespace {type.Namespace}");
                scriptCode[namespaceIndex] = "{";
                scriptCode.Insert(namespaceIndex, usedNamespace);
                scriptCode.Add("}");
            }
            else
            {
                scriptCode.RemoveAt(namespaceIndex);
            }

            for (int i = 0; i < scriptCode.Count; ++i)
            {
                scriptCode[i] = scriptCode[i].Replace("#CLASS_NAME#", type.Name);
            }

            var finalCode = string.Join("\n", scriptCode.ToArray());

            return finalCode;
        }

        public static void CreateEditorAssembly(string subjectAsmDefPath, string newEditorScriptPath)
        {
            Debug.Log($"Subject assembly: {subjectAsmDefPath} for created editor script {newEditorScriptPath}");

            var outasmPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(newEditorScriptPath);
            if (outasmPath == null)
            {
                throw new NotSupportedException($"Cannot create editor assembly without runtime assembly to reference");
            }

            var str = File.ReadAllText(outasmPath);
            AssemblyDefinition asmdef = JsonUtility.FromJson<AssemblyDefinition>(str);
            var guidRef = AssetDatabase.AssetPathToGUID(outasmPath);
            var guidReq = AssetDatabase.AssetPathToGUID(subjectAsmDefPath);
            var requiredGuid = $"GUID:{guidReq}";

            if (!asmdef.IncludePlatforms.Contains("Editor"))
            {
                var refName = Path.GetFileNameWithoutExtension(subjectAsmDefPath);

                var rootPath = Path.GetDirectoryName(subjectAsmDefPath);
                var editorPath = Path.Combine(rootPath, "Editor");

                var editorAsmDefPath = Path.Combine(editorPath, $"{refName}.Editor.asmdef");
                var newAssemblyName = $"{refName}.Editor";

                AssemblyDefinition editorAsmDef = new AssemblyDefinition(newAssemblyName)
                {
                    References = new List<string> { requiredGuid },
                    IncludePlatforms = new List<string> { "Editor" }
                };

                FileWriter.WriteAssemblyDefinition(editorAsmDefPath, editorAsmDef);
            }
            else if (!asmdef.References.Contains(requiredGuid) && !asmdef.References.Contains(asmdef.Name) && guidReq != guidRef)
            {
                asmdef.References.Add(requiredGuid);
                FileWriter.WriteAssemblyDefinition(outasmPath, asmdef);
            }
        }
    }
}