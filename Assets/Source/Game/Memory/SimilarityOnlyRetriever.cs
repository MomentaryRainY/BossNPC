using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimilarityOnlyRetriever : IMemoryRetriever
{
    public List<MemoryRecord> Retrieve(List<MemoryRecord> memories, MemoryQuery query, int topK)
    {
        return memories.Take(topK).ToList();
    }
}
