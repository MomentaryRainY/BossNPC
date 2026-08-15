using System.Collections;
using UnityEngine;

public sealed class ScriptedBossDialogue : MonoBehaviour
{
    private const string DialogueKeyPrefix = "boss.dialogue.";

    private Unit unit;

    private void Awake()
    {
        unit = GetComponentInParent<Unit>();
    }

    public void Speak(string intent, float duration = 3f)
    {
        if (unit == null || DialogueBubbleManager.Instance == null)
        {
            Debug.LogWarning("ScriptedBossDialogue requires a Unit and DialogueBubbleManager.");
            return;
        }

        string key = DialogueKeyPrefix + intent;
        string content = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText(key)
            : key;

        if (content == key)
        {
            Debug.LogWarning($"Missing scripted dialogue key: {key}");
        }

        DialogueBubbleManager.Instance.ShowBubble(unit, content, duration);
    }

    public IEnumerator SpeakAndWait(string intent, float displayDuration = 3f)
    {
        Speak(intent, displayDuration);
        yield return new WaitForSecondsRealtime(displayDuration);
    }
}
