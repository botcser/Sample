using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Assets.SimpleLocalization.Common;
using Assets.SimpleLocalization.Data;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.SimpleLocalization.Editor
{
    public class LocalizationSettingsWindow : EditorWindow
    {
        public static Dictionary<string, SortedDictionary<string, string>> SheetDictionary = new Dictionary<string, SortedDictionary<string, string>>();

        /// <summary>
        /// Table id on Google Spreadsheet.
        /// Let's say your table has the following url https://docs.google.com/spreadsheets/d/1RvKY3VE_y5FPhEECCa5dv4F7REJ7rBtGzQg9Z_B_DE4/edit#gid=331980525
        /// So your table id will be "1RvKY3VE_y5FPhEECCa5dv4F7REJ7rBtGzQg9Z_B_DE4" and sheet id will be "331980525" (gid parameter)
        /// </summary>
        public string TableId = "1RvKY3VE_y5FPhEECCa5dv4F7REJ7rBtGzQg9Z_B_DE4";

        /// <summary>
        /// Table sheet contains sheet name and id. First sheet has always zero id. Sheet name is used when saving.
        /// </summary>
        public static LocalizationEditorData Sheets = new LocalizationEditorData();

        /// <summary>
        /// External Google Apps script for writing to Google Sheets. You can change it and create your own.
        /// </summary>
        public static string GoogleScriptControllerUrl = "https://script.google.com/macros/s/AKfycbx62Qhu98jmX7SVdexY4YGoj4TCaR1-oeG3R8dLfTmPDk1d8hvMVAhasO-ewtuRIkN8tA/exec";

        /// <summary>
        /// Folder to save spreadsheets. Must be inside "Assets\Resources" folder.
        /// </summary>
        public static string SaveFolder = "Localization";

        private bool _busy;
        private string _error;
        private int _selectedSheet;
        
        [MenuItem("Window/Localization Sync Window/Settings")]
        public static void ShowWindow()
        {
            GetWindow<LocalizationSettingsWindow>("Localization Sync Window");
        }

        [MenuItem("Window/Localization Sync Window/Reset Settings")]
        public static void ResetSettings()
        {
            PlayerPrefs.DeleteKey("LocalizationsTableId");
            PlayerPrefs.DeleteKey("LocalizationsSaveFolder");
            PlayerPrefs.DeleteKey("LocalizationsSheets");
        }

        public void OnGUI()
        {
            MakeSettingsWindow();
        }

        private void MakeSettingsWindow()
        {
            Load(out var sheetNames, out var sheetIds);
            TableId = EditorGUILayout.TextField("Table Id", TableId);
            SaveFolder = EditorGUILayout.TextField("Save Folder", SaveFolder);
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            MakeSheetsPopup(sheetNames, sheetIds);
            
            EditorGUILayout.Space();

            if (GUILayout.Button("Sync"))
            {
                if (string.IsNullOrEmpty(TableId))
                {
                    Debug.Log("LocalizationSyncWindow: Can't make sync while Table Id is empty!");
                }
                else
                {
                    if (string.IsNullOrEmpty(Sheets.Sheet[0].Name) && !string.IsNullOrEmpty(GoogleScriptControllerUrl) && !_busy)
                    {
                        _busy = true;

                        LocalizationUtil.GetSheetsMeta(TableId, GoogleScriptControllerUrl, SaveFolder, (sheetsDict) =>
                        {
                            foreach (var key in sheetsDict.Keys)
                            {
                                sheetNames.Add(key);
                                sheetIds.Add(sheetsDict[key]);
                                SaveSettings(sheetNames, sheetIds);
                            }

                            LocalizationUtil.Sync(Sheets, TableId, SaveFolder, googleScriptUrl: GoogleScriptControllerUrl);

                            _busy = false;
                        });
                    }
                    else
                    {
                        LocalizationUtil.Sync(Sheets, TableId, SaveFolder, googleScriptUrl: GoogleScriptControllerUrl);
                    }
                }
            }

            SaveSettings(sheetNames, sheetIds);
        }

        private void MakeSheetsPopup(List<string> sheetNames, List<long> sheetIds)
        {
            var sheetNamesArray = sheetNames.ToArray();
            
            _selectedSheet = EditorGUILayout.Popup(_selectedSheet, sheetNamesArray);

            var sheetFileName = LocalizationUtil.ResourcesPath + SaveFolder + "\\" + sheetNamesArray[_selectedSheet] + ".csv";

            if (System.IO.File.Exists(sheetFileName))
            {
                if (GUILayout.Button("Open"))
                {
                    LocalizationTableEditorWindow.CurrentSheetId = sheetIds[_selectedSheet];
                    LocalizationTableEditorWindow.TableId = TableId;
                    LocalizationTableEditorWindow.SheetName = sheetNamesArray[_selectedSheet];
                    LocalizationTableEditorWindow.SheetDictionary = SheetDictionary;
                    LocalizationUtil.Read(SheetDictionary, sheetFileName);

                    var editWindow = new LocalizationTableEditorWindow();

                    editWindow.titleContent = new GUIContent("Localization Table Editor Window");
                    editWindow.minSize = new Vector2(600, 400);
                    editWindow.ShowModal();
                }
            }
        }

        private void Load(out List<string> sheetNames, out List<long> sheetIds)
        {
            sheetNames = new List<string>();
            sheetIds = new List<long>();
            Sheets.Sheet.Clear();
            TableId = PlayerPrefs.HasKey("LocalizationsTableId") ? PlayerPrefs.GetString("LocalizationsTableId") : "1RvKY3VE_y5FPhEECCa5dv4F7REJ7rBtGzQg9Z_B_DE4";

            if (PlayerPrefs.HasKey("LocalizationsSaveFolder"))
            {
                SaveFolder = PlayerPrefs.GetString("LocalizationsSaveFolder");
            }

            if (PlayerPrefs.HasKey("LocalizationsSheets"))
            {
                Sheets = JsonUtility.FromJson<LocalizationEditorData>(PlayerPrefs.GetString("LocalizationsSheets"));
            }
            else
            {
                //Sheets.Sheet.AddRange(new List<Sheet>() { new Sheet() { Name = "Settings", Id = 331980525 }, new Sheet() { Name = "Menu", Id = 0 }, new Sheet() { Name = "Tests", Id = 1674352817 } });
                Sheets.Sheet.Add(new Sheet());
            }

            foreach (var sheet in Sheets.Sheet)
            {
                sheetNames.Add(sheet.Name);
                sheetIds.Add(sheet.Id);
            }
        }

        private void SaveSettings(List<string> sheetNames, List<long> sheetIds)
        {
            Sheets.Sheet.Clear();

            for (int i = 0; i < sheetNames.Count; i++)
            {
                if (!string.IsNullOrEmpty(sheetNames[i]))
                {
                    Sheets.Sheet.Add(new Sheet { Id = sheetIds[i], Name = sheetNames[i] });
                }
            }

            if (!string.IsNullOrEmpty(TableId)) PlayerPrefs.SetString("LocalizationsTableId", TableId);

            if (Sheets.Sheet.Count > 0)
            {
                var s = JsonUtility.ToJson(Sheets);

                PlayerPrefs.SetString("LocalizationsSheets", JsonUtility.ToJson(Sheets));
            }

            if (!string.IsNullOrEmpty(SaveFolder)) PlayerPrefs.SetString("LocalizationsSaveFolder", SaveFolder);
        }

        private void MakeInput(List<string> sheetNames, List<long> sheetIds)
        {
            for (int i = 0; i < sheetNames.Count; i++)
            {
                sheetNames[i] = EditorGUILayout.TextField($"Sheet[{i}] Name", sheetNames[i]);
                sheetIds[i] = EditorGUILayout.LongField($"Sheet[{i}] Id", sheetIds[i]);

                if (sheetNames[i] != null)
                {
                    var sheetFileName = LocalizationUtil.ResourcesPath + SaveFolder + "\\" + sheetNames[i] + ".csv";

                    if (System.IO.File.Exists(sheetFileName))
                    {
                        if (GUILayout.Button("Edit"))
                        {
                            LocalizationTableEditorWindow.CurrentSheetId = sheetIds[i];
                            LocalizationTableEditorWindow.TableId = TableId;
                            LocalizationTableEditorWindow.SheetName = sheetNames[i];
                            LocalizationTableEditorWindow.SheetDictionary = SheetDictionary;
                            LocalizationUtil.Read(SheetDictionary, sheetFileName);

                            var editWindow = new LocalizationTableEditorWindow();

                            editWindow.titleContent = new GUIContent("Localization Table Editor Window");
                            editWindow.minSize = new Vector2(600, 400);
                            editWindow.ShowModal();
                        }
                    }
                }

                EditorGUILayout.Space();
            }
        }
    }
}