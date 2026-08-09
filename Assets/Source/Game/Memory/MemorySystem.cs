using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MemorySystem : MonoBehaviour
{
    public static MemorySystem Instance;

    private List<MemoryRecord> Memories;

    public IReadOnlyList<MemoryRecord> MemoryPool => Memories;

    [SerializeField] private RetrievalStrategy strategy;
    [SerializeField] private string embeddingProxyUrl = "http://127.0.0.1:3000/embed";

    private IMemoryRetriever retriever;
    private EmbeddingClient embeddingClient;

    private int uid = 0;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Memories = new List<MemoryRecord>();
        embeddingClient = new EmbeddingClient(embeddingProxyUrl);
        SetRetrievalStrategy(strategy);
    }

    public void ClearMemories()
    {
        Memories.Clear();
        uid = 0;
    }

    public void SetRetrievalStrategy(RetrievalStrategy nextStrategy)
    {
        strategy = nextStrategy;

        switch (strategy)
        {
            case RetrievalStrategy.SimilarityOnly:
                retriever = new SimilarityOnlyRetriever();
                break;

            case RetrievalStrategy.RuleBasedImportance:
                retriever = new RuleBasedImportanceRetriever();
                break;

            case RetrievalStrategy.ModelAssistedImportance:
                retriever = new ModelAssistedImportanceRetriever();
                break;
        }
    }

    public IEnumerator Retrieve(
        MemoryQuery query,
        int topK,
        Action<List<MemoryRecord>> onSuccess,
        Action<string> onError)
    {
        if (retriever == null)
        {
            SetRetrievalStrategy(strategy);
        }

        if (query == null || string.IsNullOrWhiteSpace(query.QueryText))
        {
            onError?.Invoke("Memory retrieval requires a non-empty query.");
            yield break;
        }

        if (topK <= 0 || Memories.Count == 0)
        {
            onSuccess?.Invoke(new List<MemoryRecord>());
            yield break;
        }

        float retrievalStartedAt = Time.realtimeSinceStartup;

        List<MemoryRecord> memoriesWithoutVectors = Memories
            .Where(memory => memory.Vector == null || memory.Vector.Length == 0)
            .ToList();

        List<string> texts = new List<string> { query.QueryText };
        texts.AddRange(memoriesWithoutVectors.Select(memory => memory.Text));

        List<float[]> vectors = null;
        string embeddingError = null;

        yield return embeddingClient.Embed(
            texts,
            result => vectors = result,
            error => embeddingError = error);

        if (!string.IsNullOrEmpty(embeddingError))
        {
            onError?.Invoke(embeddingError);
            yield break;
        }

        if (vectors == null || vectors.Count != texts.Count)
        {
            onError?.Invoke(
                $"Embedding service returned {vectors?.Count ?? 0} vector(s) " +
                $"for {texts.Count} text(s).");
            yield break;
        }

        query.Vector = vectors[0];

        for (int i = 0; i < memoriesWithoutVectors.Count; i++)
        {
            memoriesWithoutVectors[i].Vector = vectors[i + 1];
        }

        List<MemoryRecord> retrievedMemories;
        try
        {
            retrievedMemories = retriever.Retrieve(Memories, query, topK);
        }
        catch (Exception exception)
        {
            onError?.Invoke(
                $"{strategy} retrieval failed: {exception.Message}");
            yield break;
        }

        float elapsedMilliseconds = (Time.realtimeSinceStartup - retrievalStartedAt) * 1000f;
        Debug.Log($"Memory retrieval completed: strategy={strategy}, " +
            $"pool={Memories.Count}, embeddedMemories={memoriesWithoutVectors.Count}, " +
            $"returned={retrievedMemories.Count}, elapsed={elapsedMilliseconds:F1} ms");

        onSuccess?.Invoke(retrievedMemories);
    }

    private void OnMemoryEvent(MemoryEventData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.Text))
        {
            Debug.LogWarning("Ignored an invalid memory event.");
            return;
        }

        MemoryRecord record = new MemoryRecord
        {
           Id = $"Memory_{uid++}",
           BattleId = data.BattleId,
           Category = data.Category,
           Text = data.Text,
           Recency = 3f,
           Metrics = data.Metrics?.Clone() ?? new MemoryEventMetrics(),
           Vector = null
        };
        Memories.Add(record);
        Debug.Log(
            $"Memory recorded: id={record.Id}, category={record.Category}, " +
            $"text={record.Text}");
    }

    [ContextMenu("Debug/Dump Memory Pool")]
    public void DumpMemoryPool()
    {
        Debug.Log($"MemoryPool count: {MemoryPool.Count}");

        foreach (MemoryRecord memory in MemoryPool)
        {
            Debug.Log(
                $"[{memory.Id}] " +
                $"battle={memory.BattleId}, " +
                $"category={memory.Category}, " +
                $"vectorDimensions={memory.Vector?.Length ?? 0}, " +
                $"text={memory.Text}, " +
                $"turns={memory.Metrics.TurnCount}, " +
                $"health={memory.Metrics.RemainingHealthPercent:P0}, " +
                $"emptyHand={memory.Metrics.EmptyHandTurnCount}, " +
                $"highestDamage={memory.Metrics.HighestTurnDamage}, " +
                $"highestDamagePercent={memory.Metrics.HighestTurnDamagePercent:P0}");
        }
    }

    private void OnEnable()
    {
        EventsHandler.RegisterEvent<MemoryEventData>(MemoryEvents.MEMORY_EVENT, OnMemoryEvent);
    }

    private void OnDisable()
    {
        EventsHandler.UnregisterEvent<MemoryEventData>(MemoryEvents.MEMORY_EVENT, OnMemoryEvent);
    }
}

public class MemoryRecord
{
    public string Id;
    public string BattleId;
    public MemoryCategory Category;
    public string Text;
    public float Recency;
    public MemoryEventMetrics Metrics;
    public float[] Vector;
}

public interface IMemoryRetriever
{
    List<MemoryRecord> Retrieve(
        List<MemoryRecord> memories,
        MemoryQuery query,
        int topK
    );
}

public sealed class MemoryQuery
{
    public string QueryText;
    public float[] Vector;
}

public enum RetrievalStrategy
{
    SimilarityOnly,
    RuleBasedImportance,
    ModelAssistedImportance
}
