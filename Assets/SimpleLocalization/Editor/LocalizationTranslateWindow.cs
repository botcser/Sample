using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.SimpleLocalization.Editor
{
    public class LocalizationTranslateWindow : EditorWindow
    {
        public static string ToLanguage;
        public static List<string> FromLanguages;
        public static Dictionary <string, string> Dict;

        private string _currentLanguage;

        void OnGUI()
        {
            if (string.IsNullOrEmpty(_currentLanguage))
            {
                GUILayout.TextArea("Select source language please:");

                GUILayout.BeginHorizontal();

                foreach (var language in FromLanguages)
                {
                    if (GUILayout.Button(language))
                    {
                        _currentLanguage = language;

                        Dict = LocalizationSettingsWindow.SheetDictionary[_currentLanguage].Where(i => LocalizationSettingsWindow.SheetDictionary[ToLanguage][i.Key] == "").ToDictionary(x => x.Key, x => x.Value);

                        break;
                    }
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.TextArea($"Translate all empty cells ({Dict.Count}) from language {_currentLanguage} into language {ToLanguage}?");

                if (GUILayout.Button("Translate"))
                {
                    foreach (var key in Dict.Keys)
                    {
                        TranslateText(GetLangCode(ToLanguage), Dict[key], (translated) =>
                        {
                            LocalizationTableEditorWindow.FillEmptyValues(key,translated, ToLanguage);
                        });
                    }
                }
            }
        }

        public void TranslateText(string targetLang, string sourceText, Action<string> callback)
        {
            if (!string.IsNullOrEmpty(sourceText))
            {
                string url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=" + GetLangCode(_currentLanguage) + "&tl=" + targetLang + "&dt=t&q=" + WWW.EscapeURL(sourceText);

                var client = new HttpClient();
                var request = UnityWebRequest.Get(url);

                request.SetRequestHeader("Accept", "application/json");

                request.SendWebRequest().completed += _ =>
                {
                    if (request.error == null)
                    {
                        var parsedTexts = request.downloadHandler.text;
                        var start = parsedTexts.IndexOf('"') + 1;
                        var length = parsedTexts.IndexOf('"', start + 1);

                        client.Dispose();
                        callback(parsedTexts.Substring(start, length - start));
                    }
                };
            }
        }

        private string GetLangCode(string language)
        {
            switch (language)
            {
                case "German":
                    return "de";
                case "Chinese":
                    return "zh-TW";
                case "Japanese":
                    return "ja";
                case "Korean":
                    return "ko";
                case "Russian":
                    return "ru";
                case "France":
                    return "fr";
                case "Indonesia":
                    return "id";
                case "Italy":
                    return "it";
                case "Philippines":
                    return "ph";
                case "Polish":
                    return "pl";
                case "Portuguese":
                    return "pt";
                case "Spain":
                    return "es";
                case "Swedish":
                    return "sv";
                case "Thailand":
                    return "th";
                case "Turkey":
                    return "tr";
                case "Vietnam":
                    return "vi";
                case "Arabic":
                    return "ar";
                case "Romanian":
                    return "ro";
                default:
                    return "en";
            }
        }
    }
}
