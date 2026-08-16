using System;
using UnityEngine;

[Serializable]
public sealed class MemoryRetrievalConfig
{
    [Min(1)] public int TopK = 3;

    [Header("Rule-based Score Fusion")]
    [Range(0f, 1f)] public float SimilarityWeight = 0.7f;
    [Range(0f, 1f)] public float ImportanceWeight = 0.3f;

    [Header("Encounter Duration")]
    [Min(1)] public int ExceptionalFastTurnMaximum = 2;
    [Min(1)] public int TypicalTurnMinimum = 3;
    [Min(1)] public int TypicalTurnMaximum = 4;
    [Min(1)] public int ExceptionalSlowTurnMinimum = 7;

    [Header("Final Health")]
    [Range(0f, 1f)] public float CriticalHealthThreshold = 0.25f;
    [Range(0f, 1f)] public float WoundedHealthThreshold = 0.5f;
    [Range(0f, 1f)] public float StrongFinishThreshold = 0.9f;

    [Header("Turn Damage")]
    [Range(0f, 1f)] public float MeaningfulDamageThreshold = 0.25f;
    [Range(0f, 1f)] public float ExceptionalDamageThreshold = 0.5f;

    public int EffectiveTopK => Mathf.Max(1, TopK);

    public void Sanitize()
    {
        TopK = Mathf.Max(1, TopK);
        ExceptionalFastTurnMaximum = Mathf.Max(1, ExceptionalFastTurnMaximum);
        TypicalTurnMinimum = Mathf.Max(
            ExceptionalFastTurnMaximum + 1,
            TypicalTurnMinimum);
        TypicalTurnMaximum = Mathf.Max(TypicalTurnMinimum, TypicalTurnMaximum);
        ExceptionalSlowTurnMinimum = Mathf.Max(
            TypicalTurnMaximum + 1,
            ExceptionalSlowTurnMinimum);

        CriticalHealthThreshold = Mathf.Clamp01(CriticalHealthThreshold);
        WoundedHealthThreshold = Mathf.Clamp(
            WoundedHealthThreshold,
            CriticalHealthThreshold,
            1f);
        StrongFinishThreshold = Mathf.Clamp(
            StrongFinishThreshold,
            WoundedHealthThreshold,
            1f);
        MeaningfulDamageThreshold = Mathf.Clamp01(MeaningfulDamageThreshold);
        ExceptionalDamageThreshold = Mathf.Clamp(
            ExceptionalDamageThreshold,
            MeaningfulDamageThreshold,
            1f);
    }

    public void NormalizeWeights(out float similarityWeight, out float importanceWeight)
    {
        float total = Mathf.Max(0f, SimilarityWeight) + Mathf.Max(0f, ImportanceWeight);
        if (total <= Mathf.Epsilon)
        {
            similarityWeight = 0.7f;
            importanceWeight = 0.3f;
            return;
        }

        similarityWeight = Mathf.Max(0f, SimilarityWeight) / total;
        importanceWeight = Mathf.Max(0f, ImportanceWeight) / total;
    }
}

public enum NarrativeConsequence
{
    None = 0,
    Indirect = 1,
    Irreversible = 2
}

public readonly struct RuleImportanceResult
{
    public int Score { get; }
    public string RuleId { get; }
    public string Reason { get; }
    public float NormalizedScore => Score / 2f;

    public RuleImportanceResult(int score, string ruleId, string reason)
    {
        Score = Mathf.Clamp(score, 0, 2);
        RuleId = ruleId ?? string.Empty;
        Reason = reason ?? string.Empty;
    }
}
