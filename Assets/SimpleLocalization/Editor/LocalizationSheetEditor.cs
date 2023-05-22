using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.SimpleLocalization.Common;
using UnityEngine;

namespace Assets.SimpleLocalization.Editor
{
    public partial class LocalizationTableEditorWindow
    {
        private void ResetSheetAndSync(string sheetFileName, Action callback = null)
        {
            ResetSheet();
            LocalizationUtil.Sync(LocalizationSettingsWindow.Sheets, TableId, LocalizationSettingsWindow.SaveFolder, callback);
            LocalizationUtil.Read(SheetDictionary, sheetFileName);
        }

        private void ResetSheet()
        {
            _deletedIndex.Clear();
            ChangedIndex.Clear();
            _oldKeys.Clear();
            Keys.Clear();
            Values.Clear();
            _newKey = "";
            _newValue = "";
        }

        private void DeleteRow(int i)
        {
            Keys[i] = "";

            if (!ChangedIndex.Contains(i))
            {
                ChangedIndex.Add(i);
            }

            if (!_deletedIndex.Contains(i))
            {
                _deletedIndex.Add(i);
            }
        }

        public async Task SaveSheet(long sheetId, string sheetFileName)
        {
            if (HaveDuplicate())
            {
                ShowMessage(_error);
                return;
            }

            foreach (var i in ChangedIndex)
            {
                await Task.Run(() => LocalizationUtil.SendRow(CreateRow(i), CurrentSheetId, TableId,
                    LocalizationSettingsWindow.GoogleScriptControllerUrl, (message) =>
                    {
                        if (message != "OK" || message.Contains("ERROR"))
                        {
                            ShowMessage(message);
                        }
                        else
                        {
                            LocalizationUtil.Sync(LocalizationSettingsWindow.Sheets, TableId,
                                LocalizationSettingsWindow.SaveFolder,
                                () => { LocalizationUtil.Read(SheetDictionary, sheetFileName); });
                        }
                    }));
            }
        }

        private bool HaveDuplicate()
        {
            if (Keys.GroupBy(i => i).Count(i => i.Count<string>() > 1) > 0)
            {
                _error = "It is duplicate keys in your sheet!";

                return true;
            }

            return false;
        }
        
        private Dictionary<string, string> CreateRow(int index)
        {
            var dict = new Dictionary<string, string>() { { "Key", _oldKeys[index] }, { "NewKey", Keys[index] } };

            if (Keys.Count == 0 || Values.Count == 0 || Keys.Count != Values.Count)
            {
                return null;
            }

            foreach (var language in SheetDictionary.Keys)
            {
                dict.Add(language, language == _currentLanguage ? Values[index] : _oldKeys[index] == "" ? "" : SheetDictionary[language][_oldKeys[index]]);
            }

            return dict;
        }

        public static void FillEmptyValues(string key, string value, string language)
        {
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                var index = Keys.IndexOf(key);

                Values[index] = value;
                ChangedIndex.Add(index);
            }
        }
    }
}
