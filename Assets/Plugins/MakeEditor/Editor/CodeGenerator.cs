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
        private static class TemplateIdentifiers
        {
            public const string Namespace = "#NAMESPACE#";
            public const string ClassName = "#CLASS_NAME#";
        }

        private const string _tabulatorSign = "\t";
        private const string _bracketOpenSign = "{";
        private const string _bracketCloseSign = "}";
        private const string _namespaceKeyword = "namespace";

        private static readonly EditorDialog _overrideDialog;
        private const string _overrideMessageFormat = "Are you sure you want to override {0}?";

        static CodeGenerator()
        {
            _overrideDialog = new EditorDialog("Override script?", "Yes", "No");
        }

        public static string CreateEditorScriptAsset(List<string> scriptCode, string subjectPath)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(subjectPath);
            var scriptContent = PrepareScriptContent(scriptCode, script);

            var subjectAssemblyDefinition = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(subjectPath);
            bool isEditorAssemblyRequired = subjectAssemblyDefinition != null;

            var rootPath = isEditorAssemblyRequired ? Path.GetDirectoryName(subjectAssemblyDefinition) : PathUtility.AssetsFolder;
            var scriptSavePath = PathUtility.GetScriptPath(rootPath, subjectPath);

            if (File.Exists(scriptSavePath))
            {
                if (!IsOverrideAllowed(scriptSavePath))
                {
                    return null;
                }
            }

            FileWriter.WriteText(scriptSavePath, scriptContent);
            if (isEditorAssemblyRequired)
            {
                CreateEditorAssembly(subjectAssemblyDefinition, scriptSavePath);
            }
            return scriptSavePath;
        }

        private static bool IsOverrideAllowed(string scriptPath)
        {
            var message = string.Format(_overrideMessageFormat, scriptPath);
            var selectedOption = _overrideDialog.Show(message);

            return selectedOption == EditorDialog.Option.Accepted;
        }

        public static string PrepareScriptContent(List<string> scriptCode, MonoScript script)
        {
            var type = script.GetClass();

            AddOrRemoveNamespace();
            ReplaceClassName(scriptCode, type);

            return string.Join(Environment.NewLine, scriptCode.ToArray());

            void AddOrRemoveNamespace()
            {
                int namespaceIndex = scriptCode.FindIndex(str => str.Contains(TemplateIdentifiers.Namespace));
                if (namespaceIndex < 0)
                {
                    return;
                }

                if (type.Namespace == null)
                {
                    scriptCode.RemoveAt(namespaceIndex);
                }
                else
                {
                    AddIndentation(namespaceIndex);
                    AddNamespace(namespaceIndex);
                }
            }

            void AddNamespace(int namespaceIndex)
            {
                string usedNamespace = $"{_namespaceKeyword} {type.Namespace}";
                string namespaceCodeLine = scriptCode[namespaceIndex].Replace(TemplateIdentifiers.Namespace, usedNamespace);

                scriptCode[namespaceIndex] = _bracketOpenSign;
                scriptCode.Insert(namespaceIndex, namespaceCodeLine);
                scriptCode.Add(_bracketCloseSign);
            }

            void AddIndentation(int namespaceIndex)
            {
                for (int i = namespaceIndex + 1; i < scriptCode.Count; ++i)
                {
                    if (scriptCode[i].Length > 0)
                    {
                        scriptCode[i] = scriptCode[i].Insert(0, _tabulatorSign);
                    }
                }
            }
        }

        private static void ReplaceClassName(List<string> scriptCode, Type type)
        {
            for (int i = 0; i < scriptCode.Count; ++i)
            {
                scriptCode[i] = scriptCode[i].Replace(TemplateIdentifiers.ClassName, type.Name);
            }
        }

        public static void CreateEditorAssembly(string subjectAsmDefPath, string newEditorScriptPath)
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