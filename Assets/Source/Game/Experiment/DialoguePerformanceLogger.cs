using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class DialoguePerformanceRecord
{
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

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
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
                $"Dialogue metrics: session={record.SessionCode}, " +
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
}
