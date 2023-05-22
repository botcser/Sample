using UnityEditor;
using UnityEngine;

namespace Assets.SimpleLocalization.Editor
{
    public class PopUpWindow : EditorWindow
    {
        public string Error = "";

        public void OnGUI()
        {
            EditorGUILayout.LabelField(Error);
            GUILayout.Space(70);

            if (GUILayout.Button("OK"))
            {
                this.Close();
            }
        }
    }
}
