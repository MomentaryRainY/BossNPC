using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class SimilarityOnlyRetriever : IMemoryRetriever, IRetrievalTraceProvider
{
    public MemoryRetrievalTrace LastTrace { get; private set; }

    public List<MemoryRecord> Retrieve(List<MemoryRecord> memories, MemoryQuery query, int topK)
    {
        if (query?.Vector == null || query.Vector.Length == 0)
        {
            throw new ArgumentException("Similarity retrieval requires a query vector.");
        }

        List<ScoredMemory> ranked = (memories ?? new List<MemoryRecord>())
            .Select((memory, index) => new { Memory = memory, OriginalIndex = index })
            .Where(item => item.Memory?.Vector != null &&
                item.Memory.Vector.Length == query.Vector.Length)
            .Select(item => new ScoredMemory
            {
                Memory = item.Memory,
                OriginalIndex = item.OriginalIndex,
                RawCosineSimilarity = CosineSimilarity(query.Vector, item.Memory.Vector)
            })
            .OrderByDescending(item => item.RawCosineSimilarity)
            .ThenBy(item => item.OriginalIndex)
            .ToList();

        int selectedCount = Mathf.Min(Mathf.Max(0, topK), ranked.Count);
        LastTrace = BuildTrace(
            query.RequestId,
            query.Trigger,
            memories?.Count ?? 0,
            query.QueryText,
            topK,
            ranked,
            selectedCount);

        foreach (MemoryRetrievalTraceEntry entry in LastTrace.Entries)
        {
            Debug.Log(
                $"Similarity retrieval candidate: selected={entry.Selected}, " +
                $"rank={entry.Rank}, query=\"{query.QueryText}\", " +
                $"memory={entry.MemoryId}, cosine={entry.RawCosineSimilarity:F4}, " +
                $"normalizedSimilarity={entry.NormalizedSimilarity:F4}");
        }

        return ranked
            .Take(selectedCount)
            .Select(item => item.Memory)
            .ToList();
    }

    public static float CosineSimilarity(float[] left, float[] right)
    {
        if (left == null || right == null || left.Length != right.Length || left.Length == 0)
        {
            throw new ArgumentException("Cosine similarity requires equal non-empty vectors.");
        }

        double dot = 0d;
        double leftMagnitude = 0d;
        double rightMagnitude = 0d;

        for (int i = 0; i < left.Length; i++)
        {
            dot += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        double denominator = Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude);
        return denominator > 0d ? (float)(dot / denominator) : 0f;
    }

    private static MemoryRetrievalTrace BuildTrace(
        string requestId,
        string trigger,
        int poolSize,
        string queryText,
        int topK,
        List<ScoredMemory> ranked,
        int selectedCount)
    {
        MemoryRetrievalTrace trace = new MemoryRetrievalTrace
        {
            RequestId = requestId,
            Strategy = RetrievalStrategy.SimilarityOnly.ToString(),
            Trigger = trigger,
            QueryText = queryText,
            PoolSize = poolSize,
            EligibleMemoryCount = ranked.Count,
            TopK = Mathf.Max(0, topK),
            SimilarityWeight = 1f,
            ImportanceWeight = 0f
        };

        for (int i = 0; i < ranked.Count; i++)
        {
            ScoredMemory item = ranked[i];
            float normalizedSimilarity =
                Mathf.Clamp01((item.RawCosineSimilarity + 1f) * 0.5f);

            trace.Entries.Add(new MemoryRetrievalTraceEntry
            {
                MemoryId = item.Memory.Id,
                BattleId = item.Memory.BattleId,
                Category = item.Memory.Category.ToString(),
                Text = item.Memory.Text,
                RawCosineSimilarity = item.RawCosineSimilarity,
                NormalizedSimilarity = normalizedSimilarity,
                RuleImportanceScore = -1,
                NormalizedImportance = -1f,
                RuleId = "not_applicable",
                RuleReason = "Similarity-only retrieval does not calculate importance.",
                FinalScore = normalizedSimilarity,
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
    }
}
