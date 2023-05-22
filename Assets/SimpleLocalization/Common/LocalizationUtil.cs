using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Assets.SimpleLocalization.Data;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.SimpleLocalization.Common
{
    public class LocalizationUtil
    {
        public static string ResourcesPath = "Assets\\Resources\\";

        private const string UrlPattern = "https://docs.google.com/spreadsheets/d/{0}/export?format=csv&gid={1}";

        public static async Task SendRow(Dictionary<string, string> row, long sheetId, string tableId, string googleScriptUrl, Action<string> callback = null)
        {
            if (row == null || row.Count == 0 || string.IsNullOrEmpty(tableId))
            {
                Debug.Log("LocalizationSyncWindow: wrong input!");
                return;
            }

            Debug.Log($"LocalizationSyncWindow: writing to table \"{tableId}\" ...");

            var json = JsonConvert.SerializeObject(row);
            var url = string.Format(UrlPattern, tableId, sheetId);
            var values = new Dictionary<string, string>
            {
                { "tableUrl", url },
                { "data", json }
            };

            await Task.Run(() => Download(googleScriptUrl, values, (result, message) =>
            {
                if (result == "OK")
                {
                    if (!message.Contains("ERROR"))
                    {
                        Debug.Log($"LocalizationSyncWindow: ...<color=green>SUCCESS</color>: {message}");
                    }
                    else
                    {
                        Debug.Log($"LocalizationSyncWindow: ...<color=green>ERROR</color>: {message}");
                    }
                }
                else
                {
                    Debug.Log($"LocalizationSyncWindow: ...<color=red>FATAL ERROR</color>: {result}");
                }

                callback?.Invoke(string.IsNullOrEmpty(message) ? result : message);
            }));
        }

        public static void GetSheetsMeta(string tableId, string googleScriptUrl, string saveFolder, Action<Dictionary<string, long>> callback)
        {
            var values = new Dictionary<string, string>
            {
                { "tableUrl", tableId },
                { "data", "get" }
            };

            var request = UnityWebRequest.Post(googleScriptUrl, values);

            request.SendWebRequest().completed += (op) =>
            {
                if (request.error == null)
                {
                    var sheetsDict = JsonConvert.DeserializeObject<Dictionary<string, long>>(System.Text.Encoding.Default.GetString(request.downloadHandler.data));
                    
                    callback?.Invoke(sheetsDict);
                }
                else
                {
                    throw new Exception(request.error);
                }
            };
        }
        
        public static void Sync(LocalizationEditorData sheets, string tableId, string saveFolder, Action callback = null, string googleScriptUrl = null)
        {
            Debug.Log("<color=yellow>Localization sync started...</color>");

            var dict = new Dictionary<string, UnityWebRequest>();

            if (!string.IsNullOrEmpty(sheets.Sheet[0].Name))
            {
                GetSheetsData(dict, sheets, tableId, saveFolder, callback);
                UnityEditor.AssetDatabase.Refresh();

                Debug.Log("<color=yellow>Localization sync completed!</color>");
            }
        }

        private static void GetSheetsData(Dictionary<string, UnityWebRequest> dict, LocalizationEditorData sheets, string tableId, string saveFolder, Action callback = null)
        {
            foreach (var sheet in sheets.Sheet)
            {
                var url = string.Format(UrlPattern, tableId, sheet.Id);

                Debug.Log($"Downloading: {url}...");

                dict.Add(url, UnityWebRequest.Get(url));
            }

            foreach (var entry in dict)
            {
                var url = entry.Key;
                var request = UnityWebRequest.Get(url);

                request.SendWebRequest().completed += op =>
                {
                    if (request.error == null)
                    {
                        var sheet = sheets.Sheet.Single(i => url == string.Format(UrlPattern, tableId, i.Id));
                        var path = System.IO.Path.Combine(ResourcesPath + saveFolder, sheet.Name + ".csv");

                        System.IO.File.WriteAllBytes(path, request.downloadHandler.data);
                        Debug.LogFormat("Sheet {0} downloaded to <color=grey>{1}</color>", sheet.Id, path);

                        callback?.Invoke();
                    }
                    else
                    {
                        throw new Exception(request.error);
                    }
                };
            }
        }

        public static void Read(Dictionary<string, SortedDictionary<string, string>> sheetDictionary, string sheetFilePath)
        {
            sheetDictionary.Clear();

            var text = System.IO.File.ReadAllText(sheetFilePath).Replace("\r\n", "\n").Replace("\"\"", "[_quote_]");
            var matches = Regex.Matches(text, "\"[\\s\\S]+?\"");

            foreach (Match match in matches)
            {
                text = text.Replace(match.Value,
                    match.Value.Replace("\"", null).Replace(",", "[_comma_]").Replace("\n", "[_newline_]"));
            }

            // Making uGUI line breaks to work in asian texts.
            text = text.Replace("。", "。 ").Replace("、", "、 ").Replace("：", "： ").Replace("！", "！ ").Replace("（", " （").Replace("）", "） ").Trim();

            var lines = text.Split('\n').Where(i => i != "").ToList();
            var languages = lines[0].Split(',').Select(i => i.Trim()).ToList();

            for (var i = 1; i < languages.Count; i++)
            {
                if (!sheetDictionary.ContainsKey(languages[i]))
                {
                    sheetDictionary.Add(languages[i], new SortedDictionary<string, string>());
                }
            }

            for (var i = 1; i < lines.Count; i++)
            {
                var columns = lines[i].Split(',').Select(j => j.Trim()).Select(j => j.Replace("[_quote_]", "\"").Replace("[_comma_]", ",").Replace("[_newline_]", "\n")).ToList();
                var key = columns[0];

                if (key == "") continue;

                for (var j = 1; j < languages.Count; j++)
                {
                    sheetDictionary[languages[j]].Add(key, columns[j]);
                }
            }
        }

        private static async Task Download(string url, Dictionary<string, string> values, Action<string, string> callback)
        {
            var client = new HttpClient();
            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync(url, content);

            var responseString = await response.Content.ReadAsStringAsync();

            var matches = Regex.Matches(responseString, @">(?<Message>.+?)<\/div>");
            var message = matches[1].Groups["Message"].Value;

            callback?.Invoke(response.ReasonPhrase, message);
        }
    }
}
