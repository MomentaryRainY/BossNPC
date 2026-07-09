using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleInputController : MonoBehaviour
{
    private BattleManager CurrentBattleManager;

    public void Init(BattleManager battleManager)
    {
        CurrentBattleManager = battleManager;   
    }

    private bool IsInputBlockedByTutorial()
    {
        return TutorialManager.Instance != null && TutorialManager.Instance.IsPlaying;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetMouseButtonDown(1))
        {
            if (!IsInputBlockedByTutorial())
            {
                CurrentBattleManager.TryUndoMove();
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (!IsInputBlockedByTutorial())
            {
                TryClickBoardCell();
            }
        }
    }

    public void OnLanguageChange()
    {
        if (LocalizationManager.Instance.CurrentLanguage == Language.Zh)
        {
            LocalizationManager.Instance.SetLanguage(Language.En);
        } else
        {
            LocalizationManager.Instance.SetLanguage(Language.Zh);
        }
    }

    private void TryClickBoardCell()
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsPlaying)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, 1 << 8))
        {
            //Debug.Log($"Clicked: {hit.collider.name}, layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            Board board = hit.collider.GetComponentInParent<Board>();

            if (board == null)
            {
                return;
            }

            Vector2Int cell = board.WorldToGrid(hit.point);
            CurrentBattleManager.TryMovePlayer(cell);
        }
    }

    void OnTurnEnd()
    {
        CurrentBattleManager.TryEndTurn();
    }

    void OnCardPlayRequested(PlayCardRequest request)
    {
        CurrentBattleManager.TryPlayCard(request.Card, request.Target);
    }

    void OnCardPreviewRequested(CardInstance card)
    {
        CurrentBattleManager.ShowCardPreview(card);
    }

    void OnCardPreviewClear()
    {
        CurrentBattleManager.ClearCardPreview();
    }

    void OnCardTargetPreview(CardTargetPreviewRequest request)
    {
        CurrentBattleManager.PreviewCardOnTarget(request.Card, request.Target);
    }

    void OnEnable()
    {
        EventsHandler.RegisterEvent(TurnEvents.END_TURN, OnTurnEnd);
        EventsHandler.RegisterEvent<PlayCardRequest>(CardEvents.PLAY_CARD_REQUEST, OnCardPlayRequested);
        EventsHandler.RegisterEvent<CardInstance>(CardEvents.SHOW_CARD_RANGE, OnCardPreviewRequested);
        EventsHandler.RegisterEvent(CardEvents.CLEAR_CARD_RANGE, OnCardPreviewClear);
        EventsHandler.RegisterEvent<CardTargetPreviewRequest>(CardEvents.PREVIEW_CARD_TARGET, OnCardTargetPreview);
    }

     void OnDisable()
    {
        EventsHandler.UnregisterEvent(TurnEvents.END_TURN, OnTurnEnd);
        EventsHandler.UnregisterEvent<PlayCardRequest>(CardEvents.PLAY_CARD_REQUEST, OnCardPlayRequested);
        EventsHandler.UnregisterEvent<CardInstance>(CardEvents.SHOW_CARD_RANGE, OnCardPreviewRequested);
        EventsHandler.UnregisterEvent(CardEvents.CLEAR_CARD_RANGE, OnCardPreviewClear);
        EventsHandler.UnregisterEvent<CardTargetPreviewRequest>(CardEvents.PREVIEW_CARD_TARGET, OnCardTargetPreview);
    }
}


public class PlayCardRequest
{
    public CardInstance Card { get; private set; }
    public Unit Target { get; private set; }
    public PlayCardRequest(CardInstance card, Unit unit)
    {
        Card = card;
        Target = unit;
    }
}