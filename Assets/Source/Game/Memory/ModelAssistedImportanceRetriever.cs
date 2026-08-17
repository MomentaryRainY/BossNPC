using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed class ModelAssistedImportanceRetriever: IMemoryRetriever, IRetrievalTraceProvider
{
    public const string ScoringRubric =
        "Score memory importance from 0 to 2 using these game-specific references:\n" +
        "0 = ordinary or not useful for Rowan's later judgement of the player; " +
        "1 = supporting evidence that may affect Rowan's attitude; " +
        "2 = strong evidence of strategy, risk, justice, loyalty, or narrative intent.\n" +
        "Each record is atomic. Score only the supplied fact and do not infer an " +
        "unrecorded encounter summary.\n" +
        "Turn count: 1-2 turns is exceptionally fast; 3-4 is typical; 5-6 is slow; " +
        "7 or more is exceptionally prolonged.\n" +
        "Remaining health: 90-100% is a strong finish worth remembering; 50-89% is " +
        "stable; 25-49% is wounded; below 25% is critical.\n" +
        "Turn damage is total damage across the entire player turn. At least 50% of " +
        "combined enemy maximum health is exceptional; 25-49% is meaningful; below " +
        "25% is ordinary.\n" +
        "Because cards are constrained by range and stamina, exhausting the available " +
        "hand can show deliberate resource use. Do not describe it as foolish, and do " +
        "not call it successful unless the recorded damage supports that claim.\n" +
        "Narrative choices are important when they strongly affect Rowan's judgement " +
        "through justice, loyalty, mercy, betrayal, or treatment of Rowan's forces.\n" +
        "Judge importance independently from semantic similarity to any retrieval query.";

    private const string RowanAndMinionContext =
        "Rowan is a chivalrous aristocrat with a rigid commitment to justice. He opposes " +
        "the royal family after their persecution of his family and the nearby villages. " +
        "He values loyalty, judges how the player treats subordinates, and initially sees " +
        "the player as a knight who may have been deceived by the Emperor.\n" +
        "Battle1 involved Rowan's inorganic robot guard. It had no emotions but was " +
        "completely loyal to Rowan.\n" +
        "Battle2 involved a captured and coerced guard. Rowan protected the guard's " +
        "family, and the guard reluctantly defended the mountain.\n" +
        "Battle3 involved Rowan's kinsman and willing subordinate, who shared Rowan's " +
        "background and accepted Rowan's goals.\n" +
        "A post-battle choice refers to the defeated character from that battle.";

    private readonly MemoryRetrievalConfig config;
    private readonly ModelImportanceScoreCache scoreCache;

    public MemoryRetrievalTrace LastTrace { get; private set; }

    public ModelAssistedImportanceRetriever(MemoryRetrievalConfig config, ModelImportanceScoreCache scoreCache){
        this.config = config ?? new MemoryRetrievalConfig();
        this.scoreCache = scoreCache ?? throw new ArgumentNullException(nameof(scoreCache));
    }

    public List<MemoryRecord> Retrieve(List<MemoryRecord> memories, MemoryQuery query, int topK){
        if (query?.Vector == null || query.Vector.Length == 0)
        {
            throw new ArgumentException("Model-assisted retrieval requires a query vector.");
        }

        List<ScoredMemory> ranked = (memories ?? new List<MemoryRecord>())
            .Select((memory, index) => new { Memory = memory, OriginalIndex = index })
            .Where(item => item.Memory?.Vector != null &&
                item.Memory.Vector.Length == query.Vector.Length)
            .Select(item => ScoreMemory(item.Memory, query, item.OriginalIndex))
            .OrderByDescending(item => item.FinalScore)
            .ThenByDescending(item => item.RawCosineSimilarity)
            .ThenBy(item => item.OriginalIndex)
            .ToList();

        int selectedCount = Mathf.Min(Mathf.Max(0, topK), ranked.Count);
        config.NormalizeWeights(out float similarityWeight, out float importanceWeight);
        LastTrace = BuildTrace(
            query,
            memories?.Count ?? 0,
            topK,
            similarityWeight,
            importanceWeight,
            ranked,
            selectedCount);

        foreach (MemoryRetrievalTraceEntry entry in LastTrace.Entries) {
            Debug.Log(
                $"Model-assisted retrieval candidate: selected={entry.Selected}, " +
                $"rank={entry.Rank}, query=\"{query.QueryText}\", memory={entry.MemoryId}, " +
                $"cosine={entry.RawCosineSimilarity:F4}, " +
                $"importance={entry.ModelImportanceScore}/2, " +
                $"cacheHit={entry.ModelScoreCacheHit}, final={entry.FinalScore:F4}, " +
                $"reason={entry.ModelReason}, error={entry.ModelScoreError}");
        }

        return ranked
            .Take(selectedCount)
            .Select(item => item.Memory)
            .ToList();
    }

    public static string BuildScoringPrompt(MemoryRecord memory) {
        return BuildBatchScoringPrompt(new List<MemoryRecord> { memory });
    }

    public static string BuildBatchScoringPrompt(IReadOnlyList<MemoryRecord> memories) {
        StringBuilder prompt = new StringBuilder();
        prompt.AppendLine(
            "You are an importance scorer for long-term episodic memories in a " +
            "turn-based game.");
        prompt.AppendLine();
        prompt.AppendLine("[ROWAN AND ENCOUNTER CONTEXT]");
        prompt.AppendLine(RowanAndMinionContext);
        prompt.AppendLine();
        prompt.AppendLine("[SCORING RUBRIC]");
        prompt.AppendLine(ScoringRubric);
        prompt.AppendLine();
        prompt.AppendLine("[MEMORIES TO SCORE]");

        if (memories != null)
        {
            foreach (MemoryRecord memory in memories)
            {
                if (memory == null)
                {
                    continue;
                }

                prompt.AppendLine($"MemoryId: {memory.Id}");
                prompt.AppendLine($"Battle: {memory.BattleId}");
                prompt.AppendLine($"Category: {memory.Category}");
                prompt.AppendLine($"Text: {memory.Text}");
                prompt.AppendLine($"Raw metrics: {BuildRawMetricsSummary(memory)}");
                prompt.AppendLine("---");
            }
        }

        prompt.AppendLine();
        prompt.AppendLine(
            "Return JSON only, with exactly one result for every supplied MemoryId:");
        prompt.AppendLine(
            "{\"scores\":[{\"memoryId\":\"Memory_0\",\"importance\":0," +
            "\"reason\":\"brief evidence-based reason\"}]}");
        prompt.AppendLine(
            "Importance must be an integer from 0 to 2. Do not add markdown or commentary.");
        return prompt.ToString();
    }

    private ScoredMemory ScoreMemory(MemoryRecord memory, MemoryQuery query,int originalIndex){
        float cosineSimilarity = SimilarityOnlyRetriever.CosineSimilarity(
            query.Vector,
            memory.Vector);
        float normalizedSimilarity = Mathf.Clamp01((cosineSimilarity + 1f) * 0.5f);

        bool hasScore = scoreCache.TryGet(memory.Id, out ModelImportanceCacheEntry entry) &&
            entry.HasScore;
        int score = hasScore ? entry.Score : 0;
        float normalizedImportance = score / 2f;
        config.NormalizeWeights(out float similarityWeight, out float importanceWeight);

        string runtimeOrigin = $"runtime:{query.RequestId}";
        bool cacheHit = hasScore && !string.Equals(
            entry.Origin,
            runtimeOrigin,
            StringComparison.Ordinal);

        return new ScoredMemory
        {
            Memory = memory,
            OriginalIndex = originalIndex,
            RawCosineSimilarity = cosineSimilarity,
            NormalizedSimilarity = normalizedSimilarity,
            ModelImportanceScore = hasScore ? score : -1,
            NormalizedImportance = hasScore ? normalizedImportance : 0f,
            ModelReason = hasScore ? entry.Reason : string.Empty,
            ModelScoreCacheHit = cacheHit,
            ModelScoreError = hasScore
                ? string.Empty
                : entry?.Error ?? "No model-assisted importance score was available.",
            FinalScore = similarityWeight * normalizedSimilarity +
                importanceWeight * normalizedImportance
        };
    }

    private static string BuildRawMetricsSummary(MemoryRecord memory) {
        MemoryEventMetrics metrics = memory?.Metrics;
        if (metrics == null)
        {
            return "not recorded";
        }

        switch (memory.Category)
        {
            case MemoryCategory.EncounterDuration:
                return $"turnCount={metrics.TurnCount}";

            case MemoryCategory.FinalHealth:
                return $"remainingHealthPercent={Format(metrics.RemainingHealthPercent)}";

            case MemoryCategory.TurnEvent:
                return $"turnIndex={metrics.TurnIndex}, " +
                    $"turnDamage={Format(metrics.TurnDamage)}, " +
                    $"turnDamagePercent={Format(metrics.TurnDamagePercent)}, " +
                    $"handExhausted={metrics.HandExhausted}, " +
                    $"playerHealthPercentAfterTurn=" +
                    $"{Format(metrics.PlayerHealthPercentAfterTurn)}";

            case MemoryCategory.NarrativeChoice:
                return "The choice text is the raw evidence; no rule-based consequence " +
                    "label is supplied to the model.";

            default:
                return "not recorded";
        }
    }

    private static string Format(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static MemoryRetrievalTrace BuildTrace(MemoryQuery query, int poolSize,
        int topK, float similarityWeight, float importanceWeight,
        List<ScoredMemory> ranked, int selectedCount)
    {
        MemoryRetrievalTrace trace = new MemoryRetrievalTrace
        {
            RequestId = query.RequestId,
            Strategy = RetrievalStrategy.ModelAssistedImportance.ToString(),
            Trigger = query.Trigger,
            QueryText = query.QueryText,
            PoolSize = poolSize,
            EligibleMemoryCount = ranked.Count,
            TopK = Mathf.Max(0, topK),
            SimilarityWeight = similarityWeight,
            ImportanceWeight = importanceWeight
        };

        for (int i = 0; i < ranked.Count; i++)
        {
            ScoredMemory item = ranked[i];
            trace.Entries.Add(new MemoryRetrievalTraceEntry
            {
                MemoryId = item.Memory.Id,
                BattleId = item.Memory.BattleId,
                Category = item.Memory.Category.ToString(),
                Text = item.Memory.Text,
                RawCosineSimilarity = item.RawCosineSimilarity,
                NormalizedSimilarity = item.NormalizedSimilarity,
                RuleImportanceScore = -1,
                NormalizedImportance = item.NormalizedImportance,
                RuleId = "not_applicable",
                RuleReason = "Model-assisted retrieval uses the model score fields.",
                ModelImportanceScore = item.ModelImportanceScore,
                ModelReason = item.ModelReason,
                ModelScoreCacheHit = item.ModelScoreCacheHit,
                ModelScoreError = item.ModelScoreError,
                FinalScore = item.FinalScore,
                Selected = i < selectedCount,
                Rank = i + 1
            });
        }

        return trace;
    }

    private sealed class ScoredMemory {
        public MemoryRecord Memory;
        public int OriginalIndex;
        public float RawCosineSimilarity;
        public float NormalizedSimilarity;
        public int ModelImportanceScore;
        public float NormalizedImportance;
        public string ModelReason;
        public bool ModelScoreCacheHit;
        public string ModelScoreError;
        public float FinalScore;
    }
}

public sealed class ModelImportanceScoreCache
{
    private readonly Dictionary<string, ModelImportanceCacheEntry> entries =
        new Dictionary<string, ModelImportanceCacheEntry>();

    public int ValidScoreCount => entries.Values.Count(entry => entry.HasScore);

    public bool TryGet(string memoryId, out ModelImportanceCacheEntry entry)
    {
        if (string.IsNullOrWhiteSpace(memoryId))
        {
            entry = null;
            return false;
        }

        return entries.TryGetValue(memoryId, out entry);
    }

    public bool HasValidScore(string memoryId)
    {
        return TryGet(memoryId, out ModelImportanceCacheEntry entry) && entry.HasScore;
    }

    public void StoreScore(string memoryId, int score, string reason, string origin)
    {
        if (string.IsNullOrWhiteSpace(memoryId))
        {
            return;
        }

        entries[memoryId] = new ModelImportanceCacheEntry
        {
            HasScore = true,
            Score = Mathf.Clamp(score, 0, 2),
            Reason = reason ?? string.Empty,
            Error = string.Empty,
            Origin = origin ?? string.Empty
        };
    }

    public void StoreError(string memoryId, string error, string origin)
    {
        if (string.IsNullOrWhiteSpace(memoryId))
        {
            return;
        }

        entries[memoryId] = new ModelImportanceCacheEntry
        {
            HasScore = false,
            Score = -1,
            Reason = string.Empty,
            Error = error ?? "Unknown model scoring error.",
            Origin = origin ?? string.Empty
        };
    }

    public void Clear()
    {
        entries.Clear();
    }
}

public sealed class ModelImportanceCacheEntry
{
    public bool HasScore;
    public int Score = -1;
    public string Reason = string.Empty;
    public string Error = string.Empty;
    public string Origin = string.Empty;
}

public sealed class ImportanceScoringClient
{
    private readonly string proxyUrl;

    public ImportanceScoringClient(string proxyUrl)
    {
        this.proxyUrl = proxyUrl;
    }

    public IEnumerator Score(IReadOnlyList<MemoryRecord> memories, Action<List<ModelImportanceScoreResult>> onSuccess,
        Action<string> onError, Action<float> onTimingCompleted = null)
    {
        if (string.IsNullOrWhiteSpace(proxyUrl))
        {
            onError?.Invoke("Importance scoring proxy URL is not configured.");
            yield break;
        }

        if (memories == null || memories.Count == 0)
        {
            onSuccess?.Invoke(new List<ModelImportanceScoreResult>());
            yield break;
        }

        string json = JsonUtility.ToJson(new ScoringRequest
        {
            prompt = ModelAssistedImportanceRetriever.BuildBatchScoringPrompt(memories)
        });

        float startedAt = Time.realtimeSinceStartup;
        using UnityWebRequest request = new UnityWebRequest(proxyUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 120;

        yield return request.SendWebRequest();

        float elapsedMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;
        onTimingCompleted?.Invoke(elapsedMilliseconds);

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(
                $"Importance scoring request failed: {request.error}\n" +
                request.downloadHandler.text);
            yield break;
        }

        try
        {
            ScoringEnvelope envelope = JsonUtility.FromJson<ScoringEnvelope>(
                request.downloadHandler.text);
            string scoreJson = ExtractJsonObject(envelope?.text);
            ScoringResponse response = JsonUtility.FromJson<ScoringResponse>(scoreJson);

            if (response?.scores == null)
            {
                onError?.Invoke("Importance scoring response did not contain a scores array.");
                yield break;
            }

            List<ModelImportanceScoreResult> results = new List<ModelImportanceScoreResult>();
            foreach (ScoringResultDto score in response.scores)
            {
                if (score == null || string.IsNullOrWhiteSpace(score.memoryId))
                {
                    continue;
                }

                results.Add(new ModelImportanceScoreResult
                {
                    MemoryId = score.memoryId,
                    Score = Mathf.Clamp(score.importance, 0, 2),
                    Reason = score.reason ?? string.Empty
                });
            }

            onSuccess?.Invoke(results);
        }
        catch (Exception exception)
        {
            onError?.Invoke(
                $"Could not parse importance scoring response: {exception.Message}\n" +
                request.downloadHandler.text);
        }
    }

    private static string ExtractJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new FormatException("The scoring model returned empty text.");
        }

        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start < 0 || end < start)
        {
            throw new FormatException("The scoring model did not return a JSON object.");
        }

        return text.Substring(start, end - start + 1);
    }

    [Serializable]
    private sealed class ScoringRequest
    {
        public string prompt;
    }

    [Serializable]
    private sealed class ScoringEnvelope
    {
        public string text;
    }

    [Serializable]
    private sealed class ScoringResponse
    {
        public ScoringResultDto[] scores = Array.Empty<ScoringResultDto>();
    }

    [Serializable]
    private sealed class ScoringResultDto
    {
        public string memoryId;
        public int importance;
        public string reason;
    }
}

public sealed class ModelImportanceScoreResult
{
    public string MemoryId;
    public int Score;
    public string Reason;
}
