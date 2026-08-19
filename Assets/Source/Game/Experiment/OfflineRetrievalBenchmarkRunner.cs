using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class OfflineRetrievalBenchmarkRunner : MonoBehaviour
{
    private const int ExpectedMemoryCount = 16;

    private static readonly RetrievalStrategy[] Strategies =
    {
        RetrievalStrategy.SimilarityOnly,
        RetrievalStrategy.RuleBasedImportance,
        RetrievalStrategy.ModelAssistedImportance
    };

    [SerializeField] private bool RunOnStart = true;
    [SerializeField] private bool StopPlayModeWhenComplete = true;

    private bool isRunning;

    private void Start()
    {
        if (RunOnStart)
        {
            RunBenchmark();
        }
    }

    [ContextMenu("Run Offline Retrieval Benchmark")]
    public void RunBenchmark()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "The offline retrieval benchmark can only run in Play Mode.");
            return;
        }

        if (isRunning)
        {
            Debug.LogWarning("The offline retrieval benchmark is already running.");
            return;
        }

        StartCoroutine(RunBenchmarkCoroutine());
    }

    private IEnumerator RunBenchmarkCoroutine()
    {
        isRunning = true;
        MemorySystem memorySystem = MemorySystem.Instance;
        if (memorySystem == null)
        {
            Debug.LogError(
                "Offline retrieval benchmark requires a MemorySystem in the scene.");
            FinishRun();
            yield break;
        }

        string sessionCode = $"OFFLINE-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        DialoguePerformanceLogger.BeginSession(
            sessionCode,
            "OfflineRetrievalBenchmark");

        memorySystem.ClearMemories();
        foreach (MemoryEventData memoryEvent in
                 OfflineRetrievalBenchmarkDataset.CreateMemories())
        {
            EventsHandler.TriggerEvent(MemoryEvents.MEMORY_EVENT, memoryEvent);
        }

        if (memorySystem.MemoryPool.Count != ExpectedMemoryCount)
        {
            Debug.LogError(
                $"Offline retrieval benchmark expected {ExpectedMemoryCount} " +
                $"memories but found {memorySystem.MemoryPool.Count}.");
            ExperimentSessionLogLogger.CompleteSession();
            FinishRun();
            yield break;
        }

        // Precompute memory vectors so all measured retrievals start from the same state.
        memorySystem.BeginBossPreparation(includeModelScoring: false);
        yield return memorySystem.WaitForBossPreparation();

        int successCount = 0;
        int requestCount = 0;

        foreach (RetrievalStrategy strategy in Strategies)
        {
            memorySystem.SetRetrievalStrategy(strategy);

            if (strategy == RetrievalStrategy.ModelAssistedImportance)
            {
                memorySystem.BeginBossPreparation(includeModelScoring: true);
                yield return memorySystem.WaitForBossPreparation();
            }

            foreach (OfflineRetrievalQueryDefinition definition in
                     OfflineRetrievalBenchmarkDataset.Queries)
            {
                requestCount++;
                string requestId =
                    $"{sessionCode}-{definition.Id}-{strategy}";
                MemoryQuery query = new MemoryQuery
                {
                    RequestId = requestId,
                    Trigger = definition.TriggerGroup,
                    QueryText = definition.QueryText
                };

                List<MemoryRecord> retrievedMemories = null;
                string retrievalError = null;
                float retrievalMilliseconds = 0f;
                float startedAt = Time.realtimeSinceStartup;

                yield return memorySystem.Retrieve(
                    query,
                    result => retrievedMemories = result,
                    error => retrievalError = error,
                    elapsed => retrievalMilliseconds = elapsed);

                float endToEndMilliseconds =
                    (Time.realtimeSinceStartup - startedAt) * 1000f;
                bool success = string.IsNullOrEmpty(retrievalError);
                if (success)
                {
                    successCount++;
                }

                DialoguePerformanceLogger.Record(
                    new DialoguePerformanceRecord
                    {
                        RequestId = requestId,
                        RetrievalStrategy = strategy.ToString(),
                        Trigger = definition.TriggerGroup,
                        RetrievedMemoryCount = retrievedMemories?.Count ?? 0,
                        RetrievalMilliseconds = retrievalMilliseconds,
                        EndToEndMilliseconds = endToEndMilliseconds,
                        Success = success,
                        Error = retrievalError
                    });

                Debug.Log(
                    $"Offline retrieval completed: query={definition.Id}, " +
                    $"strategy={strategy}, success={success}, " +
                    $"returned={retrievedMemories?.Count ?? 0}.");
            }
        }

        ExperimentSessionLogLogger.CompleteSession();
        Debug.Log(
            $"Offline retrieval benchmark completed: success={successCount}/" +
            $"{requestCount}, output={ExperimentSessionLogLogger.FilePath}");

        FinishRun();
    }

    private void FinishRun()
    {
        isRunning = false;

#if UNITY_EDITOR
        if (StopPlayModeWhenComplete)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
#endif
    }
}
