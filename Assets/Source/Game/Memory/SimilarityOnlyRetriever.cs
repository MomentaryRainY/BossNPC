using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimilarityOnlyRetriever : IMemoryRetriever
{
    public List<MemoryRecord> Retrieve(List<MemoryRecord> memories, MemoryQuery query, int topK)
    {
        if (query?.Vector == null || query.Vector.Length == 0)
        {
            throw new ArgumentException("Similarity retrieval requires a query vector.");
        }

        List<ScoredMemory> ranked = memories
            .Where(memory => memory.Vector != null &&
                memory.Vector.Length == query.Vector.Length)
            .Select((memory, index) => new ScoredMemory
            {
                Memory = memory,
                OriginalIndex = index,
                Score = CosineSimilarity(query.Vector, memory.Vector)
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.OriginalIndex)
            .Take(Mathf.Max(0, topK))
            .ToList();

        foreach (ScoredMemory item in ranked)
        {
            Debug.Log(
                $"Similarity retrieval: query=\"{query.QueryText}\", " +
                $"memory={item.Memory.Id}, score={item.Score:F4}");
        }

        return ranked.Select(item => item.Memory).ToList();
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

    private sealed class ScoredMemory
    {
        public MemoryRecord Memory;
        public int OriginalIndex;
        public float Score;
    }
}
