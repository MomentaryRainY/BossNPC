using System;
using System.Collections.Generic;
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
    public static string FilePath => ExperimentSessionLogLogger.FilePath;

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

        ExperimentSessionLogLogger.RecordDialogueOutput(record);

        Debug.Log(
            $"Dialogue output saved: request={record.RequestId}, " +
            $"strategy={record.Strategy}, trigger={record.Trigger}, " +
            $"success={record.Success}, output={FilePath}");
    }
}
