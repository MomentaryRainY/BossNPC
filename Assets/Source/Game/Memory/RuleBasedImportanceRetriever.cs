using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RuleBasedImportanceRetriever : IMemoryRetriever
{
    private const float SimilarityWeight = 0.7f;
    private const float ImportanceWeight = 0.3f;

    public List<MemoryRecord> Retrieve(List<MemoryRecord> memories, MemoryQuery query, int topK)
    {
        if (query?.Vector == null || query.Vector.Length == 0)
        {
            throw new ArgumentException("Rule-based retrieval requires a query vector.");
        }

        List<ScoredMemory> ranked = memories
            .Where(memory => memory.Vector != null &&
                memory.Vector.Length == query.Vector.Length)
            .Select((memory, index) => ScoreMemory(memory, query.Vector, index))
            .OrderByDescending(item => item.FinalScore)
            .ThenByDescending(item => item.SemanticSimilarity)
            .ThenBy(item => item.OriginalIndex)
            .Take(Mathf.Max(0, topK))
            .ToList();

        foreach (ScoredMemory item in ranked)
        {
            Debug.Log(
                $"Rule retrieval: query=\"{query.QueryText}\", memory={item.Memory.Id}, " +
                $"similarity={item.SemanticSimilarity:F4}, " +
                $"importance={item.RuleImportance:F4}, final={item.FinalScore:F4}");
        }

        return ranked.Select(item => item.Memory).ToList();
    }

    public static float CalculateImportance(MemoryRecord memory)
    {
        if (memory == null)
        {
            return 0f;
        }

        switch (memory.Category)
        {
            case MemoryCategory.NarrativeChoice:
                return 1f;

            case MemoryCategory.EncounterDuration:
                return CalculateTurnScore(memory.Metrics?.TurnCount ?? -1);

            case MemoryCategory.FinalHealth:
                return CalculateHealthScore(
                    memory.Metrics?.RemainingHealthPercent ?? -1f);

            case MemoryCategory.TurnEvent:
                return CalculateTurnEventImportance(memory.Metrics);

            default:
                return 0f;
        }
    }

    private static ScoredMemory ScoreMemory(
        MemoryRecord memory,
        float[] queryVector,
        int originalIndex)
    {
        float cosineSimilarity = SimilarityOnlyRetriever.CosineSimilarity(
            queryVector,
            memory.Vector);
        float normalizedSimilarity = Mathf.Clamp01((cosineSimilarity + 1f) * 0.5f);
        float ruleImportance = CalculateImportance(memory);

        return new ScoredMemory
        {
            Memory = memory,
            OriginalIndex = originalIndex,
            SemanticSimilarity = normalizedSimilarity,
            RuleImportance = ruleImportance,
            FinalScore = SimilarityWeight * normalizedSimilarity +
                ImportanceWeight * ruleImportance
        };
    }

    private static float CalculateTurnScore(int turnCount)
    {
        if (turnCount < 0)
        {
            return 0f;
        }

        return turnCount <= 3 || turnCount >= 7 ? 1f : 0.4f;
    }

    private static float CalculateHealthScore(float remainingHealthPercent)
    {
        if (remainingHealthPercent < 0f)
        {
            return 0f;
        }

        if (remainingHealthPercent >= 0.75f || remainingHealthPercent < 0.25f)
        {
            return 1f;
        }

        return remainingHealthPercent >= 0.5f ? 0.4f : 0.6f;
    }

    private static float CalculateTurnEventImportance(MemoryEventMetrics metrics)
    {
        if (metrics == null)
        {
            return 0f;
        }

        if (metrics.TurnDamagePercent < 0f)
        {
            return 0f;
        }

        float damageScore;
        if (metrics.TurnDamagePercent >= 0.25f)
        {
            damageScore = 0.9f;
        }
        else if (metrics.TurnDamagePercent >= 0.1f)
        {
            damageScore = 0.5f;
        }
        else
        {
            damageScore = 0.2f;
        }

        if (metrics.HandExhausted && metrics.TurnDamagePercent >= 0.25f)
        {
            return 1f;
        }

        return metrics.HandExhausted
            ? Mathf.Max(0.6f, damageScore)
            : damageScore;
    }

    private sealed class ScoredMemory
    {
        public MemoryRecord Memory;
        public int OriginalIndex;
        public float SemanticSimilarity;
        public float RuleImportance;
        public float FinalScore;
    }
}
