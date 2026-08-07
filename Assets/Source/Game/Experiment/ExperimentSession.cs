using System;
using UnityEngine;
using UnityEngine.Networking;

public enum ExperimentMode
{
    ModeA,
    ModeB
}

public sealed class ExperimentSession
{
    private const string SessionKey = "Experiment.SessionCode";
    private const string ModeKey = "Experiment.Mode";
    private const string RunInProgressKey = "Experiment.RunInProgress";

    private static readonly BossDialogueCondition[] ModeAOrder =
    {
        BossDialogueCondition.Scripted,
        BossDialogueCondition.SimilarityOnly,
        BossDialogueCondition.RuleBasedImportance,
        BossDialogueCondition.ModelAssistedImportance
    };

    private static readonly BossDialogueCondition[] ModeBOrder =
    {
        BossDialogueCondition.ModelAssistedImportance,
        BossDialogueCondition.RuleBasedImportance,
        BossDialogueCondition.SimilarityOnly,
        BossDialogueCondition.Scripted
    };

    public string SessionCode { get; private set; }
    public ExperimentMode Mode { get; private set; }
    public string PreviousIncompleteSessionCode { get; private set; }

    public void Begin(ExperimentMode mode)
    {
        PreviousIncompleteSessionCode = PlayerPrefs.GetInt(RunInProgressKey, 0) == 1
            ? PlayerPrefs.GetString(SessionKey, string.Empty)
            : string.Empty;

        Mode = mode;
        SessionCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();

        PlayerPrefs.SetString(SessionKey, SessionCode);
        PlayerPrefs.SetInt(ModeKey, (int)Mode);
        PlayerPrefs.SetInt(RunInProgressKey, 1);
        PlayerPrefs.Save();
    }

    public void Complete()
    {
        PlayerPrefs.SetInt(RunInProgressKey, 0);
        PlayerPrefs.Save();
    }

    public BossDialogueCondition ResolveCondition(int bossEncounterIndex, BossDialogueCondition fallback) {
        BossDialogueCondition[] order = Mode == ExperimentMode.ModeA
            ? ModeAOrder
            : ModeBOrder;

        if (bossEncounterIndex < 0 || bossEncounterIndex >= order.Length)
        {
            return fallback;
        }

        return order[bossEncounterIndex];
    }

    public string BuildSurveyUrl(string baseUrl, int encounterNumber) {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return string.Empty;
        }

        string separator = baseUrl.Contains("?") ? "&" : "?";
        return baseUrl
            + separator
            + "session=" + UnityWebRequest.EscapeURL(SessionCode)
            + "&mode=" + UnityWebRequest.EscapeURL(Mode.ToString())
            + "&encounter=" + encounterNumber;
    }
}
