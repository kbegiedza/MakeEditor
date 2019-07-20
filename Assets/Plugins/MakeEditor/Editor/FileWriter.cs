using System.IO;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    internal static class FileWriter
    {
        public static void WriteAssemblyDefinition(string path, AssemblyDefinition assemblyDefinition)
        {
            var serializedObject = JsonUtility.ToJson(assemblyDefinition, true);

            WriteText(path, serializedObject);
        }

        public static void WriteText(string path, string text)
        {
            PrepareDirectory(path);

            File.WriteAllText(path, text);
        }

        private static void PrepareDirectory(string savePath)
        {
            var requiredDirectory = Path.GetDirectoryName(savePath);

            if (!Directory.Exists(requiredDirectory))
            {
                Directory.CreateDirectory(requiredDirectory);
            }
        }
    }
}