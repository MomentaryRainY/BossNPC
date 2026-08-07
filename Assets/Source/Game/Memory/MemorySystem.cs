using System.Collections.Generic;
using UnityEngine;

public class MemorySystem : MonoBehaviour
{
    public static MemorySystem Instance;

    private List<MemoryRecord> Memories;

    public IReadOnlyList<MemoryRecord> MemoryPool => Memories;

    [SerializeField] private RetrievalStrategy strategy;

    private IMemoryRetriever retriever;

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

    public List<MemoryRecord> Retrieve(MemoryQuery query, int topK)
    {
        if (retriever == null)
        {
            SetRetrievalStrategy(strategy);
        }

        return retriever.Retrieve(Memories, query, topK);
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
           Metrics = data.Metrics?.Clone() ?? new MemoryEventMetrics()
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
}

public interface IMemoryRetriever
{
    List<MemoryRecord> Retrieve(
        List<MemoryRecord> memories,
        MemoryQuery query,
        int topK
    );
}

public class MemoryQuery
{
    public string QueryText;
    public string SpeakerId;
    public string TargetId;
    public string BattleId;
    public string Intent;
}

public enum RetrievalStrategy
{
    SimilarityOnly,
    RuleBasedImportance,
    ModelAssistedImportance
}
