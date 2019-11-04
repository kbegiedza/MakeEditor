using System.IO;
using System.Text;
using UnityEditor;

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
            relatedAssemblyPath.ThrowIfNull(nameof(relatedAssemblyPath));

            var relatedAssemblyName = Path.GetFileNameWithoutExtension(relatedAssemblyPath);

            var rootPath = Path.GetDirectoryName(relatedAssemblyPath);
            var editorPath = Path.Combine(rootPath, EditorSuffix);

            var filenameWithExtension = BuildFilename(relatedAssemblyName, Extensions.AssemblyDefinition);

            return Path.Combine(editorPath, filenameWithExtension);
        }

        public static string GetEditorScriptPath(string assemblyPath, string scriptPath)
        {
            scriptPath.ThrowIfNull(nameof(scriptPath));

            var rootPath = assemblyPath != null
                ? Path.GetDirectoryName(assemblyPath)
                : AssetsFolder;

            int relativePathStartIndex = rootPath.Length + 1;

            var scriptRelativePath = Path.GetDirectoryName(scriptPath.Substring(relativePathStartIndex));
            var editorPath = Path.Combine(rootPath, EditorSuffix);
            var directoryPath = Path.Combine(editorPath, scriptRelativePath);

            var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
            var filename = BuildFilename(scriptName, Extensions.CSharpScript);

            return Path.Combine(directoryPath, filename);
        }

        public static string BuildFilename(string filename, string extension)
        {
            filename.ThrowIfNull(filename);
            extension.ThrowIfNull(extension);

            const char separator = '.';

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
                throw new FileNotFoundException("Cannot find Bloodstone.MakeEditor assembly definition. Please try reimport plugin.");
            }

            var firstGUID = AssetDatabase.GUIDToAssetPath(guids[0]);
            var pluginPath = Path.GetDirectoryName(firstGUID);

            return Path.Combine(pluginPath, _templateFolder, _editorTemplateName);
        }

        public static class Extensions
        {
            public const string AssemblyDefinition = "asmdef";
            public const string CSharpScript = "cs";
        }
    }
}