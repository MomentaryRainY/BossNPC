using System.Collections.Generic;
using UnityEngine;

public sealed class FullMemoryRetriever : IMemoryRetriever
{
    public List<MemoryRecord> Retrieve(
        List<MemoryRecord> memories,
        MemoryQuery query,
        int topK)
    {
        List<MemoryRecord> result = memories != null
            ? new List<MemoryRecord>(memories)
            : new List<MemoryRecord>();

        Debug.Log(
            $"Full-memory retrieval: pool={result.Count}, returned={result.Count}, " +
            "embedding=false, ranking=false.");
        return result;
    }
}
