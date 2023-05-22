using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.SimpleLocalization.Common;
using Assets.SimpleLocalization.Data;
using UnityEditor;
using UnityEngine;

namespace Assets.SimpleLocalization.Editor
{
    public partial class LocalizationTableEditorWindow : EditorWindow
    {
        public static Dictionary<string, SortedDictionary<string, string>> SheetDictionary;
        public static string TableId;
        public static string SheetName;

        public static List<string> Keys = new List<string>();
        public static List<string> Values = new List<string>();
        public static List<int> ChangedIndex = new List<int>();

        public static long CurrentSheetId = -1;

        private string _error;
        private string _currentLanguage;
        private string _sheetFileName;
        private string _newValue;
        private string _newKey;
        private bool _keyOnlyFilter;
        private bool _addRowPressed;

        private string _filter = "";

        private List<string> _oldKeys = new List<string>();
        private List<int> _deletedIndex = new List<int>();

        void OnGUI()
        {
            if (LocalizationSettingsWindow.Sheets == null || string.IsNullOrEmpty(SheetName) || string.IsNullOrEmpty(TableId) || LocalizationSettingsWindow.Sheets.Sheet.Count == 0 || CurrentSheetId == -1 || SheetDictionary.Count == 0)
            {
                Debug.Log("LocalizationSyncWindow: wrong input!");

                return;
            }

            MakeEditWindow();
        }

        private void MakeEditWindow()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.MaxWidth(100), GUILayout.MinWidth(100), GUILayout.ExpandHeight(true));

            foreach (var language in SheetDictionary.Keys)
            {
                var style = new GUIStyle(GUI.skin.button);

                style.fontStyle = FontStyle.Bold;

                if (GUILayout.Button(language, _currentLanguage == language ? style : GUI.skin.button))
                {
                    if (_currentLanguage != "" && _currentLanguage != language)
                    {
                        ResetSheet();
                    }

                    _currentLanguage = language;
                }
            }

            GUILayout.EndVertical();

            if (!string.IsNullOrEmpty(_currentLanguage))
            {
                MakeSheet(_currentLanguage);
            }

            GUILayout.EndHorizontal();
        }

        private void MakeSheet(string language)
        {
            GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

            MakeOptionsField(language);

            if (Keys.Count == 0)
            {
                _oldKeys = SheetDictionary[language].Keys.ToList();
                Keys = SheetDictionary[language].Keys.ToList();
                Values = SheetDictionary[language].Values.ToList();
                ChangedIndex.Clear();
                _deletedIndex.Clear();
                _newKey = "";
                _newValue = "";
            }

            GUILayout.BeginHorizontal("box", GUILayout.ExpandHeight(true));
            GUILayout.BeginVertical("box", GUILayout.MaxWidth(250), GUILayout.ExpandHeight(true));

            for (int i = 0; i < Keys.Count; i++)
            {
                if (_deletedIndex.Contains(i))
                {
                    continue;
                }

                if (_filter.Length > 2 && (_keyOnlyFilter ? !Keys[i].Contains(_filter) : !Keys[i].Contains(_filter) && !Values[i].Contains(_filter)))
                {
                    continue;
                }

                GUILayout.BeginHorizontal("box");

                var newString = EditorGUILayout.TextField(Keys[i]);

                if (newString != Keys[i])
                {
                    Keys[i] = newString;

                    if (!ChangedIndex.Contains(i))
                    {
                        ChangedIndex.Add(i);
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal("box");
            _newKey = EditorGUILayout.TextField(_newKey);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

            for (int i = 0; i < Values.Count; i++)
            {
                if (_deletedIndex.Contains(i))
                {
                    continue;
                }

                if (_filter.Length > 2 && (_keyOnlyFilter ? !Keys[i].Contains(_filter) : !Keys[i].Contains(_filter) && !Values[i].Contains(_filter)))
                {
                    continue;
                }

                GUILayout.BeginHorizontal("box");

                var newString = EditorGUILayout.TextField(Values[i]);

                if (newString != Values[i])
                {
                    Values[i] = newString;

                    if (!ChangedIndex.Contains(i))
                    {
                        ChangedIndex.Add(i);
                    }
                }

                if (GUILayout.Button("X", GUILayout.Width(17), GUILayout.Height(17)))
                {
                    DeleteRow(i);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal("box");
            _newValue = EditorGUILayout.TextField(_newValue);

            var styleGreen = new GUIStyle(GUI.skin.button);
            var styleRed = new GUIStyle(GUI.skin.button);

            styleGreen.fontStyle = styleRed.fontStyle = FontStyle.Bold;
            styleGreen.normal.textColor = Color.green;
            styleRed.normal.textColor = Color.red;

            _addRowPressed = GUILayout.Button("+", ((!string.IsNullOrEmpty(_newKey) && !string.IsNullOrEmpty(_newValue)) ? styleGreen : styleRed), GUILayout.Width(20), GUILayout.Height(20));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            if (_addRowPressed && !string.IsNullOrEmpty(_newKey) && !string.IsNullOrEmpty(_newValue))
            {
                _oldKeys.Add("");
                Keys.Add(_newKey);
                Values.Add(_newValue);
                _newValue = _newKey = "";
                ChangedIndex.Add(Keys.Count - 1);
            }
        }

        private void MakeOptionsField(string language)
        {
            var sheetFileName = LocalizationUtil.ResourcesPath + LocalizationSettingsWindow.SaveFolder + "\\" + SheetName +".csv";

            GUILayout.BeginHorizontal("box");

            GUILayout.Label("Filter:");
            _filter = EditorGUILayout.TextField(_filter, GUILayout.ExpandWidth(true));

            var style = new GUIStyle(GUI.skin.button);

            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.green;

            if (GUILayout.Button("Key only", _keyOnlyFilter ? style : GUI.skin.button))
            {
                _keyOnlyFilter = !_keyOnlyFilter;
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Translate"))
            {
                LocalizationTranslateWindow.ToLanguage = language;
                LocalizationTranslateWindow.FromLanguages = SheetDictionary.Keys.Where(i => i != language).ToList();

                var window = new LocalizationTranslateWindow();

                window.titleContent = new GUIContent("Translate empty cells");
                window.position = new Rect(Screen.width / 2, Screen.height / 2, 600, 400);
                window.Show();
            }

            if (GUILayout.Button("Submit"))
            {
                SaveSheet(CurrentSheetId, sheetFileName);
            }

            if (GUILayout.Button("Reload"))
            {
                ResetSheetAndSync(sheetFileName);
            }

            GUILayout.EndHorizontal();
        }

        private void ShowMessage(string message)
        {
            var window = new PopUpWindow();

            window.titleContent = new GUIContent(message.Contains("Error") ? "ERROR" : "Message");
            window.Error = message;
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 150);
            window.ShowModal();
        }
    }
}
