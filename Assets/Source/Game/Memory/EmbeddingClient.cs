using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed class EmbeddingClient
{
    private readonly string proxyUrl;

    public EmbeddingClient(string proxyUrl)
    {
        this.proxyUrl = proxyUrl;
    }

    public IEnumerator Embed(
        IReadOnlyList<string> texts,
        Action<List<float[]>> onSuccess,
        Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(proxyUrl))
        {
            onError?.Invoke("Embedding proxy URL is not configured.");
            yield break;
        }

        if (texts == null || texts.Count == 0)
        {
            onError?.Invoke("Embedding request requires at least one text.");
            yield break;
        }

        string[] requestTexts = new string[texts.Count];
        for (int i = 0; i < texts.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(texts[i]))
            {
                onError?.Invoke($"Embedding request text at index {i} is empty.");
                yield break;
            }

            requestTexts[i] = texts[i];
        }

        string json = JsonUtility.ToJson(new EmbeddingRequest
        {
            texts = requestTexts
        });

        using UnityWebRequest request = new UnityWebRequest(proxyUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 120;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(
                $"Embedding request failed: {request.error}\n" +
                request.downloadHandler.text);
            yield break;
        }

        EmbeddingResponse response;
        try
        {
            response = JsonUtility.FromJson<EmbeddingResponse>(
                request.downloadHandler.text);
        }
        catch (Exception exception)
        {
            onError?.Invoke($"Could not parse embedding response: {exception.Message}");
            yield break;
        }

        if (response == null || response.vectors == null)
        {
            onError?.Invoke("Embedding response did not contain vectors.");
            yield break;
        }

        if (response.vectors.Length != requestTexts.Length)
        {
            onError?.Invoke(
                $"Embedding response contained {response.vectors.Length} vector(s) " +
                $"for {requestTexts.Length} text(s).");
            yield break;
        }

        List<float[]> result = new List<float[]>(response.vectors.Length);
        for (int i = 0; i < response.vectors.Length; i++)
        {
            float[] values = response.vectors[i]?.values;
            if (values == null || values.Length == 0)
            {
                onError?.Invoke($"Embedding vector at index {i} is empty.");
                yield break;
            }

            if (response.dimensions > 0 && values.Length != response.dimensions)
            {
                onError?.Invoke(
                    $"Embedding vector at index {i} has {values.Length} dimensions; " +
                    $"expected {response.dimensions}.");
                yield break;
            }

            result.Add(values);
        }

        onSuccess?.Invoke(result);
    }

    [Serializable]
    private sealed class EmbeddingRequest
    {
        public string[] texts;
    }

    [Serializable]
    private sealed class EmbeddingResponse
    {
        public int dimensions = 0;
        public EmbeddingVector[] vectors = Array.Empty<EmbeddingVector>();
    }

    [Serializable]
    private sealed class EmbeddingVector
    {
        public float[] values = Array.Empty<float>();
    }
}
