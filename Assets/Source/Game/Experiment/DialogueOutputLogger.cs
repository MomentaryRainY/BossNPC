using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class DialogueOutputRecord
{
    public string RequestId;
    public string SessionCode;
    public string Mode;
    public string Strategy;
    public string Trigger;
    public List<string> SelectedMemoryIds = new List<string>();
    public List<string> WorkingMemories = new List<string>();
    public string ResponseText;
    public bool Success;
    public string TimestampUtc;
}

public static class DialogueOutputLogger
{
    private const string FileName = "dialogue_output.jsonl";

    public static string FilePath => Path.Combine(GetGameDirectory(), FileName);

    public static void Record(DialogueOutputRecord record)
    {
        if (record == null)
        {
            return;
        }

        record.SessionCode = DialoguePerformanceLogger.CurrentSessionCode;
        record.Mode = DialoguePerformanceLogger.CurrentExperimentMode;
        record.SelectedMemoryIds = record.SelectedMemoryIds ?? new List<string>();
        record.WorkingMemories = record.WorkingMemories ?? new List<string>();
        record.ResponseText = record.ResponseText ?? string.Empty;
        record.TimestampUtc = DateTime.UtcNow.ToString("O");

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
                $"Dialogue output saved: request={record.RequestId}, " +
                $"strategy={record.Strategy}, trigger={record.Trigger}, " +
                $"success={record.Success}, output={FilePath}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to write dialogue output: {exception.Message}");
        }
    }

    private static string GetGameDirectory()
    {
        DirectoryInfo dataDirectory = Directory.GetParent(Application.dataPath);
        return dataDirectory != null ? dataDirectory.FullName : Application.dataPath;
    }
}
