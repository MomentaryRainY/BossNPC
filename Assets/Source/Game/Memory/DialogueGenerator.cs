using System.Collections.Generic;

public class DialogueGenerator
{
    public string Generate(DialogueContext context, List<MemoryRecord> memories)
    {
        if (memories == null || memories.Count == 0)
        {
            return "So, you finally reached me.";
        }

        return $"So, you finally reached me. I remember this: {memories[0].Text}";
    }

}

public class DialogueContext
{
    public string SpeakerId;
    public string SpeakerName;
    public string TargetName;
    public string Intent;
    public string Tone;
    public string SceneId;
}