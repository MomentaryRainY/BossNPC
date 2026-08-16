using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class RuleBasedImportanceRetriever : IMemoryRetriever, IRetrievalTraceProvider
{
    private readonly MemoryRetrievalConfig config;

    public MemoryRetrievalTrace LastTrace { get; private set; }

    public RuleBasedImportanceRetriever(MemoryRetrievalConfig config = null)
    {
        this.config = config ?? new MemoryRetrievalConfig();
    }

    public List<MemoryRecord> Retrieve(
        List<MemoryRecord> memories,
        MemoryQuery query,
        int topK)
    {
        if (query?.Vector == null || query.Vector.Length == 0)
        {
            throw new ArgumentException("Rule-based retrieval requires a query vector.");
        }

        List<ScoredMemory> ranked = (memories ?? new List<MemoryRecord>())
            .Select((memory, index) => new { Memory = memory, OriginalIndex = index })
            .Where(item => item.Memory?.Vector != null &&
                item.Memory.Vector.Length == query.Vector.Length)
            .Select(item => ScoreMemory(item.Memory, query.Vector, item.OriginalIndex))
            .OrderByDescending(item => item.FinalScore)
            .ThenByDescending(item => item.RawCosineSimilarity)
            .ThenBy(item => item.OriginalIndex)
            .ToList();

        int selectedCount = Mathf.Min(Mathf.Max(0, topK), ranked.Count);
        config.NormalizeWeights(out float similarityWeight, out float importanceWeight);
        LastTrace = BuildTrace(
            query.RequestId,
            query.Trigger,
            memories?.Count ?? 0,
            query.QueryText,
            topK,
            similarityWeight,
            importanceWeight,
            ranked,
            selectedCount);

        foreach (MemoryRetrievalTraceEntry entry in LastTrace.Entries)
        {
            Debug.Log(
                $"Rule retrieval candidate: selected={entry.Selected}, rank={entry.Rank}, " +
                $"query=\"{query.QueryText}\", memory={entry.MemoryId}, " +
                $"cosine={entry.RawCosineSimilarity:F4}, " +
                $"normalizedSimilarity={entry.NormalizedSimilarity:F4}, " +
                $"importance={entry.RuleImportanceScore}/2, " +
                $"rule={entry.RuleId}, final={entry.FinalScore:F4}, " +
                $"reason={entry.RuleReason}");
        }

        return ranked
            .Take(selectedCount)
            .Select(item => item.Memory)
            .ToList();
    }

    public static RuleImportanceResult CalculateImportance(
        MemoryRecord memory,
        MemoryRetrievalConfig config = null)
    {
        MemoryRetrievalConfig effectiveConfig = config ?? new MemoryRetrievalConfig();
        if (memory == null)
        {
            return Result(0, "invalid_memory", "The memory record is null.");
        }

        switch (memory.Category)
        {
            case MemoryCategory.NarrativeChoice:
                return CalculateChoiceImportance(memory.Metrics);

            case MemoryCategory.EncounterDuration:
                return CalculateTurnScore(
                    memory.Metrics?.TurnCount ?? -1,
                    effectiveConfig);

            case MemoryCategory.FinalHealth:
                return CalculateHealthScore(
                    memory.Metrics?.RemainingHealthPercent ?? -1f,
                    effectiveConfig);

            case MemoryCategory.TurnEvent:
                return CalculateTurnEventImportance(memory.Metrics, effectiveConfig);

            default:
                return Result(0, "unsupported_category", "No rule exists for this category.");
        }
    }

    private ScoredMemory ScoreMemory(
        MemoryRecord memory,
        float[] queryVector,
        int originalIndex)
    {
        float cosineSimilarity = SimilarityOnlyRetriever.CosineSimilarity(
            queryVector,
            memory.Vector);
        float normalizedSimilarity = Mathf.Clamp01((cosineSimilarity + 1f) * 0.5f);
        RuleImportanceResult importance = CalculateImportance(memory, config);
        config.NormalizeWeights(out float similarityWeight, out float importanceWeight);

        return new ScoredMemory
        {
            Memory = memory,
            OriginalIndex = originalIndex,
            RawCosineSimilarity = cosineSimilarity,
            NormalizedSimilarity = normalizedSimilarity,
            Importance = importance,
            FinalScore = similarityWeight * normalizedSimilarity +
                importanceWeight * importance.NormalizedScore
        };
    }

    private static RuleImportanceResult CalculateChoiceImportance(MemoryEventMetrics metrics)
    {
        NarrativeConsequence consequence =
            metrics?.NarrativeConsequence ?? NarrativeConsequence.None;

        switch (consequence)
        {
            case NarrativeConsequence.Irreversible:
                return Result(
                    2,
                    "choice_high_rowan_impact",
                    "The choice strongly affects Rowan's judgement through his values, " +
                    "loyalties, or relationship with the people involved.");
            case NarrativeConsequence.Indirect:
                return Result(
                    1,
                    "choice_moderate_rowan_impact",
                    "The choice gives Rowan a meaningful but indirect signal about the player's attitude.");
            default:
                return Result(
                    0,
                    "choice_no_rowan_impact",
                    "The choice has no recorded effect on Rowan's values, loyalties, or attitude.");
        }
    }

    private static RuleImportanceResult CalculateTurnScore(
        int turnCount,
        MemoryRetrievalConfig config)
    {
        if (turnCount < 0)
        {
            return Result(0, "duration_missing", "Encounter duration was not recorded.");
        }

        if (turnCount <= config.ExceptionalFastTurnMaximum)
        {
            return Result(2, "duration_exceptionally_fast", "The encounter ended exceptionally quickly.");
        }

        if (turnCount >= config.ExceptionalSlowTurnMinimum)
        {
            return Result(2, "duration_exceptionally_slow", "The encounter lasted an exceptionally long time.");
        }

        if (turnCount >= config.TypicalTurnMinimum &&
            turnCount <= config.TypicalTurnMaximum)
        {
            return Result(0, "duration_typical", "The encounter duration was within the expected range.");
        }

        return Result(1, "duration_notable", "The encounter duration was outside the expected range.");
    }

    private static RuleImportanceResult CalculateHealthScore(
        float remainingHealthPercent,
        MemoryRetrievalConfig config)
    {
        if (remainingHealthPercent < 0f)
        {
            return Result(0, "health_missing", "Final player health was not recorded.");
        }

        if (remainingHealthPercent < config.CriticalHealthThreshold)
        {
            return Result(2, "health_critical", "The player completed the encounter at critical health.");
        }

        if (remainingHealthPercent < config.WoundedHealthThreshold)
        {
            return Result(1, "health_wounded", "The player completed the encounter while wounded.");
        }

        if (remainingHealthPercent >= config.StrongFinishThreshold)
        {
            return Result(
                2,
                "health_strong_finish",
                $"The player completed the encounter with at least " +
                $"{Mathf.RoundToInt(config.StrongFinishThreshold * 100f)}% health remaining.");
        }

        return Result(0, "health_stable", "The player's final health was not unusually low.");
    }

    private static RuleImportanceResult CalculateTurnEventImportance(
        MemoryEventMetrics metrics,
        MemoryRetrievalConfig config)
    {
        if (metrics == null || metrics.TurnDamagePercent < 0f)
        {
            return Result(0, "turn_event_missing", "Turn damage was not recorded.");
        }

        bool meaningfulDamage =
            metrics.TurnDamagePercent >= config.MeaningfulDamageThreshold;
        bool exceptionalDamage =
            metrics.TurnDamagePercent >= config.ExceptionalDamageThreshold;

        if (exceptionalDamage)
        {
            return Result(
                2,
                metrics.HandExhausted
                    ? "turn_exceptional_damage_and_hand_exhausted"
                    : "turn_exceptional_damage",
                metrics.HandExhausted
                    ? "The turn combined exceptional damage with deliberate use of the full hand."
                    : "The turn dealt an exceptional share of enemy maximum health.");
        }

        if (metrics.HandExhausted && meaningfulDamage)
        {
            return Result(
                2,
                "turn_meaningful_damage_and_hand_exhausted",
                "The turn combined meaningful damage with deliberate use of the full hand.");
        }

        if (metrics.HandExhausted)
        {
            return Result(
                1,
                "turn_hand_exhausted",
                "The player deliberately used every available card in the hand.");
        }

        if (meaningfulDamage)
        {
            return Result(
                1,
                "turn_meaningful_damage",
                "The turn dealt a meaningful share of enemy maximum health.");
        }

        return Result(0, "turn_ordinary", "The turn contained no exceptional recorded event.");
    }

    private static RuleImportanceResult Result(int score, string ruleId, string reason)
    {
        return new RuleImportanceResult(score, ruleId, reason);
    }

    private static MemoryRetrievalTrace BuildTrace(
        string requestId,
        string trigger,
        int poolSize,
        string queryText,
        int topK,
        float similarityWeight,
        float importanceWeight,
        List<ScoredMemory> ranked,
        int selectedCount)
    {
        MemoryRetrievalTrace trace = new MemoryRetrievalTrace
        {
            RequestId = requestId,
            Strategy = RetrievalStrategy.RuleBasedImportance.ToString(),
            Trigger = trigger,
            QueryText = queryText,
            PoolSize = poolSize,
            EligibleMemoryCount = ranked.Count,
            TopK = Mathf.Max(0, topK),
            SimilarityWeight = similarityWeight,
            ImportanceWeight = importanceWeight
        };

        for (int i = 0; i < ranked.Count; i++)
        {
            ScoredMemory item = ranked[i];
            trace.Entries.Add(new MemoryRetrievalTraceEntry
            {
                MemoryId = item.Memory.Id,
                BattleId = item.Memory.BattleId,
                Category = item.Memory.Category.ToString(),
                Text = item.Memory.Text,
                RawCosineSimilarity = item.RawCosineSimilarity,
                NormalizedSimilarity = item.NormalizedSimilarity,
                RuleImportanceScore = item.Importance.Score,
                NormalizedImportance = item.Importance.NormalizedScore,
                RuleId = item.Importance.RuleId,
                RuleReason = item.Importance.Reason,
                FinalScore = item.FinalScore,
                Selected = i < selectedCount,
                Rank = i + 1
            });
        }

        return trace;
    }

    private sealed class ScoredMemory
    {
        public MemoryRecord Memory;
        public int OriginalIndex;
        public float RawCosineSimilarity;
        public float NormalizedSimilarity;
        public RuleImportanceResult Importance;
        public float FinalScore;
    }
}
