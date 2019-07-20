using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    public class CodeGenerator
    {
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

        public static void CreateAssembly(string subjectAsmDefPath, string newEditorScriptPath)
        {
            Debug.Log($"Subject assembly: {subjectAsmDefPath} for created editor script {newEditorScriptPath}");

            var outasmPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(newEditorScriptPath);
            if (outasmPath == null)
            {
                throw new NotSupportedException($"Cannot create editor assembly without runtime assembly to reference");
            }

            var str = File.ReadAllText(outasmPath);
            AssemblyDefinition asmdef = JsonUtility.FromJson<AssemblyDefinition>(str);
            var guidRef = AssetDatabase.AssetPathToGUID(subjectAsmDefPath);
            var requiredGuid = $"GUID:{guidRef}";

            if (!asmdef.IncludePlatforms.Contains("Editor"))
            {
                var refName = Path.GetFileNameWithoutExtension(subjectAsmDefPath);

                var rootPath = Path.GetDirectoryName(subjectAsmDefPath);
                var editorPath = Path.Combine(rootPath, "Editor");

                var editorAsmDefPath = Path.Combine(editorPath, $"{refName}.Editor.asmdef");
                var newAssemblyName = $"{refName}.Editor";

                AssemblyDefinition editorAsmDef = new AssemblyDefinition(newAssemblyName)
                {
                    References = new List<string>(1) { requiredGuid }
                };

                FileWriter.WriteAssemblyDefinition(editorAsmDefPath, editorAsmDef);
            }
            else if (!asmdef.References.Contains(requiredGuid) && !asmdef.References.Contains(asmdef.Name))
            {
                asmdef.References.Add(requiredGuid);
                FileWriter.WriteAssemblyDefinition(outasmPath, asmdef);
            }
        }
    }
}