using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    private const string DefaultChineseFontPath = "Fonts & Materials/msyh SDF";

    public static LocalizationManager Instance { get; private set; }

    [SerializeField] private TextAsset LocalizationCsv;
    [SerializeField] private TMP_FontAsset ChineseFontAsset;

    public Language CurrentLanguage { get; private set; } = Language.En;

    private readonly Dictionary<string, Dictionary<Language, string>> texts = new();
    private bool missingFontWarningLogged;

    public event System.Action LanguageChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        LoadLocalizationCsv();
    }

    private void LoadLocalizationCsv()
    {
        texts.Clear();

        if (LocalizationCsv == null)
        {
            Debug.LogError("LocalizationCsv is not assigned.");
            return;
        }

        List<string[]> rows = CsvUtility.Parse(LocalizationCsv.text);
        StringBuilder chineseCharacters = new StringBuilder();

        for (int i = 1; i < rows.Count; i++)
        {
            string[] columns = rows[i];

            if (columns.Length < 3)
            {
                Debug.LogWarning($"Invalid localization row: {string.Join(",", columns)}");
                continue;
            }

            string key = columns[0].Trim();
            string zh = columns[1].Trim();
            string en = columns[2].Trim();

            chineseCharacters.Append(zh);

            texts[key] = new Dictionary<Language, string>
                {
                    { Language.Zh, zh },
                    { Language.En, en }
                };
        }

        PrewarmChineseCharacters(chineseCharacters.ToString());
    }

    public bool PrewarmChineseCharacters(string characters)
    {
        if (string.IsNullOrEmpty(characters))
        {
            return true;
        }

        TMP_FontAsset fontAsset = GetChineseFontAsset();
        if (fontAsset == null)
        {
            if (!missingFontWarningLogged)
            {
                Debug.LogWarning(
                    $"Chinese font asset is not assigned and could not be loaded from " +
                    $"Resources/{DefaultChineseFontPath}.");
                missingFontWarningLogged = true;
            }

            return false;
        }

        fontAsset.isMultiAtlasTexturesEnabled = true;

        List<TMP_FontAsset> globalFallbacks = TMP_Settings.fallbackFontAssets;
        if (globalFallbacks != null && !globalFallbacks.Contains(fontAsset))
        {
            globalFallbacks.Add(fontAsset);
        }

        bool allCharactersAdded = fontAsset.TryAddCharacters(
            characters,
            out string missingCharacters);

        if (!allCharactersAdded && !string.IsNullOrEmpty(missingCharacters))
        {
            Debug.LogWarning(
                $"Chinese font '{fontAsset.name}' could not provide these characters: " +
                missingCharacters);
        }

        return allCharactersAdded;
    }

    private TMP_FontAsset GetChineseFontAsset()
    {
        if (ChineseFontAsset == null)
        {
            ChineseFontAsset = Resources.Load<TMP_FontAsset>(DefaultChineseFontPath);
        }

        return ChineseFontAsset;
    }

    public string GetText(string key)
    {
        if (texts.TryGetValue(key, out var entries)
            && entries.TryGetValue(CurrentLanguage, out string value))
        {
            return value;
        }

        return key;
    }

    public string GetENText(string key)
    {
        if (texts.TryGetValue(key, out var entries)
            && entries.TryGetValue(Language.En, out string value))
        {
            return value;
        }

        return key;
    }

    public void SetLanguage(Language language)
    {
        if (CurrentLanguage == language)
        {
            return;
        }

        CurrentLanguage = language;
        LanguageChanged?.Invoke();
    }
}
public enum Language
{
    Zh,
    En
}
