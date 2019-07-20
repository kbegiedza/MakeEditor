using System.IO;
using UnityEditor;

namespace Bloodstone.MakeEditor
{
    internal static class PathUtility
    {
        private const string _scriptExtension = ".cs";
        private const string _templateFolder = "Templates";
        private const string _editorTemplateName = "editor_template.txt";
        private const string _pluginAssemblyFilter = "t:asmdef Bloodstone.MakeEditor";

        public static string GetScriptPath(string rootPath, string subjectPath)
        {
            var editorPath = Path.Combine(rootPath, "Editor");
            var pathMod = Path.GetDirectoryName(subjectPath.Substring(rootPath.Length + 1)); //+1 to remove '/'
            var dirPath = Path.Combine(editorPath, pathMod);

            var name = Path.GetFileNameWithoutExtension(subjectPath);
            var outputPath = Path.Combine(dirPath, $"{name}Editor.cs");

            if (File.Exists(outputPath))
            {
                outputPath = GenerateNotExistingFilename(outputPath);
            }

            return outputPath;
        }

        public static string FindEditorTemplatePath()
        {
            var guids = AssetDatabase.FindAssets(_pluginAssemblyFilter);
            if (guids.Length <= 0)
            {
                throw new FileNotFoundException("Cannot find Bloodstone.MakeEditor assembly definition");
            }

            var pluginPath = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guids[0]));
            return Path.Combine(pluginPath, _templateFolder, _editorTemplateName);
        }

        private static string GenerateNotExistingFilename(in string path)
        {
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileNameWithoutExtension(path);

            var newFileName = fileName + _scriptExtension;

            return Path.Combine(directory, newFileName);
        }
    }
}