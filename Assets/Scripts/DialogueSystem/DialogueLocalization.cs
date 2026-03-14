using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Three-tier localization lookup used by the dialogue editor preview and runtime.
///
/// Priority:
///   1. Unity Localization package (com.unity.localization) — if installed and a
///      StringTable named "Dialogues" exists for the active locale.
///   2. CSV file at  Resources/Localization/dialogues.csv
///      Format:  id ; English ; Español ; Français  (semicolon-separated, first row = header)
///   3. Raw fallback — returns the id as-is so the graph is always usable.
/// </summary>
public static class DialogueLocalization
{
    // ── CSV cache ────────────────────────────────────────────────────────────
    // [textId][languageCode] = localised string
    private static Dictionary<string, Dictionary<string, string>> _csvTexts;
    private static bool _csvLoaded;

    // Language display-name → ISO code used in the CSV header row
    private static readonly Dictionary<string, string> _languageMap = new()
    {
        { "English",  "EN" },
        { "Español",  "ES" },
        { "Français", "FR" },
        { "Deutsch",  "DE" },
        { "Italiano", "IT" },
        { "日本語",    "JA" },
    };

    // ── Unity Localization availability (resolved once at load) ──────────────
    private static readonly bool _unityLocalizationAvailable = CheckUnityLocalization();

    private static bool CheckUnityLocalization()
    {
#if UNITY_LOCALIZATION
        return true;
#else
        // Soft check: see if the type exists without a hard compile-time dependency
        return System.Type.GetType("UnityEngine.Localization.Settings.LocalizationSettings, Unity.Localization") != null;
#endif
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the localised text for <paramref name="id"/> in <paramref name="languageCode"/>
    /// (e.g. "EN", "ES"). Falls back gracefully through all three tiers.
    /// </summary>
    public static string GetText(string id, string languageCode)
    {
        if (string.IsNullOrWhiteSpace(id))
            return string.Empty;

        languageCode = languageCode?.ToUpper() ?? "EN";

        // Tier 1 — Unity Localization
        if (_unityLocalizationAvailable)
        {
            string unityResult = TryGetFromUnityLocalization(id);
            if (unityResult != null)
                return unityResult;
        }

        // Tier 2 — CSV
        if (!_csvLoaded) LoadCSV();
        if (_csvTexts != null &&
            _csvTexts.TryGetValue(id, out var langs) &&
            langs.TryGetValue(languageCode, out var csvText))
        {
            return csvText;
        }

        // Tier 3 — Raw fallback (just show the id)
        return id;
    }

    /// <summary>
    /// Force-reload the CSV cache (useful after editing the CSV at runtime/in-editor).
    /// </summary>
    public static void ReloadCSV()
    {
        _csvLoaded = false;
        _csvTexts  = null;
        LoadCSV();
    }

    // ── Unity Localization (Tier 1) ──────────────────────────────────────────

    private static string TryGetFromUnityLocalization(string id)
    {
        try
        {
#if UNITY_LOCALIZATION
            var settings = UnityEngine.Localization.Settings.LocalizationSettings.Instance;
            if (settings == null) return null;

            var db = settings.GetStringDatabase();
            var op = db.GetTableEntryAsync("Dialogues", id);
            // In editor / synchronous context we can check IsDone immediately
            if (op.IsDone && op.Result.Entry != null)
                return op.Result.Entry.GetLocalizedString();
#endif
        }
        catch { /* package not available or table missing — fall through */ }
        return null;
    }

    // ── CSV (Tier 2) ─────────────────────────────────────────────────────────

    private static void LoadCSV()
    {
        _csvLoaded = true;
        _csvTexts  = new Dictionary<string, Dictionary<string, string>>();

        TextAsset csvFile = Resources.Load<TextAsset>("Localization/dialogues");
        if (csvFile == null)
        {
            // Not an error — CSV is optional
            Debug.Log("[DialogueLocalization] No CSV found at Resources/Localization/dialogues.csv — using raw-text fallback.");
            return;
        }

        string[] lines = csvFile.text.Split('\n');
        if (lines.Length <= 1) return;

        // Header row: id ; English ; Español ; ...
        var headers  = lines[0].Trim().Split(';');
        var langKeys = new List<string>();
        for (int i = 1; i < headers.Length; i++)
        {
            string h = headers[i].Trim();
            langKeys.Add(_languageMap.TryGetValue(h, out var code) ? code : h.ToUpper());
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var values = line.Split(';');
            if (values.Length < 2) continue;

            string id = values[0].Trim();
            _csvTexts[id] = new Dictionary<string, string>();

            for (int j = 1; j < values.Length && j - 1 < langKeys.Count; j++)
                _csvTexts[id][langKeys[j - 1]] = values[j].Trim();
        }

        Debug.Log($"[DialogueLocalization] CSV loaded — {_csvTexts.Count} entries.");
    }

    // ── Editor helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the active language code for use in editor previews.
    /// Reads from Unity Localization if available, otherwise returns "EN".
    /// </summary>
    public static string GetEditorLanguageCode()
    {
#if UNITY_LOCALIZATION
        try
        {
            var locale = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale;
            if (locale != null)
                return locale.Identifier.Code.ToUpper();
        }
        catch { }
#endif
        return "EN";
    }

    // ── CSV mutation (for unused-dialogue cleanup) ────────────────────────────

    /// <summary>Returns all text IDs currently in the CSV, or null if no CSV loaded.</summary>
    public static List<string> GetAllIds()
    {
        if (!_csvLoaded) LoadCSV();
        return _csvTexts == null ? null : new List<string>(_csvTexts.Keys);
    }

    /// <summary>Removes the given IDs from the CSV and writes the file back to disk.</summary>
    public static void RemoveIds(List<string> ids)
    {
        if (_csvTexts == null || ids == null || ids.Count == 0) return;

        foreach (var id in ids)
            _csvTexts.Remove(id);

        WriteCSV();
        Debug.Log($"[DialogueLocalization] Removed {ids.Count} unused ID(s) from CSV.");
    }

    private static void WriteCSV()
    {
#if UNITY_EDITOR
        // Find the CSV asset path
        string[] guids = UnityEditor.AssetDatabase.FindAssets("dialogues", new[] { "Assets/Resources/Localization" });
        string path = guids.Length > 0
            ? UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0])
            : "Assets/Resources/Localization/dialogues.csv";

        // Collect all language codes from existing data
        var langCodes = new List<string>();
        foreach (var entry in _csvTexts.Values)
            foreach (var code in entry.Keys)
                if (!langCodes.Contains(code)) langCodes.Add(code);

        // Build reverse language map (code → display name)
        var codeToDisplay = new Dictionary<string, string>();
        foreach (var kv in _languageMap) codeToDisplay[kv.Value] = kv.Key;

        var sb = new System.Text.StringBuilder();
        // Header
        sb.Append("id");
        foreach (var code in langCodes)
            sb.Append(";" + (codeToDisplay.TryGetValue(code, out var display) ? display : code));
        sb.AppendLine();

        // Rows
        foreach (var kv in _csvTexts)
        {
            sb.Append(kv.Key);
            foreach (var code in langCodes)
                sb.Append(";" + (kv.Value.TryGetValue(code, out var text) ? text : ""));
            sb.AppendLine();
        }

        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}