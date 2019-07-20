using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Bloodstone.MakeEditor
{
    public static class EditorScriptGenerator
    {
        private const string _spaceSign = " ";
        private const string _tabulatorSign = "\t";
        private const string _bracketOpenSign = "{";
        private const string _bracketCloseSign = "}";
        private const string _namespaceKeyword = "namespace";

        public static void CreateEditorScript(List<string> scriptCode, string newScriptPath, MonoScript relatedScript)
        {
            var scriptContent = PrepareContent(scriptCode, relatedScript);
            FileWriter.WriteText(newScriptPath, scriptContent);
        }

        public static string PrepareContent(List<string> scriptCode, MonoScript script)
        {
            var type = script.GetClass();

            ReplaceClassName(scriptCode, type);
            AddOrRemoveNamespace(scriptCode, type);

            return string.Join(Environment.NewLine, scriptCode.ToArray());
        }

        public static void AddIndentation(List<string> scriptCode, int startIndex, int endIndex)
        {
            for (int i = startIndex; i <= endIndex; ++i)
            {
                if (scriptCode[i].Length > 0)
                {
                    scriptCode[i] = scriptCode[i].Insert(0, _tabulatorSign);
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

        private static void AddOrRemoveNamespace(List<string> scriptCode, Type type)
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
                AddIndentation(scriptCode, namespaceIndex + 1, scriptCode.Count - 1);
                AddNamespace();
            }

            void AddNamespace()
            {
                string usedNamespace = string.Join(_spaceSign, _namespaceKeyword, type.Namespace);
                string namespaceCodeLine = scriptCode[namespaceIndex].Replace(TemplateIdentifiers.Namespace, usedNamespace);

                scriptCode[namespaceIndex] = _bracketOpenSign;
                scriptCode.Insert(namespaceIndex, namespaceCodeLine);
                scriptCode.Add(_bracketCloseSign);
            }
        }

        private static class TemplateIdentifiers
        {
            public const string Namespace = "#NAMESPACE#";
            public const string ClassName = "#CLASS_NAME#";
        }
    }
}