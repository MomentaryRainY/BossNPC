using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class DialoguePerformanceRecord
{
    public string RequestId;
    public string SessionCode;
    public string ExperimentMode;
    public string RetrievalStrategy;
    public string Trigger;
    public int PromptCharacters;
    public int PromptUtf8Bytes;
    public int RetrievedMemoryCount;
    public int EncounterMemoryCount;
    public float RetrievalMilliseconds;
    public float GenerationMilliseconds;
    public float EndToEndMilliseconds;
    public int ResponseCharacters;
    public bool Success;
    public string Error;
    public string TimestampUtc;
}

public static class DialoguePerformanceLogger
{
    private const string FileName = "dialogue_performance.jsonl";

    private static readonly List<DialoguePerformanceRecord> currentSessionRecords =
        new List<DialoguePerformanceRecord>();

    private static string currentSessionCode = "standalone";
    private static string currentExperimentMode = "standalone";

    public static string FilePath => Path.Combine(GetGameDirectory(), FileName);
    public static string CurrentSessionCode => currentSessionCode;
    public static string CurrentExperimentMode => currentExperimentMode;
    public static IReadOnlyList<DialoguePerformanceRecord> CurrentSessionRecords =>
        currentSessionRecords;

    public static void BeginSession(string sessionCode, string experimentMode)
    {
        currentSessionCode = string.IsNullOrWhiteSpace(sessionCode)
            ? "standalone"
            : sessionCode;
        currentExperimentMode = string.IsNullOrWhiteSpace(experimentMode)
            ? "standalone"
            : experimentMode;
        currentSessionRecords.Clear();

        Debug.Log($"Dialogue performance session started: {currentSessionCode}, " +
            $"mode={currentExperimentMode}, output={FilePath}");
    }

    public static void UpdateExperimentMode(string experimentMode)
    {
        if (!string.IsNullOrWhiteSpace(experimentMode))
        {
            currentExperimentMode = experimentMode;
        }
    }

    public static string BuildCurrentSessionSummary()
    {
        StringBuilder summary = new StringBuilder();
        summary.AppendLine($"session={currentSessionCode}");
        summary.AppendLine($"mode={currentExperimentMode}");

        if (currentSessionRecords.Count == 0)
        {
            summary.Append("requests=0");
            return summary.ToString();
        }

        Dictionary<string, List<DialoguePerformanceRecord>> groups =
            new Dictionary<string, List<DialoguePerformanceRecord>>();

        foreach (DialoguePerformanceRecord record in currentSessionRecords)
        {
            string strategy = string.IsNullOrWhiteSpace(record.RetrievalStrategy)
                ? "Unknown"
                : record.RetrievalStrategy;

            if (!groups.TryGetValue(strategy, out List<DialoguePerformanceRecord> records))
            {
                records = new List<DialoguePerformanceRecord>();
                groups.Add(strategy, records);
            }

            records.Add(record);
        }

        foreach (KeyValuePair<string, List<DialoguePerformanceRecord>> group in groups)
        {
            int successCount = 0;
            float retrievalTotal = 0f;
            float generationTotal = 0f;
            float endToEndTotal = 0f;
            float promptTotal = 0f;

            foreach (DialoguePerformanceRecord record in group.Value)
            {
                if (record.Success)
                {
                    successCount++;
                }

                retrievalTotal += record.RetrievalMilliseconds;
                generationTotal += record.GenerationMilliseconds;
                endToEndTotal += record.EndToEndMilliseconds;
                promptTotal += record.PromptCharacters;
            }

            float count = group.Value.Count;
            summary.AppendLine();
            summary.Append(
                $"{group.Key}: requests={group.Value.Count}, success={successCount}, " +
                $"avgRetrievalMs={retrievalTotal / count:F1}, " +
                $"avgGenerationMs={generationTotal / count:F1}, " +
                $"avgEndToEndMs={endToEndTotal / count:F1}, " +
                $"avgPromptChars={promptTotal / count:F0}");
        }

        return summary.ToString();
    }

    public static void Record(DialoguePerformanceRecord record)
    {
        if (record == null)
        {
            return;
        }

        record.SessionCode = currentSessionCode;
        record.ExperimentMode = currentExperimentMode;
        record.TimestampUtc = DateTime.UtcNow.ToString("O");
        record.Error = record.Error ?? string.Empty;

        currentSessionRecords.Add(record);

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
                $"Dialogue metrics: request={record.RequestId}, " +
                $"session={record.SessionCode}, " +
                $"condition={record.RetrievalStrategy}, trigger={record.Trigger}, " +
                $"promptChars={record.PromptCharacters}, retrieved={record.RetrievedMemoryCount}, " +
                $"retrieval={record.RetrievalMilliseconds:F1}ms, " +
                $"generation={record.GenerationMilliseconds:F1}ms, " +
                $"endToEnd={record.EndToEndMilliseconds:F1}ms");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to write dialogue performance data: {exception.Message}");
        }
    }

    private static string GetGameDirectory()
    {
        DirectoryInfo dataDirectory = Directory.GetParent(Application.dataPath);
        return dataDirectory != null ? dataDirectory.FullName : Application.dataPath;
    }
}
