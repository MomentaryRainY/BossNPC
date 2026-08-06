using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class DialogueGenerator
{
    private string proxyUrl = "http://localhost:3000/dialogue";

    public IEnumerator Generate(string prompt, Action<string> onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(new DialogueRequest
        {
            prompt = prompt
        });

        using UnityWebRequest request = new UnityWebRequest(proxyUrl, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            yield break;
        }

        DialogueResponse response = JsonUtility.FromJson<DialogueResponse>(request.downloadHandler.text);
        Debug.Log($"LLM output: {response.text}");
        onSuccess?.Invoke(response.text);
    }

    [Serializable]
    private class DialogueRequest
    {
        public string prompt;
    }

    [Serializable]
    private class DialogueResponse
    {
        public string text;
    }

    public string BuildPrompt(DialogueContext context, List<MemoryRecord> memories)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"You are {context.SpeakerName}, a boss NPC.");
        sb.AppendLine($"Tone: {context.Tone}");
        sb.AppendLine($"Intent: {context.Intent}");
        sb.AppendLine("Speak in one short line.");
        sb.AppendLine("Use the memories only if relevant.");
        sb.AppendLine();
        sb.AppendLine("Memories:");

        if (memories == null || memories.Count == 0)
        {
            sb.AppendLine("- No relevant memories.");
        }
        else
        {
            foreach (MemoryRecord memory in memories)
            {
                sb.AppendLine($"- {memory.Text}");
            }
        }

        return sb.ToString();
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