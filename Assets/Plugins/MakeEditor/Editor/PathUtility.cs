using System.IO;
using UnityEditor;
using UnityEditor.Compilation;

namespace Bloodstone.MakeEditor
{
    internal static class PathUtility
    {
        public const string AssetsFolder = "Assets";
        public const string EditorSuffix = "Editor";

        private const string _templateFolder = "Templates";
        private const string _editorTemplateName = "editor_template.txt";
        private const string _pluginAssemblyFilter = "t:asmdef Bloodstone.MakeEditor";


        public static string GetEditorScriptPath(string rootPath, string subjectPath)
        {
            var editorPath = Path.Combine(rootPath, EditorSuffix);
            var pathMod = Path.GetDirectoryName(subjectPath.Substring(rootPath.Length + 1)); //+1 to remove '/'
            var dirPath = Path.Combine(editorPath, pathMod);

            var name = Path.GetFileNameWithoutExtension(subjectPath);
            var outputPath = Path.Combine(dirPath, $"{name}{EditorSuffix}{Extensions.CSharpScript}");

            return outputPath;
        }

        public static string FindEditorTemplatePath()
        {
            var guids = AssetDatabase.FindAssets(_pluginAssemblyFilter);
            if (guids.Length <= 0)
            {
                throw new FileNotFoundException("Cannot find Bloodstone.MakeEditor assembly definition.");
            }

            var pluginPath = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guids[0]));
            return Path.Combine(pluginPath, _templateFolder, _editorTemplateName);
        }

        public static class Extensions
        {
            public const string AssemblyDefinition = ".asmdef";
            public const string CSharpScript = ".cs";
        }
    }
}