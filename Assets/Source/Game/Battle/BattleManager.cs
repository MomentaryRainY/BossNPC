using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private bool HasMovedThisTurn;
    private bool CanUndoMove;
    private Vector2Int PreviousPlayerCell;
    private Vector2Int LastMovedCell; // not used 

    private BoardPreviewMode CurrentPreviewMode;

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
        }
    }

    public void GameStart()
    {
        ChangeState(BattleState.PlayerTurnStart);
    }

    private IEnumerator StartPlayerTurnCoroutine()
    {
        yield return CurrentUIManager.ShowTurnBanner(
            LocalizationManager.Instance.GetText("ui.yourturn"), .7f);

        HasMovedThisTurn = false;
        CanUndoMove = false;
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
        yield return Queue.Execute();

        if (CheckBattleEnd(out BattleResult result))
        {
            ChangeState(BattleState.BattleEnd);
            EndBattle(result);

        } else if(CurrentBattleState.State == BattleState.EnemyTakingActions)
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

        // settle dots on player or something
        ChangeState(BattleState.EnemyTurnStart);
    }

    private IEnumerator StartEnemyTurnCoroutine()
    {
        yield return CurrentUIManager.ShowTurnBanner(
            LocalizationManager.Instance.GetText("ui.enemyturn"), .7f);

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

        BuildCardActions(context);

        if (Queue.HasActions)
        {
            ChangeState(BattleState.PlayerTakingActions);
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

    private bool CheckBattleEnd(out BattleResult result)
    {
        if (CurrentPlayer.State == UnitState.Dead)
        {
            result = BattleResult.Defeat;
            return true;
        }

        for (int i = CurrentEnemies.Count - 1; i >= 0; i--)
        {
            Unit enemy = CurrentEnemies[i];

            if (enemy.State != UnitState.Dead)
            {
                continue;
            }

            CurrentEnemies.RemoveAt(i);
            Destroy(enemy.gameObject);
        }

        if (CurrentEnemies.Count <= 0)
        {
            result = BattleResult.Victory;
            return true;
        }

        result = BattleResult.Victory;
        return false;
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
        Debug.Log(msg);
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
        List<Vector2Int> cells = GetMovePreviewCells();

        Board.SetHighlightCells(cells, BoardHighlightMode.Move);
        CurrentPreviewMode = BoardPreviewMode.Move;
    }

    private List<Vector2Int> GetMovePreviewCells()
    {
        List<Vector2Int> cells = new();
        Vector2Int origin = CurrentPlayer.CurrentPos;
        int range = CurrentPlayer.MoveRange;

        for (int x = 0; x < Board.BoardWidth; x++)
        {
            for (int y = 0; y < Board.BoardHeight; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);

                if (!IsValidMoveCell(cell))
                {
                    continue;
                }

                cells.Add(cell);
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

        PreviousPlayerCell = CurrentPlayer.CurrentPos;
        LastMovedCell = targetCell;

        HasMovedThisTurn = true;
        CanUndoMove = true;

        Queue.Enqueue(new FaceTargetAction(CurrentPlayer, targetCell));
        Queue.Enqueue(new MoveAction(Board, CurrentPlayer, targetCell));
        ChangeState(BattleState.PlayerTakingActions);

        return true;
    }

    private bool IsValidMoveCell(Vector2Int target)
    {
        if(target.x < 0 || target.y < 0 || target.x >= Board.BoardWidth || target.y >= Board.BoardHeight)
        {
            return false;
        }

        if (target == CurrentPlayer.CurrentPos) return false;

        if (!Board.IsInside(target)) return false;

        if (Board.IsOccupied(target)) return false;

        int distance = Mathf.Abs(CurrentPlayer.CurrentPos.x - target.x)
                     + Mathf.Abs(CurrentPlayer.CurrentPos.y - target.y);

        return distance <= CurrentPlayer.MoveRange;
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