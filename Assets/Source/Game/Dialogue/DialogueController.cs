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
        StartCoroutine(SpeakFromMemoryCoroutine(queryText, 3f, null));
    }

    public void SpeakFromMemory(
        string queryText,
        IReadOnlyList<string> workingMemories)
    {
        StartCoroutine(SpeakFromMemoryCoroutine(
            queryText,
            3f,
            CopyWorkingMemories(workingMemories)));
    }

    public IEnumerator SpeakFromMemoryAndWait(string queryText, float displayDuration = 3f)
    {
        yield return SpeakFromMemoryCoroutine(queryText, displayDuration, null);
        yield return new WaitForSecondsRealtime(displayDuration);
    }

    public IEnumerator SpeakFromMemoryAndWait(
        string queryText,
        IReadOnlyList<string> workingMemories,
        float displayDuration = 3f)
    {
        yield return SpeakFromMemoryCoroutine(
            queryText,
            displayDuration,
            CopyWorkingMemories(workingMemories));
        yield return new WaitForSecondsRealtime(displayDuration);
    }

    private IEnumerator SpeakFromMemoryCoroutine(
        string queryText,
        float displayDuration,
        List<string> workingMemories)
    {
        float responseStartedAt = Time.realtimeSinceStartup;
        string requestId = System.Guid.NewGuid().ToString("N");
        MemoryQuery query = new MemoryQuery
        {
            RequestId = requestId,
            Trigger = queryText,
            QueryText = BuildRetrievalQuery(queryText)
        };

        if (MemorySystem.Instance == null)
        {
            Debug.LogError("Cannot retrieve memories because MemorySystem is missing.");
            RecordRetrievalFailure(
                requestId,
                queryText,
                "Unavailable",
                "MemorySystem is missing.",
                0f,
                responseStartedAt,
                workingMemories?.Count ?? 0);
            Speak("So, you finally reached me. (MS missing)", displayDuration);
            yield break;
        }

        List<MemoryRecord> memories = null;
        string retrievalError = null;
        float retrievalMilliseconds = 0f;

        yield return MemorySystem.Instance.Retrieve(
            query,
            result => memories = result,
            error => retrievalError = error,
            elapsed => retrievalMilliseconds = elapsed);

        if (!string.IsNullOrEmpty(retrievalError))
        {
            Debug.LogError(retrievalError);
            RecordRetrievalFailure(
                requestId,
                queryText,
                MemorySystem.Instance.CurrentStrategy.ToString(),
                retrievalError,
                retrievalMilliseconds,
                responseStartedAt,
                workingMemories?.Count ?? 0);
            Speak("So, you finally reached me. (retrieval error)", displayDuration);
            yield break;
        }

        DialogueContext context = new DialogueContext
        {
            SpeakerId = "boss",
            SpeakerName = "Rowan Serenade",
            TargetName = "the player knight",
            Intent = queryText,
            Tone = "formal, controlled, concise, admonitory, with restrained anger",
            SceneId = "Rowan's snow-mountain boss encounter",
            OutputLanguage = GetOutputLanguage()
        };

        string prompt = Generator.BuildPrompt(context, memories, workingMemories);
        string generatedText = null;
        string generationError = null;
        float generationMilliseconds = 0f;

        yield return Generator.Generate(
            prompt,
            text => generatedText = text,
            error => generationError = error,
            elapsed => generationMilliseconds = elapsed);

        float endToEndMilliseconds =
            (Time.realtimeSinceStartup - responseStartedAt) * 1000f;

        DialoguePerformanceLogger.Record(new DialoguePerformanceRecord
        {
            RequestId = requestId,
            RetrievalStrategy = MemorySystem.Instance.CurrentStrategy.ToString(),
            Trigger = queryText,
            PromptCharacters = prompt.Length,
            PromptUtf8Bytes = System.Text.Encoding.UTF8.GetByteCount(prompt),
            RetrievedMemoryCount = memories?.Count ?? 0,
            EncounterMemoryCount = workingMemories?.Count ?? 0,
            RetrievalMilliseconds = retrievalMilliseconds,
            GenerationMilliseconds = generationMilliseconds,
            EndToEndMilliseconds = endToEndMilliseconds,
            ResponseCharacters = generatedText?.Length ?? 0,
            Success = string.IsNullOrEmpty(generationError),
            Error = generationError
        });

        if (!string.IsNullOrEmpty(generationError))
        {
            Debug.LogError(generationError);
            Speak("So, you finally reached me. (LLM error)", displayDuration);
            yield break;
        }

        Speak(generatedText, displayDuration);
    }

    private static void RecordRetrievalFailure(
        string requestId,
        string trigger,
        string strategy,
        string error,
        float retrievalMilliseconds,
        float responseStartedAt,
        int encounterMemoryCount)
    {
        DialoguePerformanceLogger.Record(new DialoguePerformanceRecord
        {
            RequestId = requestId,
            RetrievalStrategy = strategy,
            Trigger = trigger,
            EncounterMemoryCount = encounterMemoryCount,
            RetrievalMilliseconds = retrievalMilliseconds,
            EndToEndMilliseconds =
                (Time.realtimeSinceStartup - responseStartedAt) * 1000f,
            Success = false,
            Error = error
        });
    }

    private static List<string> CopyWorkingMemories(
        IReadOnlyList<string> workingMemories)
    {
        if (workingMemories == null || workingMemories.Count == 0)
        {
            return null;
        }

        List<string> copy = new List<string>(workingMemories.Count);
        for (int i = 0; i < workingMemories.Count; i++)
        {
            copy.Add(workingMemories[i]);
        }

        return copy;
    }

    private static string GetOutputLanguage()
    {
        if (LocalizationManager.Instance != null &&
            LocalizationManager.Instance.CurrentLanguage == Language.Zh)
        {
            return "Simplified Chinese";
        }

        return "English";
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
            case "devil_first":
            case "monster_first":
                return "What previous choices help Rowan judge the player's tactical decisions " +
                    "and treatment of Rowan's forces?";
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
