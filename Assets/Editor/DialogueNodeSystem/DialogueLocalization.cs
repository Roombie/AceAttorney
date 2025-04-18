using System.Collections.Generic;
using UnityEngine;

public static class DialogueLocalization
{
    private static Dictionary<string, Dictionary<string, string>> localizedTexts;

    private static Dictionary<string, string> languageMap = new Dictionary<string, string>
    {
        { "Español", "ES" },
        { "English", "EN" },
        { "Français", "FR" }
    };

    public static void Load()
    {
        localizedTexts = new Dictionary<string, Dictionary<string, string>>();

        TextAsset csvFile = Resources.Load<TextAsset>("Localization/dialogues");
        if (csvFile == null)
        {
            Debug.LogError("No se encontró el archivo dialogues.csv en Resources/Localization/");
            return;
        }

        string[] lines = csvFile.text.Split('\n');
        if (lines.Length <= 1) return;

        var headers = lines[0].Trim().Split(';'); // it uses ; as a separator
        var langKeys = new List<string>();

        // English → EN
        for (int i = 1; i < headers.Length; i++)
        {
            string header = headers[i].Trim();
            langKeys.Add(languageMap.ContainsKey(header) ? languageMap[header] : header.ToUpper());
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Trim().Split(';'); // it uses ; as a separator

            if (values.Length < 2) continue;

            string id = values[0].Trim();
            localizedTexts[id] = new Dictionary<string, string>();

            for (int j = 1; j < values.Length && j <= langKeys.Count; j++)
            {
                string lang = langKeys[j - 1];
                localizedTexts[id][lang] = values[j].Trim();
            }
        }

        Debug.Log("Localization loaded successfully.");
    }

    public static string GetText(string id, string language)
    {
        if (localizedTexts == null) Load();

        if (localizedTexts.ContainsKey(id) && localizedTexts[id].ContainsKey(language))
        {
            return localizedTexts[id][language];
        }

        return $"[MISSING:{id}/{language}]";
    }
}