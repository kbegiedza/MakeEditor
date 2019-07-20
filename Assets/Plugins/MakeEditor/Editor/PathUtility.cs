using System.IO;
using System.Text;
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

        public static string GetEditorAssemblyDefinitionPath(string relatedAssemblyPath)
        {
            var relatedAssemblyName = Path.GetFileNameWithoutExtension(relatedAssemblyPath);

            var rootPath = Path.GetDirectoryName(relatedAssemblyPath);
            var editorPath = Path.Combine(rootPath, PathUtility.EditorSuffix);

            var filenameWithExtension = BuildFilename(relatedAssemblyName, Extensions.AssemblyDefinition);
            return Path.Combine(editorPath, filenameWithExtension);
        }

        public static string GetEditorScriptPath(string assemblyPath, string scriptPath)
        {
            var rootPath = assemblyPath != null
                ? Path.GetDirectoryName(assemblyPath)
                : PathUtility.AssetsFolder;

            int relativePathStart = rootPath.Length + 1;

            var scriptRelativePath = Path.GetDirectoryName(scriptPath.Substring(relativePathStart));
            var editorPath = Path.Combine(rootPath, EditorSuffix);
            var dirPath = Path.Combine(editorPath, scriptRelativePath);

            var name = Path.GetFileNameWithoutExtension(scriptPath);
            var filenameWithExtension = BuildFilename(name, Extensions.CSharpScript);
            var outputPath = Path.Combine(dirPath, filenameWithExtension);

            return outputPath;
        }

        public static string BuildFilename(string filename, string extension)
        {
            char separator = '.';

            StringBuilder builder = new StringBuilder(filename);
            builder.Append(EditorSuffix);
            builder.Append(separator);
            builder.Append(extension);

            return builder.ToString();
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
            public const string AssemblyDefinition = "asmdef";
            public const string CSharpScript = "cs";
        }
    }
}