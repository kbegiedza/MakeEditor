using System.IO;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    public static class FileWriter
    {
        public static void WriteAssemblyDefinition(string path, AssemblyDefinition assemblyDefinition)
        {
            path.ThrowIfNull(nameof(path));
            assemblyDefinition.ThrowIfNull(nameof(assemblyDefinition));

            var serializedObject = JsonUtility.ToJson(assemblyDefinition, true);

            WriteText(path, serializedObject);
        }

        public static void WriteText(string path, string text)
        {
            path.ThrowIfNull(nameof(path));
            text.ThrowIfNull(nameof(text));

            CreateFileDirectory(path);

            File.WriteAllText(path, text);
        }

        private static void CreateFileDirectory(string fileDirectory)
        {
            var requiredDirectory = Path.GetDirectoryName(fileDirectory);

            if (!Directory.Exists(requiredDirectory))
            {
                Directory.CreateDirectory(requiredDirectory);
            }
        }
    }
}