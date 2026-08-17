using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class MemoryRetrievalTraceEntry
{
    public string MemoryId;
    public string BattleId;
    public string Category;
    public string Text;
    public float RawCosineSimilarity;
    public float NormalizedSimilarity;
    public int RuleImportanceScore;
    public float NormalizedImportance;
    public string RuleId;
    public string RuleReason;
    public int ModelImportanceScore = -1;
    public string ModelReason;
    public bool ModelScoreCacheHit;
    public string ModelScoreError;
    public float FinalScore;
    public bool Selected;
    public int Rank;
}

[Serializable]
public sealed class MemoryImportanceScoringRecord
{
    public string BatchId;
    public string SessionCode;
    public string ExperimentMode;
    public string Origin;
    public int PoolSize;
    public int RequestedMemoryCount;
    public int ScoredMemoryCount;
    public int CacheHitCount;
    public float ScoringMilliseconds;
    public bool Success;
    public string Error;
    public string TimestampUtc;
}

public static class MemoryImportanceScoringLogger
{
    public static string FilePath => ExperimentSessionLogLogger.FilePath;

    public static void Record(MemoryImportanceScoringRecord record)
    {
        if (record == null)
        {
            return;
        }

        record.SessionCode = DialoguePerformanceLogger.CurrentSessionCode;
        record.ExperimentMode = DialoguePerformanceLogger.CurrentExperimentMode;
        record.TimestampUtc = DateTime.UtcNow.ToString("O");
        record.Error = record.Error ?? string.Empty;

        ExperimentSessionLogLogger.RecordImportanceScoring(record);

        Debug.Log(
            $"Memory importance scoring logged: batch={record.BatchId}, " +
            $"origin={record.Origin}, requested={record.RequestedMemoryCount}, " +
            $"scored={record.ScoredMemoryCount}, " +
            $"elapsed={record.ScoringMilliseconds:F1}ms, " +
            $"success={record.Success}, output={FilePath}");
    }
}

[Serializable]
public sealed class MemoryRetrievalTrace
{
    public string RequestId;
    public string SessionCode;
    public string ExperimentMode;
    public string Strategy;
    public string Trigger;
    public string QueryText;
    public int PoolSize;
    public int EligibleMemoryCount;
    public int TopK;
    public float SimilarityWeight;
    public float ImportanceWeight;
    public string TimestampUtc;
    public List<MemoryRetrievalTraceEntry> Entries = new List<MemoryRetrievalTraceEntry>();
}

public interface IRetrievalTraceProvider
{
    MemoryRetrievalTrace LastTrace { get; }
}

public static class MemoryRetrievalTraceLogger
{
    public static string FilePath => ExperimentSessionLogLogger.FilePath;

    public static void Record(MemoryRetrievalTrace trace)
    {
        if (trace == null)
        {
            return;
        }

        trace.TimestampUtc = DateTime.UtcNow.ToString("O");
        trace.SessionCode = DialoguePerformanceLogger.CurrentSessionCode;
        trace.ExperimentMode = DialoguePerformanceLogger.CurrentExperimentMode;

        ExperimentSessionLogLogger.RecordRetrievalTrace(trace);

        Debug.Log(
            $"Memory retrieval trace saved: request={trace.RequestId}, " +
            $"strategy={trace.Strategy}, " +
            $"candidates={trace.EligibleMemoryCount}, topK={trace.TopK}, " +
            $"output={FilePath}");
    }
}
