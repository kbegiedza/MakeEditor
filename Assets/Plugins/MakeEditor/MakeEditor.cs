using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    [InitializeOnLoad]
    public class MakeEditor
    {
        private static string _editorTemplate;
        
        static MakeEditor()
        {
            var pluginAsmdef = AssetDatabase.FindAssets($"t:asmdef Bloodstone.MakeEditor")[0];
            var pluginPath = AssetDatabase.GUIDToAssetPath(pluginAsmdef);

            _editorTemplate = Path.Combine(Path.GetDirectoryName(pluginPath), "editor_template.txt");
            Debug.Log(_editorTemplate);
        }

        [MenuItem("Assets/Create/Editor script")]
        public static void CreateEditorScript()
        {
            var selected = Selection.objects;
            if(selected.Length > 1)
            {
                //create standard editor or for each selected?
            }
            else if (selected.Length == 1)
            {
                var firstSelected = selected[0];
                var firstSelectedPath = AssetDatabase.GetAssetPath(firstSelected);
                Debug.Log($"first Selected path: {firstSelectedPath}");
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(firstSelectedPath);

                var dirPath = Path.GetDirectoryName(firstSelectedPath);
                var path = Path.Combine(dirPath, "Editor" , $"{firstSelected.name}Editor.cs");


                //should override?
                if (File.Exists(path))
                {
                    //
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    
                    //File.Copy(_editorTemplate, path);
                    var content = PrepareContent(_editorTemplate, firstSelectedPath, ms);
                    File.WriteAllText(path, content);
                    AssetDatabase.Refresh();
                }
            }
        }

        private static string PrepareContent(string template, string target, MonoScript s)
        {
            var code = File.ReadAllLines(template).ToList();

            var type = s.GetClass();
            if(type.Namespace != null)
            {
                //namespace
                for (int i = 0; i < code.Count; ++i)
                {
                    code[i] = Regex.Replace(code[i], "#NAMESPACE#", type.Namespace);
                    code[i] = Regex.Replace(code[i], "#CLASS_NAME#", type.Name);
                }
            }

            //namespace
            for (int i = 0; i < code.Count; ++i)
            {
                code[i] = Regex.Replace(code[i], "#CLASS_NAME#", type.Name);
            }

            var finalCode = string.Join("\n", code.ToArray());

            return finalCode;
        }
    }
}