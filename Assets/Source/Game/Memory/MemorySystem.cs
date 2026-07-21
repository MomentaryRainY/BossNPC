using System.Collections.Generic;
using UnityEngine;

public class MemorySystem : MonoBehaviour
{
    public static MemorySystem Instance;

    List<MemoryRecord> Memories;

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

    private void OnMadeChoice(ChoiceMemoryData data)
    {
        MemoryRecord record = new MemoryRecord
        {
           Id = $"Memory_{uid++}",
           BattleId = data.BattleId,
           EventType = data.EventType,
           Actor = "Player",
           Target = "",
           RelatedCharacter = data.RelatedCharacter,
           RelationToBoss = data.RelationToBoss,
           Text = LocalizationManager.Instance.GetENText(data.MemoryTextKey),
           Importance = data.Importance,
           Recency = 3f
        };
        Memories.Add(record);
        Debug.Log($"Memory recorded: {record.Text}");
    }

    private void OnEnable()
    {
        EventsHandler.RegisterEvent<ChoiceMemoryData>(MemoryEvents.MEMORY_EVENT, OnMadeChoice);
    }

    private void OnDisable()
    {
        EventsHandler.UnregisterEvent<ChoiceMemoryData>(MemoryEvents.MEMORY_EVENT, OnMadeChoice);
    }
}

public class MemoryRecord
{
    public string Id;
    public string BattleId;
    public string EventType;
    public string Actor;
    public string Target;
    public string RelatedCharacter;
    public string RelationToBoss;
    public string Text;
    public int Importance;
    public float Recency;
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

public class ChoiceMemoryData
{
    public string BattleId;
    public int ChoiceIndex;
    public string EventType;
    public string RelatedCharacter;
    public string RelationToBoss;
    public string MemoryTextKey;
    public int Importance;
}