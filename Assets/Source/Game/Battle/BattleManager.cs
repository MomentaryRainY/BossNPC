using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    // chess board
    private Board Board;

    // cards
    private CardManager CurrentCardManager;

    // UI
    private UIManager CurrentUIManager;

    // player
    private Unit CurrentPlayer;

    // enemies
    private List<Unit> CurrentEnemies;

    // gameplay
    private RuntimeBattleState CurrentBattleState;
    private BattleActionQueue Queue; // animation sequence
    private DamageResolver DMGResolver;
    private Unit CurrentBoss;
    private BossDialogueDirector CurrentBossDialogueDirector;
    private BattleActionResult PendingActionResult;
    private float PlayerTurnBossDamage;
    private BattleMemoryTracker MemoryTracker;

    private bool HasMovedThisTurn;
    private bool CanUndoMove;
    private Vector2Int PreviousPlayerCell;
    private Vector2Int LastMovedCell; // not used 

    private BoardPreviewMode CurrentPreviewMode;

    private List<Vector2Int> CurrentReachableCells;

    private enum BoardPreviewMode
    {
        None,
        Move,
        Card
    }

    public enum BattleResult
    {
        Victory,
        Defeat
    }

    public class BattleEndData
    {
        public BattleResult Result;
    }


    private void Awake()
    {
        CurrentEnemies = new List<Unit>();
        Board = FindFirstObjectByType<Board>();
        Queue = new();
        DMGResolver = new();
    }

    public void Init(RuntimeBattleState rtbs, CardManager cardManager, UIManager uiManager)
    {
        CurrentBattleState = rtbs;
        CurrentUIManager = uiManager;
        CurrentCardManager = cardManager;
        MemoryTracker = rtbs.CollectGameplayMemories
            ? new BattleMemoryTracker(rtbs)
            : null;

        // chess board
        Board.Init();

        // player
        CurrentPlayer = Instantiate(rtbs.Player.Prefab);
        CurrentPlayer.Init(Board, CurrentBattleState.Player);
        HPBarView hpBar = CurrentUIManager.CreateHPBar(CurrentPlayer);
        CurrentPlayer.BindHealthBar(hpBar);

        // enemies
        foreach (UnitRuntime enemy in CurrentBattleState.Enemies)
        {
            Unit enemyUnit = Instantiate(enemy.Prefab);
            enemyUnit.Init(Board, enemy);
            HPBarView hpBarE = CurrentUIManager.CreateHPBar(enemyUnit);
            enemyUnit.BindHealthBar(hpBarE);
            CurrentEnemies.Add(enemyUnit);

            if (CurrentBattleState.isBossFight)
            {
                bool isExplicitBoss = enemyUnit.EnemyRole == EnemyType.BOSS;
                bool canUseLegacyFallback =
                    CurrentBossDialogueDirector == null &&
                    enemyUnit.EnemyRole == EnemyType.DEFAULT;

                if (isExplicitBoss || canUseLegacyFallback)
                {
                    BossDialogueDirector director =
                        enemyUnit.GetComponentInChildren<BossDialogueDirector>(true);

                    if (director != null)
                    {
                        CurrentBoss = enemyUnit;
                        CurrentBossDialogueDirector = director;
                        CurrentBossDialogueDirector.Configure(
                            CurrentBattleState.DialogueCondition);
                    }
                }
            }
        }
    }

    public void GameStart()
    {
        ChangeState(BattleState.PlayerTurnStart);
        if(CurrentBattleState.isBossFight)
        {
            if (CurrentBossDialogueDirector != null)
            {
                CurrentBossDialogueDirector.OnBossEncounterStart(CurrentBoss, CurrentBattleState);
            }
        }
    }

    private IEnumerator StartPlayerTurnCoroutine()
    {
        yield return CurrentUIManager.ShowTurnBanner(
            LocalizationManager.Instance.GetText("ui.yourturn"), .7f);

        HasMovedThisTurn = false;
        CanUndoMove = false;
        PlayerTurnBossDamage = 0f;
        MemoryTracker?.StartPlayerTurn();
        CurrentBattleState.CurrentStamina = CurrentBattleState.MaxStamina;
        EventsHandler.TriggerEvent(UIEvents.STAMINA_CHANGE, 
            new StaminaChangedData{
            Current = CurrentBattleState.CurrentStamina,
            Max = CurrentBattleState.MaxStamina
        });
        // Settle Dots dmg

        CurrentBattleState.CurrentTurn++;

        CurrentCardManager.DrawRandomCards(2, Queue);

        if (Queue.HasActions)
        {
            ChangeState(BattleState.PlayerTakingActions);
        }
        else
        {
            ChangeState(BattleState.WaitingForPlayerInput);
        }
    }

    private IEnumerator ExecuteActions()
    {
        BattleActionResult actionResult = PendingActionResult;
        PendingActionResult = null;

        yield return Queue.Execute(actionResult);

        SubmitPlayerActionResult(actionResult);

        if (CurrentBossDialogueDirector != null)
        {
            CurrentBossDialogueDirector.CheckPlayerHpThreshold(CurrentPlayer);
        }

        if (TryGetBattleResult(out BattleResult result))
        {
            if (CurrentBossDialogueDirector != null)
            {
                yield return CurrentBossDialogueDirector.PlayBattleEndDialogue(result);
            }

            RemoveDeadEnemies();
            ChangeState(BattleState.BattleEnd);
            EndBattle(result);
            yield break;
        }

        RemoveDeadEnemies();

        if(CurrentBattleState.State == BattleState.EnemyTakingActions)
        {
            ChangeState(BattleState.EnemyTurnEnd);
        }
        else if(CurrentBattleState.State == BattleState.PlayerTakingActions)
        {
            ChangeState(BattleState.WaitingForPlayerInput);
        }
    }

    public void TryEndTurn()
    {
        if(CanChangeTo(BattleState.PlayerTurnEnd))
        {
            CanUndoMove = false;
            ChangeState(BattleState.PlayerTurnEnd);
        }
    }

    private void EndPlayerTurn()
    {
        bool handIsEmpty = CurrentCardManager.HandCardCount == 0;
        MemoryTracker?.CompletePlayerTurn(handIsEmpty, CurrentPlayer.HPPercent);

        if (CurrentBossDialogueDirector != null && CurrentBoss != null)
        {
            CurrentBossDialogueDirector.OnPlayerTurnEnd(
                PlayerTurnBossDamage,
                CurrentBoss,
                handIsEmpty);
        }

        // settle dots on player or something
        ChangeState(BattleState.EnemyTurnStart);
    }

    private IEnumerator StartEnemyTurnCoroutine()
    {
        yield return CurrentUIManager.ShowTurnBanner(
            LocalizationManager.Instance.GetText("ui.enemyturn"), .7f);

        if (CurrentBossDialogueDirector != null && CurrentBoss != null)
        {
            CurrentBossDialogueDirector.CheckBossHpThreshold(CurrentBoss);
            CurrentBossDialogueDirector.OnBossTurnStart(
                CurrentBoss,
                CurrentPlayer,
                CurrentBattleState);
        }

        HashSet<Vector2Int> reservedCells = new();

        foreach (Enemy enemy in CurrentEnemies)
        {
            reservedCells.Add(enemy.CurrentPos);
        }

        reservedCells.Add(CurrentPlayer.CurrentPos);

        foreach (Enemy enemy in CurrentEnemies)
        {
            enemy.BuildTurnActions(CurrentPlayer, Board, Queue, reservedCells);
        }

        if (Queue.HasActions)
        {
            ChangeState(BattleState.EnemyTakingActions);
        }
        else
        {
            ChangeState(BattleState.EnemyTurnEnd);
        }
    }

    private void EndEnemyTurn()
    {
        ChangeState(BattleState.PlayerTurnStart);
    }

    private void EndBattle(BattleResult result)
    {
        if (result == BattleResult.Victory && MemoryTracker != null)
        {
            MemoryTracker.CompletePlayerTurn(
                CurrentCardManager.HandCardCount == 0,
                CurrentPlayer.HPPercent);

            List<MemoryEventData> memoryEvents = MemoryTracker.BuildVictoryMemories(
                CurrentBattleState.CurrentTurn,
                CurrentPlayer.HPPercent);

            foreach (MemoryEventData memoryEvent in memoryEvents)
            {
                EventsHandler.TriggerEvent(MemoryEvents.MEMORY_EVENT, memoryEvent);
            }
        }

        EventsHandler.TriggerEvent(BattleEvents.END_BATTLE, new BattleEndData { Result = result });
    }

    public bool TryPlayCard(CardInstance card, Unit target)
    {
        if (CurrentBattleState.State != BattleState.WaitingForPlayerInput)
        {
            Debug.LogWarning($"Cannot play card while state is {CurrentBattleState.State}");
            return false;
        }

        if (card == null || target == null)
        {
            return false;
        }

        CardPlayContext context = new(card, CurrentPlayer, target,
            Board, CurrentBattleState, CurrentCardManager, DMGResolver);

        if (CurrentBattleState.CurrentStamina < card.CurrentCost)
        {
            Debug.LogWarning("Not enough stamina.");
            return false;
        }


        if (!CanPlayCard(context))
        {
            return false;
        }

        CurrentBattleState.CurrentStamina -= card.CurrentCost;
        EventsHandler.TriggerEvent(UIEvents.STAMINA_CHANGE, 
            new StaminaChangedData{
            Current = CurrentBattleState.CurrentStamina,
            Max = CurrentBattleState.MaxStamina
        });

        CanUndoMove = false;

        CurrentCardManager.PlayCard(card);

        PendingActionResult = new BattleActionResult(
            CurrentPlayer,
            target,
            card,
            isPlayerAction: true);

        BuildCardActions(context);

        if (Queue.HasActions)
        {
            ChangeState(BattleState.PlayerTakingActions);
        }
        else
        {
            PendingActionResult = null;
        }

        return true;
    }

    private bool CanPlayCard(CardPlayContext context)
    {
        if (context.Card.Data.TargetingRule != null && 
            !context.Card.Data.TargetingRule.IsValidTarget(context))
        {
            Debug.LogWarning("Invalid target.");
            return false;
        }

        foreach (CardCondition condition in context.Card.Data.Conditions)
        {
            if (!condition.IsSatisfied(context))
            {
                Debug.LogWarning($"Violate {condition.GetType()}");
                return false;
            }
        }
        return true;
    }

    private void BuildCardActions(CardPlayContext context)
    {
        foreach(CardEffect effect in context.Card.Data.Effects) {
            effect.BuildActions(context, Queue);
        }
    }

    private void SubmitPlayerActionResult(BattleActionResult result)
    {
        if (result == null || !result.IsPlayerAction)
        {
            return;
        }

        MemoryTracker?.RecordPlayerAction(result);

        if (CurrentBoss == null)
        {
            return;
        }

        PlayerTurnBossDamage += result.GetDamageDealtTo(CurrentBoss);

        if (CurrentBossDialogueDirector != null)
        {
            CurrentBossDialogueDirector.CheckBossHpThreshold(CurrentBoss);
        }
    }

    private bool TryGetBattleResult(out BattleResult result)
    {
        if (CurrentPlayer.State == UnitState.Dead)
        {
            result = BattleResult.Defeat;
            return true;
        }

        foreach (Unit enemy in CurrentEnemies)
        {
            if (enemy.State != UnitState.Dead)
            {
                result = BattleResult.Victory;
                return false;
            }
        }

        result = BattleResult.Victory;
        return true;
    }

    private void RemoveDeadEnemies()
    {
        List<Unit> defeatedTacticalMinions = new List<Unit>();

        foreach (Unit enemy in CurrentEnemies)
        {
            if (enemy == CurrentBoss || enemy.State != UnitState.Dead)
            {
                continue;
            }

            if (enemy.EnemyRole == EnemyType.RANGED_MINION ||
                enemy.EnemyRole == EnemyType.MELEE_MINION)
            {
                defeatedTacticalMinions.Add(enemy);
            }
        }

        if (defeatedTacticalMinions.Count == 1 &&
            CurrentBossDialogueDirector != null &&
            CurrentBoss != null &&
            CurrentBoss.State != UnitState.Dead)
        {
            CurrentBossDialogueDirector.OnBossMinionDefeated(
                defeatedTacticalMinions[0].EnemyRole);
        }
        else if (defeatedTacticalMinions.Count > 1)
        {
            Debug.Log("Boss encounter memory skipped: tactical minions were defeated simultaneously.");
        }

        for (int i = CurrentEnemies.Count - 1; i >= 0; i--)
        {
            Unit enemy = CurrentEnemies[i];
            if (enemy.State != UnitState.Dead) continue;

            if (DialogueBubbleManager.Instance != null)
            {
                DialogueBubbleManager.Instance.RemoveBubble(enemy);
            }

            CurrentEnemies.RemoveAt(i);
            Destroy(enemy.gameObject);
        }
    }

    private void ChangeState(BattleState nextState)
    {
        if (!CanChangeTo(nextState))
        {
            Debug.LogWarning($"Invalid state change: {CurrentBattleState.State} -> {nextState}");
            return;
        }

        CurrentBattleState.State = nextState;

        switch (nextState)
        {
            case BattleState.Initializing:
                DebugInfo("to Initializing");
                break;

            case BattleState.PlayerTurnStart:
                DebugInfo("to PlayerTurnStart");
                StartCoroutine(StartPlayerTurnCoroutine());
                break;

            case BattleState.WaitingForPlayerInput:
                DebugInfo("to WaitingForPlayerInput");
                RefreshInputPreview();
                break;

            case BattleState.PlayerTakingActions:
                DebugInfo("to PlayerTakingActions");
                StartCoroutine(ExecuteActions());
                break;

            case BattleState.PlayerTurnEnd:
                DebugInfo("to PlayerTurnEnd");
                EndPlayerTurn();
                break;

            case BattleState.EnemyTurnStart:
                DebugInfo("to EnemyTurnStart");
                StartCoroutine(StartEnemyTurnCoroutine());
                break;

            case BattleState.EnemyTakingActions:
                DebugInfo("to EnemyTakingActions");
                StartCoroutine(ExecuteActions());
                break;

            case BattleState.EnemyTurnEnd:
                DebugInfo("to EnemyTurnEnd");
                EndEnemyTurn();
                break;

            case BattleState.BattleEnd:
                DebugInfo("to BattleEnd");
                //EndBattle();
                break;
        }
    }

    private void DebugInfo(string msg)
    {
        //Debug.Log(msg);
    }


    private bool CanChangeTo(BattleState nextState)
    {
        BattleState currentState = CurrentBattleState.State;

        switch (currentState)
        {
            case BattleState.Initializing:
                return nextState == BattleState.PlayerTurnStart
                    || nextState == BattleState.BattleEnd;

            case BattleState.PlayerTurnStart:
                return nextState == BattleState.WaitingForPlayerInput
                    || nextState == BattleState.BattleEnd
                    || nextState == BattleState.PlayerTakingActions;

            case BattleState.WaitingForPlayerInput:
                return nextState == BattleState.PlayerTakingActions
                    || nextState == BattleState.PlayerTurnEnd
                    || nextState == BattleState.BattleEnd;

            case BattleState.PlayerTakingActions:
                return nextState == BattleState.WaitingForPlayerInput
                    || nextState == BattleState.BattleEnd;

            case BattleState.PlayerTurnEnd:
                return nextState == BattleState.EnemyTurnStart
                    || nextState == BattleState.BattleEnd;

            case BattleState.EnemyTurnStart:
                return nextState == BattleState.EnemyTakingActions
                    || nextState == BattleState.EnemyTurnEnd
                    || nextState == BattleState.BattleEnd;

            case BattleState.EnemyTakingActions:
                return nextState == BattleState.EnemyTurnEnd
                    || nextState == BattleState.BattleEnd;

            case BattleState.EnemyTurnEnd:
                return nextState == BattleState.PlayerTurnStart
                    || nextState == BattleState.BattleEnd;

            case BattleState.BattleEnd:
                return false;

            default:
                return false;
        }
    }

    public void ShowCardPreview(CardInstance card)
    {
        if (CurrentBattleState.State != BattleState.WaitingForPlayerInput || card == null)
        {
            return;
        }

        List<Vector2Int> cells = GetPreviewCells(card);

        Board.SetHighlightCells(cells, BoardHighlightMode.Card);
        CurrentPreviewMode = BoardPreviewMode.Card;
    }

    private void ShowMovePreview()
    {
        CurrentReachableCells = GetMovePreviewCells();

        Board.SetHighlightCells(CurrentReachableCells, BoardHighlightMode.Move);
        CurrentPreviewMode = BoardPreviewMode.Move;
    }

    private List<Vector2Int> GetMovePreviewCells()
    {
        List<Vector2Int> cells = new();
        Vector2Int origin = CurrentPlayer.CurrentPos;
        int range = CurrentPlayer.MoveRange;

        Vector2Int[] dirs = {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        int[,] dis = new int[Board.BoardWidth, Board.BoardHeight];

        for (int x = 0; x < Board.BoardWidth; x++)
        {
            for (int y = 0; y < Board.BoardHeight; y++)
            {
                dis[x, y] = -1;
            }
        }

        Queue<Vector2Int> queue = new();
        queue.Enqueue(origin);
        dis[origin.x, origin.y] = 0;

        while (queue.Count > 0) {
            Vector2Int current = queue.Dequeue();

            if (dis[current.x, current.y] >= range)
            {
                continue;
            }

            foreach (Vector2Int dir in dirs)
            {
                Vector2Int next = current + dir;

                if (!Board.IsInside(next)) continue;
                if (dis[next.x, next.y] != -1) continue;
                if (!Board.IsWalkable(next)) continue;
                if (Board.IsOccupied(next)) continue;

                dis[next.x, next.y] = dis[current.x, current.y] + 1;
                queue.Enqueue(next);
                cells.Add(next);
            }
        }

        return cells;
    }

    public void ClearCardPreview()
    {
        if (CurrentPreviewMode != BoardPreviewMode.Card)
        {
            return;
        }

        RefreshInputPreview();
    }

    private void RefreshInputPreview()
    {
        if (CurrentBattleState.State != BattleState.WaitingForPlayerInput)
        {
            Board.ClearHighlights();
            CurrentPreviewMode = BoardPreviewMode.None;
            return;
        }

        if (!HasMovedThisTurn)
        {
            ShowMovePreview();
        }
        else
        {
            Board.ClearHighlights();
            CurrentPreviewMode = BoardPreviewMode.None;
        }
    }

    private List<Vector2Int> GetPreviewCells(CardInstance card)
    {
        if (card == null || card.Data == null || card.Data.TargetingRule == null)
        {
            return new List<Vector2Int>();
        }

        CardPlayContext context = new(card, CurrentPlayer, null, Board,
            CurrentBattleState, CurrentCardManager, DMGResolver);

        return card.Data.TargetingRule.GetSelectableCells(context);
    }

    public void PreviewCardOnTarget(CardInstance card, Unit target)
    {
        if (CurrentBattleState.State != BattleState.WaitingForPlayerInput)
        {
            return;
        }

        if (card == null || target == null)
        {
            CurrentUIManager.ClearCardDescriptionPreview(card);
            return;
        }

        CardPlayContext context = new(card, CurrentPlayer, target, Board,
            CurrentBattleState, CurrentCardManager, DMGResolver);

        if (!CanPlayCard(context))
        {
            CurrentUIManager.ClearCardDescriptionPreview(card);
            return;
        }

        CardDescriptionPreview preview = BuildCardDescriptionPreview(context);
        CurrentUIManager.SetCardDescriptionPreview(preview);
    }

    private CardDescriptionPreview BuildCardDescriptionPreview(CardPlayContext context)
    {
        int damage = 0;

        foreach (CardEffect effect in context.Card.Data.Effects)
        {
            if (effect is DamageEffect damageEffect)
            {
                damage = Mathf.RoundToInt(DMGResolver.Resolve(context, damageEffect.CardDamage));
                break;
            }

            if (effect is LinearDamageEffect linearDamageEffect)
            {
                damage = Mathf.RoundToInt(DMGResolver.Resolve(context, linearDamageEffect.CardDamage));
                break;
            }

            if(effect is SquareDamageEffect squareDamageEffect)
            {
                damage = Mathf.RoundToInt(DMGResolver.Resolve(context, squareDamageEffect.CardDamage));
                break;
            }

            if(effect is RectangleDamageEffect rectangleDamageEffect)
            {
                damage = Mathf.RoundToInt(DMGResolver.Resolve(context, rectangleDamageEffect.CardDamage));
                break;
            }
        }

        return new CardDescriptionPreview(context.Card, damage);
    }

    public bool TryMovePlayer(Vector2Int targetCell)
    {
        if (CurrentBattleState.State != BattleState.WaitingForPlayerInput)
        {
            return false;
        }

        if (HasMovedThisTurn)
        {
            Debug.LogWarning("Player has already moved this turn.");
            return false;
        }
        
        if (!IsValidMoveCell(targetCell))
        {
            return false;
        }

        if (!Board.IsWalkable(targetCell)) return false;

        PreviousPlayerCell = CurrentPlayer.CurrentPos;
        LastMovedCell = targetCell;

        HasMovedThisTurn = true;
        CanUndoMove = true;

        Queue.Enqueue(new FaceTargetAction(CurrentPlayer, targetCell));
        List<Vector2Int> path = Board.FindPath(CurrentPlayer.CurrentPos, targetCell, CurrentPlayer.MoveRange);
        Queue.Enqueue(new MoveAction(Board, CurrentPlayer, path));
        ChangeState(BattleState.PlayerTakingActions);

        return true;
    }

    private bool IsValidMoveCell(Vector2Int target)
    {
        if (!Board.IsWalkable(target)) return false;

        if (target == CurrentPlayer.CurrentPos) return false;

        if (!Board.IsInside(target)) return false;

        if (Board.IsOccupied(target)) return false;

        List<Vector2Int> path = Board.FindPath(CurrentPlayer.CurrentPos, target, CurrentPlayer.MoveRange);
        if (path.Count == 0) return false;

        if (CurrentReachableCells == null || !CurrentReachableCells.Contains(target)) return false;

        return true;
    }

    public bool TryUndoMove()
    {
        if (CurrentBattleState.State != BattleState.WaitingForPlayerInput)
        {
            return false;
        }

        if (!HasMovedThisTurn || !CanUndoMove)
        {
            return false;
        }
       
        CurrentPlayer.TeleportTo(Board, PreviousPlayerCell);

        HasMovedThisTurn = false;
        CanUndoMove = false;
        ShowMovePreview();
        return true;
    }
}

public enum BattleState
{
    Initializing,

    PlayerTurnStart,
    WaitingForPlayerInput,
    PlayerTakingActions,
    PlayerTurnEnd,

    EnemyTurnStart,
    EnemyTakingActions,
    EnemyTurnEnd,

    BattleEnd
}
