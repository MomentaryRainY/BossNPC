using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using static BattleManager;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private CardDeck InitialOwnedCards;

    [Header("Run Mode")]
    [SerializeField] private GameRunMode RunMode = GameRunMode.Experiment;

    [Header("Experiment Battle Flow")]
    [FormerlySerializedAs("BattleConfigs")]
    [SerializeField] private BattleConfig[] ExperimentBattleConfigs;
    [FormerlySerializedAs("BattleNames")]
    [SerializeField] private string[] ExperimentBattleNames;

    [Header("Full-Memory Battle Flow")]
    [SerializeField] private BattleConfig[] FullMemoryBattleConfigs;
    [SerializeField] private string[] FullMemoryBattleNames;

    [Header("Scripted Battle Flow")]
    [SerializeField] private BattleConfig[] ScriptedBattleConfigs;
    [SerializeField] private string[] ScriptedBattleNames;

    private BattleConfig[] BattleConfigs;
    private string[] BattleNames;

    public RunState CurrentRun { get; private set; }

    [Header("Experiment")]
    [SerializeField] private bool RandomizeExperimentMode = true;
    [SerializeField] private ExperimentMode ConfiguredExperimentMode = ExperimentMode.ModeA;
    [FormerlySerializedAs("PostBattleSurveyUrl")]
    [SerializeField] private string SurveyUrl;

    private int CurrentSceneNum;

    private RuntimeBattleState RTBS;
    private readonly ExperimentSession experimentSession = new();
    private readonly HashSet<int> shownPreBattleSequences = new();
    public TransitionContext CurrentTransitionContext { get; private set; }

    public string ExperimentSessionCode => experimentSession.SessionCode;
    public ExperimentMode CurrentExperimentMode => experimentSession.Mode;
    public GameRunMode CurrentRunMode => RunMode;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ConfigureActiveBattleFlow();
        ValidateActiveBattleFlow();

    }
    private void OnLoadingNextScene()
    {
        CurrentSceneNum++;

        if (CurrentSceneNum >= BattleNames.Length)
        {
            SceneManager.LoadScene("GameStart");
            return;
        }

        SceneManager.LoadScene(BattleNames[CurrentSceneNum]);
    }

    private void OnPlayAgain()
    {
        if (CurrentSceneNum >= BattleNames.Length)
        {
            SceneManager.LoadScene("GameStart");
            return;
        }

        SceneManager.LoadScene(BattleNames[CurrentSceneNum]);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (System.Array.Exists(
            BattleNames,
            battleName => battleName == scene.name))
        {
            StartBattle();
        }
    }

    private void OnBattleEnd(BattleEndData data)
    {
        if (data.Result == BattleResult.Defeat)
        {
            CurrentTransitionContext = null;
            Time.timeScale = 1f;
            SceneManager.LoadScene(BattleNames[CurrentSceneNum]);
            return;
        }

        BattleConfig config = BattleConfigs[CurrentSceneNum];

        if (RunMode != GameRunMode.Experiment)
        {
            ContinueAfterBattleResult(config);
            return;
        }

        PlayPostBattleSurveySequence(config);
    }

    private void PlayPostBattleSurveySequence(BattleConfig config)
    {
        TutorialSequence sequence = config.PostBattleSurveySequence;

        if (sequence == null || TutorialManager.Instance == null)
        {
            ContinueAfterBattleResult(config);
            return;
        }

        Time.timeScale = 0f;
        TutorialManager.Instance.Play(sequence, () =>
        {
            Time.timeScale = 1f;
            ContinueAfterBattleResult(config);
        });
    }

    public void OpenExperimentSurvey()
    {
        if (RunMode != GameRunMode.Experiment)
        {
            Debug.LogWarning("The survey is only available in Experiment run mode.");
            return;
        }

        int encounterNumber = GetBossEncounterIndex(CurrentSceneNum) + 1;
        string url = experimentSession.BuildSurveyUrl(SurveyUrl, encounterNumber);

        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning("Experiment survey URL is not configured.");
            return;
        }

        Debug.Log(
            $"Opening survey: session={experimentSession.SessionCode}, " +
            $"mode={experimentSession.Mode}, encounter={encounterNumber}.");

        Application.OpenURL(url);
    }

    private void ContinueAfterBattleResult(BattleConfig config)
    {
        if (config.ChoiceConfig != null &&
            config.ChoiceConfig.ShowAfterBattle)
        {
            GlobalUIManager.Instance.SetChoicesText(
                config.ChoiceConfig.Options[0].ChoiceTextKey,
                config.ChoiceConfig.Options[1].ChoiceTextKey,
                config.ChoiceConfig.Options[2].ChoiceTextKey,
                config.ChoiceConfig.Options[3].ChoiceTextKey);

            GlobalUIManager.Instance.ShowChoicePanel();
            Time.timeScale = 0f;
            return;
        }

        ContinueAfterBattle(config);
    }

    private void ContinueAfterBattle(BattleConfig config)
    {
        bool isLastBattle = CurrentSceneNum >= BattleNames.Length - 1;

        if (isLastBattle)
        {
            if (RunMode == GameRunMode.Experiment)
            {
                experimentSession.Complete();
            }

            SceneManager.LoadScene("GameEnd");
            return;
        }

        int oldStamina = CurrentRun.MaxStamina;
        int oldHealth = CurrentRun.MaxHealth;

        if (!config.UseTransitionAfterBattle)
        {
            OnLoadingNextScene();
            return;
        }

        int nextIndex = CurrentSceneNum + 1;
        BattleConfig nextConfig = nextIndex < BattleConfigs.Length ? BattleConfigs[nextIndex] : null;

        if (config.ShowLevelUpPage && nextConfig != null)
        {
            CurrentRun.MaxStamina = nextConfig.MaxStamina;
            CurrentRun.MaxHealth = nextConfig.PlayerMaxHealth;
        }

        if (config.ShowRewardCardPage)
        {
            AddOwnedCard(config.RewardCard);
        }

        CurrentTransitionContext = new TransitionContext
        {
            ShowLevelUpPage = config.ShowLevelUpPage,
            ShowRewardCardPage = config.ShowRewardCardPage,
            RewardCard = config.ShowRewardCardPage ? config.RewardCard : null,
            ShowCardsPage = true,
            LevelUp = new LevelUpInfo
            {
                OldStamina = oldStamina,
                NewStamina = CurrentRun.MaxStamina,
                OldHealth = oldHealth,
                NewHealth = CurrentRun.MaxHealth
            }
        };

        SceneManager.LoadScene("Transition");
    }

    void StartBattle()
    {
        BattleManager CurrentBattleManager = FindFirstObjectByType<BattleManager>();
        CardRenderer CurrentCardRenderer = FindFirstObjectByType<CardRenderer>();
        CardManager CurrentCardManager = FindFirstObjectByType<CardManager>();
        UIManager CurrentUIManager = FindFirstObjectByType<UIManager>();
        BattleInputController CurrentBattleInputController = FindFirstObjectByType<BattleInputController>();

        RTBS = CreateRuntimeState(BattleConfigs[CurrentSceneNum]);

        Debug.Log("1 CardManager Init");
        CurrentCardManager.Init(RTBS);

        Debug.Log("2 CardRenderer Init");
        CurrentCardRenderer.Init(CurrentCardManager.instances);

        Debug.Log("3 UIManager Init");
        CurrentUIManager.Init(CurrentCardRenderer);

        Debug.Log("4 BattleManager Init");
        CurrentBattleManager.Init(RTBS, CurrentCardManager, CurrentUIManager);

        Debug.Log("5 Input Init");
        CurrentBattleInputController.Init(CurrentBattleManager);

        Debug.Log("6 GameStart");
        BattleConfig currentConfig = BattleConfigs[CurrentSceneNum];
        TutorialSequence preBattleSequence =
            RunMode != GameRunMode.Experiment && currentConfig.IsBossFight
                ? null
                : currentConfig.PreBattleSequence;

        if (preBattleSequence == null || shownPreBattleSequences.Contains(CurrentSceneNum))
        {
            CurrentBattleManager.GameStart();
            return;
        }

        PlaySequenceOrContinue(preBattleSequence, () =>
        {
            shownPreBattleSequences.Add(CurrentSceneNum);
            CurrentBattleManager.GameStart();
        });
    }
    private void PlaySequenceOrContinue(TutorialSequence sequence, System.Action onComplete)
    {
        if (sequence != null && TutorialManager.Instance != null)
        {
            TutorialManager.Instance.Play(sequence, onComplete);
            return;
        }

        onComplete?.Invoke();
    }

    public RuntimeBattleState CreateRuntimeState(BattleConfig config)
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
                GridPos = config.PlayerStartPos,
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
            DialogueCondition = ResolveDialogueCondition(config)
        };

        foreach (EnemySpawnConfig enemy in config.Enemies)
        {
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

    public void StartNewRun()
    {
        ConfigureActiveBattleFlow();
        if (!ValidateActiveBattleFlow())
        {
            return;
        }

        CurrentSceneNum = 0;
        CurrentRun = new RunState();
        shownPreBattleSequences.Clear();

        if (RunMode == GameRunMode.Experiment)
        {
            ExperimentMode mode = RandomizeExperimentMode
                ? (ExperimentMode)Random.Range(0, 3)
                : ConfiguredExperimentMode;

            experimentSession.Begin(mode, modeConfirmed: false);
            DialoguePerformanceLogger.BeginSession(
                experimentSession.SessionCode,
                "Pending");

            if (!string.IsNullOrEmpty(experimentSession.PreviousIncompleteSessionCode))
            {
                Debug.LogWarning(
                    $"Discarding incomplete experiment session " +
                    $"{experimentSession.PreviousIncompleteSessionCode} and starting a new run.");
            }

            Debug.Log(
                $"Experiment started: session={experimentSession.SessionCode}, " +
                "mode=Pending.");
        }
        else
        {
            string sessionCode = $"{RunMode}-" +
                System.Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            DialoguePerformanceLogger.BeginSession(sessionCode, RunMode.ToString());
            Debug.Log($"{RunMode} run started: session={sessionCode}.");
        }

        MemorySystem.Instance.ClearMemories();

        BattleConfig firstConfig = BattleConfigs[CurrentSceneNum];
        CurrentRun.MaxStamina = firstConfig.MaxStamina;
        CurrentRun.MaxHealth = firstConfig.PlayerMaxHealth;

        foreach (CardData card in InitialOwnedCards.Cards)
        {
            AddOwnedCard(card);
        }

        foreach (CardData card in CurrentRun.OwnedCards)
        {
            TrySetDeckCount(card, 2);
        }

        SceneManager.LoadScene(BattleNames[CurrentSceneNum]);
    }

    public void SetExperimentMode(ExperimentMode mode)
    {
        if (RunMode != GameRunMode.Experiment)
        {
            Debug.LogWarning("Experiment mode cannot be set outside an Experiment run.");
            return;
        }

        RandomizeExperimentMode = false;
        ConfiguredExperimentMode = mode;

        if (!string.IsNullOrEmpty(experimentSession.SessionCode))
        {
            experimentSession.SetMode(mode);
            DialoguePerformanceLogger.UpdateExperimentMode(mode.ToString());
        }
    }

    public bool TrySetExperimentModeCode(string input, out string normalizedCode)
    {
        normalizedCode = string.Empty;
        if (RunMode != GameRunMode.Experiment)
        {
            return false;
        }

        if (!ExperimentSession.TryParseModeCode(input, out ExperimentMode mode))
        {
            return false;
        }

        SetExperimentMode(mode);
        normalizedCode = mode.ToString().Substring("Mode".Length);
        Debug.Log($"Experiment mode confirmed: {mode}.");
        return true;
    }

    private BossDialogueCondition ResolveDialogueCondition(BattleConfig config)
    {
        if (!config.IsBossFight || RunMode != GameRunMode.Experiment)
        {
            return BossDialogueCondition.SimilarityOnly;
        }

        int encounterIndex = GetBossEncounterIndex(CurrentSceneNum);
        return experimentSession.ResolveCondition(encounterIndex);
    }

    private void ConfigureActiveBattleFlow()
    {
        switch (RunMode)
        {
            case GameRunMode.FullMemory:
                BattleConfigs = FullMemoryBattleConfigs;
                BattleNames = FullMemoryBattleNames;
                break;

            case GameRunMode.Scripted:
                BattleConfigs = ScriptedBattleConfigs;
                BattleNames = ScriptedBattleNames;
                break;

            default:
                BattleConfigs = ExperimentBattleConfigs;
                BattleNames = ExperimentBattleNames;
                break;
        }
    }

    private bool ValidateActiveBattleFlow()
    {
        if (BattleConfigs == null || BattleNames == null ||
            BattleConfigs.Length == 0 || BattleNames.Length == 0)
        {
            Debug.LogError($"{RunMode} battle flow is empty.");
            return false;
        }

        if (BattleConfigs.Length != BattleNames.Length)
        {
            Debug.LogError(
                $"{RunMode} battle flow has {BattleConfigs.Length} configs but " +
                $"{BattleNames.Length} scene names.");
            return false;
        }

        return true;
    }

    private int GetBossEncounterIndex(int sceneIndex)
    {
        int bossEncounterIndex = -1;
        int lastIndex = Mathf.Min(sceneIndex, BattleConfigs.Length - 1);

        for (int i = 0; i <= lastIndex; i++)
        {
            if (BattleConfigs[i] != null && BattleConfigs[i].IsBossFight)
            {
                bossEncounterIndex++;
            }
        }

        return bossEncounterIndex;
    }

    public void AddOwnedCard(CardData card)
    {
        if (card == null) return;

        if (!CurrentRun.OwnedCards.Contains(card))
        {
            CurrentRun.OwnedCards.Add(card);
        }
    }

    public bool TrySetDeckCount(CardData card, int count)
    {
        if (card == null) return false;
        if (!CurrentRun.OwnedCards.Contains(card)) return false;

        count = Mathf.Clamp(count, 0, CurrentRun.MaxCopiesPerCard);

        DeckCardEntry entry = CurrentRun.DeckConfig.Find(e => e.Card == card);
        int oldCount = entry != null ? entry.Count : 0;

        if (entry == null)
        {
            if (count == 0) return true;

            CurrentRun.DeckConfig.Add(new DeckCardEntry
            {
                Card = card,
                Count = count
            });
        }
        else
        {
            if (count == 0)
            {
                CurrentRun.DeckConfig.Remove(entry);
            }
            else
            {
                entry.Count = count;
            }
        }

        return true;
    }
    public int GetDeckCardCount(CardData card)
    {
        DeckCardEntry entry = CurrentRun.DeckConfig.Find(e => e.Card == card);
        return entry != null ? entry.Count : 0;
    }

    public List<CardData> BuildBattleDeck()
    {
        List<CardData> result = new();

        foreach (DeckCardEntry entry in CurrentRun.DeckConfig)
        {
            if (entry.Card == null) continue;

            for (int i = 0; i < entry.Count; i++)
            {
                result.Add(entry.Card);
            }
        }

        return result;
    }

    private void OnBattleChoiceSelected(int choiceIndex)
    {
        Time.timeScale = 1f;
        GlobalUIManager.Instance.HideChoicePanel();
        BattleConfig config = BattleConfigs[CurrentSceneNum];
        BattleChoiceOption option = config.ChoiceConfig.Options[choiceIndex - 1];

        if (config.CollectGameplayMemories)
        {
            MemoryEventData memoryEvent = MemoryEventFactory.CreateChoice(
                config.name,
                choiceIndex,
                option);

            EventsHandler.TriggerEvent(MemoryEvents.MEMORY_EVENT, memoryEvent);
        }

        ContinueAfterBattle(config);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EventsHandler.RegisterEvent(SceneEvents.NEXT_SCENE, OnLoadingNextScene);
        EventsHandler.RegisterEvent<BattleEndData>(BattleEvents.END_BATTLE, OnBattleEnd);
        EventsHandler.RegisterEvent<int>(UIEvents.MADE_CHOICE, OnBattleChoiceSelected);
        //EventsHandler.RegisterEvent(SceneEvents.TRY_AGAIN, OnPlayAgain);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EventsHandler.UnregisterEvent(SceneEvents.NEXT_SCENE, OnLoadingNextScene);
        EventsHandler.UnregisterEvent<BattleEndData>(BattleEvents.END_BATTLE, OnBattleEnd);
        EventsHandler.UnregisterEvent<int>(UIEvents.MADE_CHOICE, OnBattleChoiceSelected);
        //EventsHandler.UnregisterEvent(SceneEvents.TRY_AGAIN, OnPlayAgain);
    }
}

public class TransitionContext
{
    public bool ShowLevelUpPage;
    public bool ShowRewardCardPage;
    public CardData RewardCard;
    public bool ShowCardsPage;

    public LevelUpInfo LevelUp;
}

public class RuntimeBattleState
{
    public int CurrentTurn;
    public BattleState State;

    public UnitRuntime Player;
    public List<UnitRuntime> Enemies;

    public int MaxStamina;
    public int CurrentStamina;
    public int MaxHandCount;

    public List<CardData> CurrentCardDeck;
    public string BattleId;
    public bool CollectGameplayMemories;
    public bool isBossFight;
    public GameRunMode RunMode;
    public BossDialogueCondition DialogueCondition;
}

[System.Serializable]
public class RunState
{
    public List<CardData> OwnedCards = new();
    public List<DeckCardEntry> DeckConfig = new();

    public int MaxCopiesPerCard = 2;

    public int MaxStamina;
    public int MaxHealth;
}

[System.Serializable]
public class DeckCardEntry
{
    public CardData Card;
    public int Count;
}
