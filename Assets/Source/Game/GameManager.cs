using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static BattleManager;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private CardDeck InitialOwnedCards;

    [SerializeField] public BattleConfig[] BattleConfigs;

    public RunState CurrentRun { get; private set; }

    private string[] BattleNames = { "TeachingScene", "BattleScene1", "BattleScene2", "BattleScene3" };

    private int CurrentSceneNum;

    private RuntimeBattleState RTBS;
    public TransitionContext CurrentTransitionContext { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (BattleConfigs.Length < BattleNames.Length)
        {
            Debug.LogError("battle configs' count < scene count");
        }

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
        if (scene.name == "TeachingScene" || scene.name == "BattleScene1" ||
            scene.name == "BattleScene2" || scene.name == "BattleScene3")
        {
            StartBattle();
        }
    }

    private void OnBattleEnd(BattleEndData data)
    {
        if (data.Result == BattleResult.Defeat)
        {
            CurrentTransitionContext = null;
            SceneManager.LoadScene("GameStart");
            return;
        }

        BattleConfig config = BattleConfigs[CurrentSceneNum];

        bool isLastBattle = CurrentSceneNum >= BattleNames.Length - 1;

        if (isLastBattle)
        {
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
        CurrentBattleManager.GameStart();
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
            CurrentCardDeck = BuildBattleDeck()
        };

        foreach (EnemySpawnConfig enemy in config.Enemies)
        {
            state.Enemies.Add(new UnitRuntime
            {
                Prefab = enemy.Config.EnemyPrefab,
                Config = new UnitConfig
                {
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
        CurrentSceneNum = 0;
        CurrentRun = new RunState();

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

    public void ConfirmDeck()
    {
        // to do, open a temproal card deck
        // not necessary right now
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EventsHandler.RegisterEvent(SceneEvents.NEXT_SCENE, OnLoadingNextScene);
        EventsHandler.RegisterEvent<BattleEndData>(BattleEvents.END_BATTLE, OnBattleEnd);
        //EventsHandler.RegisterEvent(SceneEvents.TRY_AGAIN, OnPlayAgain);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EventsHandler.UnregisterEvent(SceneEvents.NEXT_SCENE, OnLoadingNextScene);
        EventsHandler.UnregisterEvent<BattleEndData>(BattleEvents.END_BATTLE, OnBattleEnd);
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