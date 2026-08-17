using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    private const string FileName = "memory_importance_scoring.jsonl";

    public static string FilePath => Path.Combine(GetGameDirectory(), FileName);

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

        try
        {
            string directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(
                FilePath,
                JsonUtility.ToJson(record) + Environment.NewLine,
                Encoding.UTF8);

            Debug.Log(
                $"Memory importance scoring logged: batch={record.BatchId}, " +
                $"origin={record.Origin}, requested={record.RequestedMemoryCount}, " +
                $"scored={record.ScoredMemoryCount}, " +
                $"elapsed={record.ScoringMilliseconds:F1}ms, " +
                $"success={record.Success}");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Failed to write memory importance scoring data: {exception.Message}");
        }
    }

    private static string GetGameDirectory()
    {
        DirectoryInfo dataDirectory = Directory.GetParent(Application.dataPath);
        return dataDirectory != null ? dataDirectory.FullName : Application.dataPath;
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
    private const string FileName = "memory_retrieval_trace.jsonl";

    public static string FilePath => Path.Combine(GetGameDirectory(), FileName);

    public static void Record(MemoryRetrievalTrace trace)
    {
        if (trace == null)
        {
            return;
        }

        trace.TimestampUtc = DateTime.UtcNow.ToString("O");
        trace.SessionCode = DialoguePerformanceLogger.CurrentSessionCode;
        trace.ExperimentMode = DialoguePerformanceLogger.CurrentExperimentMode;

        try
        {
            string directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(
                FilePath,
                JsonUtility.ToJson(trace) + Environment.NewLine,
                Encoding.UTF8);

            Debug.Log(
                $"Memory retrieval trace saved: request={trace.RequestId}, " +
                $"strategy={trace.Strategy}, " +
                $"candidates={trace.EligibleMemoryCount}, topK={trace.TopK}, " +
                $"output={FilePath}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to write memory retrieval trace: {exception.Message}");
        }
    }

    private static string GetGameDirectory()
    {
        DirectoryInfo dataDirectory = Directory.GetParent(Application.dataPath);
        return dataDirectory != null ? dataDirectory.FullName : Application.dataPath;
    }
}
