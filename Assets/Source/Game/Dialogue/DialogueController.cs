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
            QueryText = BuildRetrievalQuery(queryText)
        };

        if (MemorySystem.Instance == null)
        {
            Debug.LogError("Cannot retrieve memories because MemorySystem is missing.");
            Speak("So, you finally reached me. (MS missing)", displayDuration);
            yield break;
        }

        List<MemoryRecord> memories = null;
        string retrievalError = null;

        yield return MemorySystem.Instance.Retrieve(
            query,
            3,
            result => memories = result,
            error => retrievalError = error);

        if (!string.IsNullOrEmpty(retrievalError))
        {
            Debug.LogError(retrievalError);
            Speak("So, you finally reached me. (retrieval error)", displayDuration);
            yield break;
        }

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
                Speak("So, you finally reached me. (LLM error)", displayDuration);
            });
    }

    private static string BuildRetrievalQuery(string intent)
    {
        // natural language to build similarity vector
        switch (intent)
        {
            case "boss_intro":
                return "What important actions and choices did the player make before meeting the boss?";
            case "boss_turn_start":
                return "What previous actions show how the player fights and behaves?";
            case "player_turn_high_damage_and_hand_empty":
                return "What memories relate to the player's strongest attacks and exhausting their hand?";
            case "player_turn_high_damage":
                return "What memories describe the player's strongest attacks and combat performance?";
            case "player_turn_hand_empty":
                return "What memories describe the player exhausting their hand during combat?";
            case "boss_hp_below_75":
            case "boss_hp_below_25":
                return "What previous actions show how dangerous the player is to their enemies?";
            case "player_hp_below_50":
            case "player_hp_below_25":
                return "What memories describe the player struggling or finishing a battle wounded?";
            case "boss_defeat":
            case "player_defeat":
                return "What important choices and battle performance should be remembered at the end of this fight?";
            default:
                return intent.Replace('_', ' ');
        }
    }
}
