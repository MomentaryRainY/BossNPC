using System.Collections.Generic;
using UnityEngine;

public class SingleGameManager : MonoBehaviour
{
    [SerializeField] private BattleConfig BattleConfig;
    [SerializeField] private CardDeck InitialOwnedCards;
    [SerializeField] private int CopiesPerCard = 2;
    [SerializeField] private bool StartAutomatically = true;
    [SerializeField] private bool DisableIfGameManagerExists = true;

    private RunState CurrentRun;
    private bool eventsRegistered;
    private bool battleStarted;

    private void Start()
    {
        if (DisableIfGameManagerExists && GameManager.Instance != null)
        {
            enabled = false;
            return;
        }

        RegisterEvents();

        if (StartAutomatically)
        {
            StartSingleBattle();
        }
    }

    public void StartSingleBattle()
    {
        if (battleStarted)
        {
            return;
        }

        if (BattleConfig == null)
        {
            Debug.LogError("SingleGameManager requires a BattleConfig.");
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

        CreateRunState();
        RuntimeBattleState runtimeState = CreateRuntimeState(BattleConfig);

        cardManager.Init(runtimeState);
        cardRenderer.Init(cardManager.instances);
        uiManager.Init(cardRenderer);
        battleManager.Init(runtimeState, cardManager, uiManager);
        inputController.Init(battleManager);

        battleManager.GameStart();
    }

    private void CreateRunState()
    {
        CurrentRun = new RunState
        {
            MaxHealth = BattleConfig.PlayerMaxHealth,
            MaxStamina = BattleConfig.MaxStamina
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

    private RuntimeBattleState CreateRuntimeState(BattleConfig config)
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
            DialogueCondition = config.DialogueCondition
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
            return;
        }

        if (BattleConfig.ChoiceConfig == null || !BattleConfig.ChoiceConfig.ShowAfterBattle)
        {
            return;
        }

        if (GlobalUIManager.Instance == null ||
            BattleConfig.ChoiceConfig.Options == null ||
            BattleConfig.ChoiceConfig.Options.Length < 3)
        {
            Debug.LogWarning("SingleGameManager cannot show battle choices because GlobalUIManager or choice options are missing.");
            return;
        }

        GlobalUIManager.Instance.SetChoicesText(
            BattleConfig.ChoiceConfig.Options[0].ChoiceTextKey,
            BattleConfig.ChoiceConfig.Options[1].ChoiceTextKey,
            BattleConfig.ChoiceConfig.Options[2].ChoiceTextKey,
            BattleConfig.ChoiceConfig.Options[3].ChoiceTextKey);

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

        BattleChoiceOption option = BattleConfig.ChoiceConfig.Options[choiceIndex - 1];

        if (BattleConfig.CollectGameplayMemories)
        {
            MemoryEventData memoryEvent = MemoryEventFactory.CreateChoice(
                BattleConfig.name,
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
