using System.IO;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    internal static class FileWriter
    {
        public static void WriteAssemblyDefinition(string savePath, AssemblyDefinition assemblyDefinition)
        {
            var serializedObject = JsonUtility.ToJson(assemblyDefinition, true);

            WriteText(savePath, serializedObject);
        }

        public static void WriteText(string savePath, string text)
        {
            PrepareDirectory(savePath);

            File.WriteAllText(savePath, text);
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