using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueController : MonoBehaviour
{
    private Unit CurrentUnit;

    private DialogueGenerator Generator;

    private void Awake()
    {
        CurrentUnit = GetComponent<Unit>();
        Generator = new DialogueGenerator();
    }

    public void Speak(string text, float duration = 3f)
    {
        DialogueBubbleManager.Instance.ShowBubble(CurrentUnit, text, duration);
    }

    public void SpeakFromMemory(string queryText)
    {
        MemoryQuery query = new MemoryQuery { QueryText = queryText, Intent = "boss_intro" };
        List<MemoryRecord> memories = MemorySystem.Instance.Retrieve(query, 3);

        string text = Generator.Generate(new DialogueContext
        {
            SpeakerId = "boss",
            SpeakerName = "Cavern Lord",
            TargetName = "Player",
            Intent = "boss_intro",
            Tone = "proud, threatening",
            SceneId = "Boss"
        }, memories);
        Speak(text);
    }

}
