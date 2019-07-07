using UnityEditor;
using UnityEngine;

namespace Bloodstone.MakeEditor
{
    public class MakeEditor : MonoBehaviour
    {
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
                var o = selected[0];
                var path = AssetDatabase.GetAssetPath(o);

                print($"Editor : {path}");
            }
        }


        [MenuItem("Assets/Create/PropertyDrawer")]
        public static void CreatePropertyDrawer()
        {
            print($"PropertyDrawer!");
        }
    }
}