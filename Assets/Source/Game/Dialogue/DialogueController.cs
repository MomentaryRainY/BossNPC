using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueController : MonoBehaviour
{
    private const string ScriptedDialogueKeyPrefix = "boss.dialogue.";

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
        StartCoroutine(SpeakFromMemoryCoroutine(queryText, 3f));
    }

    public void SpeakScripted(string intent, float duration = 3f)
    {
        string key = ScriptedDialogueKeyPrefix + intent;
        string content = LocalizationManager.Instance.GetText(key);

        if (content == key)
        {
            Debug.LogWarning($"Missing scripted dialogue key: {key}");
        }

        Speak(content, duration);
    }

    public IEnumerator SpeakScriptedAndWait(string intent, float displayDuration = 3f)
    {
        SpeakScripted(intent, displayDuration);
        yield return new WaitForSecondsRealtime(displayDuration);
    }

    public IEnumerator SpeakFromMemoryAndWait(string queryText, float displayDuration = 3f)
    {
        yield return SpeakFromMemoryCoroutine(queryText, displayDuration);
        yield return new WaitForSecondsRealtime(displayDuration);
    }

    private IEnumerator SpeakFromMemoryCoroutine(string queryText, float displayDuration)
    {
        MemoryQuery query = new MemoryQuery
        {
            QueryText = queryText,
            Intent = queryText
        };

        List<MemoryRecord> memories = MemorySystem.Instance.Retrieve(query, 3);

        DialogueContext context = new DialogueContext
        {
            SpeakerId = "boss",
            SpeakerName = "Cavern Lord",
            TargetName = "Player",
            Intent = queryText,
            Tone = "proud, threatening",
            SceneId = "Boss"
        };

        string prompt = Generator.BuildPrompt(context, memories);

        yield return Generator.Generate(
            prompt,
            text => Speak(text, displayDuration),
            error =>
            {
                Debug.LogError(error);
                Speak("So, you finally reached me.", displayDuration);
            });
    }
}
