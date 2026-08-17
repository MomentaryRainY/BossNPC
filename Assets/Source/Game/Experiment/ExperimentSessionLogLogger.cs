using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
internal sealed class ExperimentSessionEventRecord
{
    public string RecordType;
    public string SessionCode;
    public string ExperimentMode;
    public string TimestampUtc;
    public string PerformanceSummary;
    public int DialoguePerformanceCount;
    public int DialogueOutputCount;
    public int ImportanceScoringCount;
    public int RetrievalTraceCount;
}

[Serializable]
internal sealed class DialoguePerformanceLogEnvelope
{
    public string RecordType;
    public string SessionCode;
    public string ExperimentMode;
    public string TimestampUtc;
    public DialoguePerformanceRecord Data;
}

[Serializable]
internal sealed class DialogueOutputLogEnvelope
{
    public string RecordType;
    public string SessionCode;
    public string ExperimentMode;
    public string TimestampUtc;
    public DialogueOutputRecord Data;
}

[Serializable]
internal sealed class ImportanceScoringLogEnvelope
{
    public string RecordType;
    public string SessionCode;
    public string ExperimentMode;
    public string TimestampUtc;
    public MemoryImportanceScoringRecord Data;
}

[Serializable]
internal sealed class RetrievalTraceLogEnvelope
{
    public string RecordType;
    public string SessionCode;
    public string ExperimentMode;
    public string TimestampUtc;
    public MemoryRetrievalTrace Data;
}

public static class ExperimentSessionLogLogger
{
    private const string FilePrefix = "experiment_session_";

    private static readonly List<MemoryImportanceScoringRecord> pendingScoringRecords =
        new List<MemoryImportanceScoringRecord>();

    private static string sessionCode = "standalone";
    private static string experimentMode = "standalone";
    private static string filePath;
    private static int dialoguePerformanceCount;
    private static int dialogueOutputCount;
    private static int importanceScoringCount;
    private static int retrievalTraceCount;

    public static string FilePath
    {
        get
        {
            EnsureFilePath();
            return filePath;
        }
    }

    public static void BeginSession(string newSessionCode, string newExperimentMode)
    {
        sessionCode = string.IsNullOrWhiteSpace(newSessionCode)
            ? "standalone"
            : newSessionCode;
        experimentMode = string.IsNullOrWhiteSpace(newExperimentMode)
            ? "standalone"
            : newExperimentMode;
        filePath = BuildFilePath(sessionCode);

        dialoguePerformanceCount = 0;
        dialogueOutputCount = 0;
        importanceScoringCount = 0;
        retrievalTraceCount = 0;
        pendingScoringRecords.Clear();

        try
        {
            EnsureDirectory();
            File.WriteAllText(filePath, string.Empty, Encoding.UTF8);
            AppendSessionEvent("SessionStarted", string.Empty);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to start experiment session log: {exception.Message}");
        }
    }

    public static void UpdateExperimentMode(string confirmedMode)
    {
        if (string.IsNullOrWhiteSpace(confirmedMode))
        {
            return;
        }

        experimentMode = confirmedMode;

        foreach (MemoryImportanceScoringRecord record in pendingScoringRecords)
        {
            record.ExperimentMode = experimentMode;
            AppendImportanceScoring(record);
        }

        pendingScoringRecords.Clear();
        AppendSessionEvent("ModeConfirmed", string.Empty);
    }

    public static void RecordDialoguePerformance(DialoguePerformanceRecord record)
    {
        if (record == null) return;

        dialoguePerformanceCount++;
        AppendJson(new DialoguePerformanceLogEnvelope
        {
            RecordType = "DialoguePerformance",
            SessionCode = sessionCode,
            ExperimentMode = experimentMode,
            TimestampUtc = record.TimestampUtc,
            Data = record
        });
    }

    public static void RecordDialogueOutput(DialogueOutputRecord record)
    {
        if (record == null) return;

        dialogueOutputCount++;
        AppendJson(new DialogueOutputLogEnvelope
        {
            RecordType = "DialogueOutput",
            SessionCode = sessionCode,
            ExperimentMode = experimentMode,
            TimestampUtc = record.TimestampUtc,
            Data = record
        });
    }

    public static void RecordImportanceScoring(MemoryImportanceScoringRecord record)
    {
        if (record == null) return;

        if (string.Equals(experimentMode, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            pendingScoringRecords.Add(record);
            return;
        }

        AppendImportanceScoring(record);
    }

    public static void RecordRetrievalTrace(MemoryRetrievalTrace record)
    {
        if (record == null) return;

        retrievalTraceCount++;
        AppendJson(new RetrievalTraceLogEnvelope
        {
            RecordType = "MemoryRetrievalTrace",
            SessionCode = sessionCode,
            ExperimentMode = experimentMode,
            TimestampUtc = record.TimestampUtc,
            Data = record
        });
    }

    public static void CompleteSession()
    {
        if (pendingScoringRecords.Count > 0)
        {
            foreach (MemoryImportanceScoringRecord record in pendingScoringRecords)
            {
                record.ExperimentMode = experimentMode;
                AppendImportanceScoring(record);
            }

            pendingScoringRecords.Clear();
        }

        AppendSessionEvent(
            "SessionCompleted",
            DialoguePerformanceLogger.BuildCurrentSessionSummary());

        Debug.Log($"Experiment session log ready for upload: {FilePath}");
    }

    private static void AppendImportanceScoring(MemoryImportanceScoringRecord record)
    {
        importanceScoringCount++;
        AppendJson(new ImportanceScoringLogEnvelope
        {
            RecordType = "MemoryImportanceScoring",
            SessionCode = sessionCode,
            ExperimentMode = experimentMode,
            TimestampUtc = record.TimestampUtc,
            Data = record
        });
    }

    private static void AppendSessionEvent(string recordType, string performanceSummary)
    {
        AppendJson(new ExperimentSessionEventRecord
        {
            RecordType = recordType,
            SessionCode = sessionCode,
            ExperimentMode = experimentMode,
            TimestampUtc = DateTime.UtcNow.ToString("O"),
            PerformanceSummary = performanceSummary ?? string.Empty,
            DialoguePerformanceCount = dialoguePerformanceCount,
            DialogueOutputCount = dialogueOutputCount,
            ImportanceScoringCount = importanceScoringCount,
            RetrievalTraceCount = retrievalTraceCount
        });
    }

    private static void AppendJson(object record)
    {
        try
        {
            EnsureDirectory();
            File.AppendAllText(
                FilePath,
                JsonUtility.ToJson(record) + Environment.NewLine,
                Encoding.UTF8);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to write experiment session log: {exception.Message}");
        }
    }

    private static void EnsureFilePath()
    {
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = BuildFilePath(sessionCode);
        }
    }

    private static void EnsureDirectory()
    {
        string directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string BuildFilePath(string code)
    {
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
        {
            code = code.Replace(invalidCharacter, '_');
        }

        return Path.Combine(
            GetGameDirectory(),
            $"{FilePrefix}{code}.jsonl");
    }

    private static string GetGameDirectory()
    {
        DirectoryInfo dataDirectory = Directory.GetParent(Application.dataPath);
        return dataDirectory != null ? dataDirectory.FullName : Application.dataPath;
    }
}
