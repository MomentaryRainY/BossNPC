using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class SingleGameManager : MonoBehaviour
{
    [Header("Run Mode")]
    [SerializeField] private GameRunMode RunMode = GameRunMode.Experiment;

    [Header("Battle Config by Run Mode")]
    [FormerlySerializedAs("BattleConfig")]
    [SerializeField] private BattleConfig ExperimentBattleConfig;
    [SerializeField] private BattleConfig FullMemoryBattleConfig;
    [SerializeField] private BattleConfig ScriptedBattleConfig;

    [Header("Standalone Experiment Condition")]
    [SerializeField] private BossDialogueCondition ExperimentDialogueCondition =
        BossDialogueCondition.SimilarityOnly;

    [SerializeField] private CardDeck InitialOwnedCards;
    [SerializeField] private int CopiesPerCard = 2;
    [SerializeField] private bool StartAutomatically = true;
    [SerializeField] private bool DisableIfGameManagerExists = true;
    [SerializeField] private bool PlayPreBattleSequence;
    [SerializeField] private bool PlayPostBattleSequence;
    [SerializeField] private bool RestartSceneOnDefeat = true;

    [Header("Memory Retrieval Test")]
    [SerializeField] private bool SeedRetrievalTestMemories;
    [SerializeField] private bool ClearMemoryPoolBeforeSeeding = true;

    private RunState CurrentRun;
    private BattleConfig ActiveBattleConfig;
    private bool eventsRegistered;
    private bool battleStarted;

    private void Start()
    {
        if (DisableIfGameManagerExists && GameManager.Instance != null)
        {
            enabled = false;
            return;
        }

        ActiveBattleConfig = ResolveBattleConfig();
        if (ActiveBattleConfig == null)
        {
            Debug.LogError($"SingleGameManager has no BattleConfig for {RunMode} mode.");
            return;
        }

        RegisterEvents();
        ConfigureRetrievalTest();

        if (StartAutomatically)
        {
            StartSingleBattle();
        }
    }

    private void ConfigureRetrievalTest()
    {
        if (SeedRetrievalTestMemories)
        {
            SeedRetrievalTestMemoryPool();
        }
    }

    [ContextMenu("Debug/Seed Retrieval Test Memory Pool")]
    private void SeedRetrievalTestMemoryPool()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Retrieval test memories can only be seeded in Play Mode.");
            return;
        }

        MemorySystem memorySystem = MemorySystem.Instance;
        if (memorySystem == null)
        {
            Debug.LogWarning(
                "SingleGameManager cannot seed retrieval test memories because " +
                "MemorySystem is missing.");
            return;
        }

        if (ClearMemoryPoolBeforeSeeding)
        {
            memorySystem.ClearMemories();
        }

        foreach (MemoryEventData memoryEvent in CreateRetrievalTestMemories())
        {
            EventsHandler.TriggerEvent(MemoryEvents.MEMORY_EVENT, memoryEvent);
        }

        Debug.Log(
            $"Seeded retrieval test memory pool: count={memorySystem.MemoryPool.Count}.");
        memorySystem.DumpMemoryPool();
    }

    private static List<MemoryEventData> CreateRetrievalTestMemories()
    {
        return new List<MemoryEventData>
        {
            MemoryEventFactory.CreateTurnEvent("Battle1", 1, 33f, 0.33f, false, 0.95f),
            MemoryEventFactory.CreateTurnEvent("Battle1", 2, 67f, 0.67f, false, 0.90f),
            MemoryEventFactory.CreateEncounterDuration("Battle1", 2),
            MemoryEventFactory.CreateFinalHealth("Battle1", 0.90f),
            CreateTestChoice(
                "Battle1",
                2,
                "Execute him",
                NarrativeConsequence.Irreversible),

            MemoryEventFactory.CreateTurnEvent("Battle2", 1, 28f, 0.40f, false, 0.91f),
            MemoryEventFactory.CreateTurnEvent("Battle2", 2, 70f, 1f, false, 0.88f),
            MemoryEventFactory.CreateEncounterDuration("Battle2", 2),
            MemoryEventFactory.CreateFinalHealth("Battle2", 0.88f),
            CreateTestChoice(
                "Battle2",
                1,
                "Ignore his situation and keep going.",
                NarrativeConsequence.Indirect),

            MemoryEventFactory.CreateTurnEvent("Battle3", 1, 40f, 0.27f, false, 0.93f),
            MemoryEventFactory.CreateTurnEvent("Battle3", 2, 55.5f, 0.37f, true, 0.90f),
            MemoryEventFactory.CreateTurnEvent("Battle3", 3, 45f, 0.30f, true, 0.87f),
            MemoryEventFactory.CreateEncounterDuration("Battle3", 3),
            MemoryEventFactory.CreateFinalHealth("Battle3", 0.87f),
            CreateTestChoice(
                "Battle3",
                3,
                "Execute him",
                NarrativeConsequence.Irreversible)
        };
    }

    private static MemoryEventData CreateTestChoice(
        string battleId,
        int choiceIndex,
        string choiceText,
        NarrativeConsequence narrativeConsequence)
    {
        return new MemoryEventData
        {
            BattleId = battleId,
            Category = MemoryCategory.NarrativeChoice,
            Text = $"The player selected: {choiceText}.",
            Metrics = new MemoryEventMetrics
            {
                ChoiceIndex = choiceIndex,
                NarrativeConsequence = narrativeConsequence
            }
        };
    }

    public void StartSingleBattle()
    {
        if (battleStarted)
        {
            return;
        }

        if (ActiveBattleConfig == null)
        {
            Debug.LogError($"SingleGameManager requires a {RunMode} BattleConfig.");
            return;
        }

        BattleManager battleManager = FindFirstObjectByType<BattleManager>();
        CardRenderer cardRenderer = FindFirstObjectByType<CardRenderer>();
        CardManager cardManager = FindFirstObjectByType<CardManager>();
        UIManager uiManager = FindFirstObjectByType<UIManager>();
        BattleInputController inputController = FindFirstObjectByType<BattleInputController>();

        if (battleManager == null || cardRenderer == null || cardManager == null ||
            uiManager == null || inputController == null)
        {
            Debug.LogError("SingleGameManager could not find all required battle scene components.");
            return;
        }

        Time.timeScale = 1f;
        battleStarted = true;
        BossDialogueCondition dialogueCondition = ResolveDialogueCondition();
        string dialogueModeLabel = RunMode == GameRunMode.Experiment
            ? $"{RunMode}-{dialogueCondition}"
            : RunMode.ToString();

        DialoguePerformanceLogger.BeginSession(
            $"SINGLE-{SceneManager.GetActiveScene().name}",
            $"Standalone-{dialogueModeLabel}");

        CreateRunState();
        RuntimeBattleState runtimeState = CreateRuntimeState(
            ActiveBattleConfig,
            dialogueCondition);

        cardManager.Init(runtimeState);
        cardRenderer.Init(cardManager.instances);
        uiManager.Init(cardRenderer);
        battleManager.Init(runtimeState, cardManager, uiManager);
        inputController.Init(battleManager);

        if (PlayPreBattleSequence &&
            RunMode == GameRunMode.Experiment &&
            ActiveBattleConfig.PreBattleSequence != null &&
            TutorialManager.Instance != null)
        {
            TutorialManager.Instance.Play(
                ActiveBattleConfig.PreBattleSequence,
                battleManager.GameStart);
            return;
        }

        battleManager.GameStart();
    }

    private void CreateRunState()
    {
        CurrentRun = new RunState
        {
            MaxHealth = ActiveBattleConfig.PlayerMaxHealth,
            MaxStamina = ActiveBattleConfig.MaxStamina
        };

        if (InitialOwnedCards == null || InitialOwnedCards.Cards == null)
        {
            Debug.LogWarning("SingleGameManager has no InitialOwnedCards. Battle will start with an empty deck.");
            return;
        }

        foreach (CardData card in InitialOwnedCards.Cards)
        {
            if (card == null || CurrentRun.OwnedCards.Contains(card))
            {
                continue;
            }

            CurrentRun.OwnedCards.Add(card);
            CurrentRun.DeckConfig.Add(new DeckCardEntry
            {
                Card = card,
                Count = Mathf.Max(0, CopiesPerCard)
            });
        }
    }

    private BossDialogueCondition ResolveDialogueCondition()
    {
        return ExperimentDialogueCondition;
    }

    private BattleConfig ResolveBattleConfig()
    {
        switch (RunMode)
        {
            case GameRunMode.FullMemory:
                return FullMemoryBattleConfig;
            case GameRunMode.Scripted:
                return ScriptedBattleConfig;
            default:
                return ExperimentBattleConfig;
        }
    }

    private RuntimeBattleState CreateRuntimeState(
        BattleConfig config,
        BossDialogueCondition dialogueCondition)
    {
        RuntimeBattleState state = new RuntimeBattleState
        {
            CurrentTurn = 0,
            State = BattleState.Initializing,

            Player = new UnitRuntime
            {
                Prefab = config.PlayerPrefab,
                Config = new UnitConfig
                {
                    MaxHealth = CurrentRun.MaxHealth,
                    MoveRange = config.PlayerMoveRange,
                    MoveSpeed = config.PlayerMoveSpeed
                },
                CurrentHP = CurrentRun.MaxHealth,
                GridPos = config.PlayerStartPos
            },

            Enemies = new List<UnitRuntime>(),
            MaxStamina = CurrentRun.MaxStamina,
            CurrentStamina = CurrentRun.MaxStamina,
            MaxHandCount = config.MaxHandCount,
            CurrentCardDeck = BuildBattleDeck(),
            BattleId = config.name,
            CollectGameplayMemories = config.CollectGameplayMemories,
            isBossFight = config.IsBossFight,
            RunMode = this.RunMode,
            DialogueCondition = dialogueCondition
        };

        if (config.Enemies == null)
        {
            Debug.LogWarning("SingleGameManager BattleConfig has no enemy list.");
            return state;
        }

        foreach (EnemySpawnConfig enemy in config.Enemies)
        {
            if (enemy == null || enemy.Config == null)
            {
                continue;
            }

            state.Enemies.Add(new UnitRuntime
            {
                Prefab = enemy.Config.EnemyPrefab,
                Config = new UnitConfig
                {
                    type = enemy.Role,
                    MaxHealth = enemy.Config.MaxHealth,
                    MoveRange = enemy.Config.MoveRange,
                    MoveSpeed = enemy.Config.MoveSpeed
                },
                CurrentHP = enemy.Config.MaxHealth,
                GridPos = enemy.GridPos
            });
        }

        return state;
    }

    private List<CardData> BuildBattleDeck()
    {
        List<CardData> deck = new();

        foreach (DeckCardEntry entry in CurrentRun.DeckConfig)
        {
            if (entry.Card == null)
            {
                continue;
            }

            for (int i = 0; i < entry.Count; i++)
            {
                deck.Add(entry.Card);
            }
        }

        return deck;
    }

    private void OnBattleEnd(BattleManager.BattleEndData data)
    {
        Debug.Log($"Single battle ended: {data.Result}");

        if (data.Result == BattleManager.BattleResult.Defeat)
        {
            if (RestartSceneOnDefeat)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            return;
        }

        if (PlayPostBattleSequence &&
            RunMode == GameRunMode.Experiment &&
            ActiveBattleConfig.PostBattleSurveySequence != null &&
            TutorialManager.Instance != null)
        {
            Time.timeScale = 0f;
            TutorialManager.Instance.Play(
                ActiveBattleConfig.PostBattleSurveySequence,
                () =>
                {
                    Time.timeScale = 1f;
                    ShowBattleChoicesIfConfigured();
                });
            return;
        }

        ShowBattleChoicesIfConfigured();
    }

    private void ShowBattleChoicesIfConfigured()
    {
        if (ActiveBattleConfig.ChoiceConfig == null ||
            !ActiveBattleConfig.ChoiceConfig.ShowAfterBattle)
        {
            return;
        }

        if (GlobalUIManager.Instance == null ||
            ActiveBattleConfig.ChoiceConfig.Options == null ||
            ActiveBattleConfig.ChoiceConfig.Options.Length < 4)
        {
            Debug.LogWarning("SingleGameManager cannot show battle choices because GlobalUIManager or choice options are missing.");
            return;
        }

        GlobalUIManager.Instance.SetChoicesText(
            ActiveBattleConfig.ChoiceConfig.Options[0].ChoiceTextKey,
            ActiveBattleConfig.ChoiceConfig.Options[1].ChoiceTextKey,
            ActiveBattleConfig.ChoiceConfig.Options[2].ChoiceTextKey,
            ActiveBattleConfig.ChoiceConfig.Options[3].ChoiceTextKey);

        GlobalUIManager.Instance.ShowChoicePanel();
        Time.timeScale = 0f;
    }

    private void OnBattleChoiceSelected(int choiceIndex)
    {
        Time.timeScale = 1f;

        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.HideChoicePanel();
        }

        BattleChoiceOption option =
            ActiveBattleConfig.ChoiceConfig.Options[choiceIndex - 1];

        if (ActiveBattleConfig.CollectGameplayMemories)
        {
            MemoryEventData memoryEvent = MemoryEventFactory.CreateChoice(
                ActiveBattleConfig.name,
                choiceIndex,
                option);

            EventsHandler.TriggerEvent(MemoryEvents.MEMORY_EVENT, memoryEvent);
        }

        Debug.Log($"Single battle choice selected: {choiceIndex}");
    }

    private void RegisterEvents()
    {
        if (eventsRegistered)
        {
            return;
        }

        EventsHandler.RegisterEvent<BattleManager.BattleEndData>(BattleEvents.END_BATTLE, OnBattleEnd);
        EventsHandler.RegisterEvent<int>(UIEvents.MADE_CHOICE, OnBattleChoiceSelected);
        eventsRegistered = true;
    }

    private void OnDisable()
    {
        if (!eventsRegistered)
        {
            return;
        }

        EventsHandler.UnregisterEvent<BattleManager.BattleEndData>(BattleEvents.END_BATTLE, OnBattleEnd);
        EventsHandler.UnregisterEvent<int>(UIEvents.MADE_CHOICE, OnBattleChoiceSelected);
        eventsRegistered = false;
    }
}
