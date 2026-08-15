using System;
using UnityEngine;
using UnityEngine.Networking;

public enum ExperimentMode
{
    ModeA,
    ModeB,
    ModeC
}

public sealed class ExperimentSession
{
    private const string SessionKey = "Experiment.SessionCode";
    private const string ModeKey = "Experiment.Mode";
    private const string RunInProgressKey = "Experiment.RunInProgress";

    private static readonly BossDialogueCondition[] ModeAOrder =
    {
        BossDialogueCondition.SimilarityOnly,
        BossDialogueCondition.RuleBasedImportance,
        BossDialogueCondition.ModelAssistedImportance
    };

    private static readonly BossDialogueCondition[] ModeBOrder =
    {
        BossDialogueCondition.RuleBasedImportance,
        BossDialogueCondition.ModelAssistedImportance,
        BossDialogueCondition.SimilarityOnly
    };

    private static readonly BossDialogueCondition[] ModeCOrder =
    {
        BossDialogueCondition.ModelAssistedImportance,
        BossDialogueCondition.SimilarityOnly,
        BossDialogueCondition.RuleBasedImportance
    };

    public string SessionCode { get; private set; }
    public ExperimentMode Mode { get; private set; }
    public bool ModeConfirmed { get; private set; }
    public string PreviousIncompleteSessionCode { get; private set; }

    public void Begin(ExperimentMode mode, bool modeConfirmed = false)
    {
        PreviousIncompleteSessionCode = PlayerPrefs.GetInt(RunInProgressKey, 0) == 1
            ? PlayerPrefs.GetString(SessionKey, string.Empty)
            : string.Empty;

        Mode = mode;
        ModeConfirmed = modeConfirmed;
        SessionCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();

        PlayerPrefs.SetString(SessionKey, SessionCode);
        PlayerPrefs.SetInt(ModeKey, (int)Mode);
        PlayerPrefs.SetInt(RunInProgressKey, 1);
        PlayerPrefs.Save();
    }

    public void SetMode(ExperimentMode mode)
    {
        Mode = mode;
        ModeConfirmed = true;
        PlayerPrefs.SetInt(ModeKey, (int)Mode);
        PlayerPrefs.Save();
    }

    public void Complete()
    {
        PlayerPrefs.SetInt(RunInProgressKey, 0);
        PlayerPrefs.Save();
    }

    public BossDialogueCondition ResolveCondition(int bossEncounterIndex) {
        if (!ModeConfirmed)
        {
            Debug.LogWarning(
                "Experiment mode has not been confirmed; using the pending assigned order.");
        }

        BossDialogueCondition[] order = GetOrder(Mode);

        if (bossEncounterIndex < 0 || bossEncounterIndex >= order.Length)
        {
            Debug.LogWarning(
                $"Boss encounter index {bossEncounterIndex} is outside the experiment order.");
            return BossDialogueCondition.SimilarityOnly;
        }

        return order[bossEncounterIndex];
    }

    public static bool TryParseModeCode(string input, out ExperimentMode mode)
    {
        string normalized = input?.Trim().ToUpperInvariant();
        switch (normalized)
        {
            case "A":
                mode = ExperimentMode.ModeA;
                return true;
            case "B":
                mode = ExperimentMode.ModeB;
                return true;
            case "C":
                mode = ExperimentMode.ModeC;
                return true;
            default:
                mode = ExperimentMode.ModeA;
                return false;
        }
    }

    private static BossDialogueCondition[] GetOrder(ExperimentMode mode)
    {
        switch (mode)
        {
            case ExperimentMode.ModeB:
                return ModeBOrder;
            case ExperimentMode.ModeC:
                return ModeCOrder;
            default:
                return ModeAOrder;
        }
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
            + "&mode=" + UnityWebRequest.EscapeURL(
                ModeConfirmed ? Mode.ToString() : "Pending")
            + "&encounter=" + encounterNumber;
    }
}
