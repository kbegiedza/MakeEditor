using System.IO;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    public static class FileWriter
    {
        public static void WriteAssemblyDefinition(string path, AssemblyDefinition assemblyDefinition)
        {
            var serializedObject = JsonUtility.ToJson(assemblyDefinition, true);

            WriteText(path, serializedObject);
        }

        public static void WriteText(string path, string text, bool allowOverride = false)
        {
            PrepareFileDirectory(path);

            File.WriteAllText(path, text);
        }

        public static void PrepareFileDirectory(string fileDirectory)
        {
            var requiredDirectory = Path.GetDirectoryName(fileDirectory);

            if (!Directory.Exists(requiredDirectory))
            {
                Directory.CreateDirectory(requiredDirectory);
            }
        }
    }
}