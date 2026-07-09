using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [SerializeField] private TextAsset LocalizationCsv;

    public Language CurrentLanguage { get; private set; } = Language.En;

    private readonly Dictionary<string, Dictionary<Language, string>> texts = new();

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

            texts[key] = new Dictionary<Language, string>
                {
                    { Language.Zh, zh },
                    { Language.En, en }
                };
        }
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