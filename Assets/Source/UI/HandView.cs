using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.GraphicsBuffer;

public class HandView : MonoBehaviour
{
    [SerializeField] private RectTransform CardsRoot;
    [SerializeField] private CardView CardPrefab;

    [SerializeField] private RectTransform DragArrowRoot;
    [SerializeField] private RectTransform ArrowLine;
    [SerializeField] private Canvas RootCanvas;

    [SerializeField] private CanvasGroup HandCanvasGroup;

    [SerializeField] private Camera WorldCamera;

    private Unit CurrentPreviewTarget;

    private float NormalAlpha = 1f;
    private float DraggingAlpha = 0.6f;

    private List<CardView> HandCards;

    private CardRenderer CurrentCardRenderer;
    public void Init(CardRenderer cardRenderer)
    {
        CurrentCardRenderer = cardRenderer;
        HandCards = new();
    }

    private void SetArrow(Vector2 startScreenPos, Vector2 endScreenPos)
    {
        Vector2 start = ScreenToCanvasLocal(startScreenPos);
        Vector2 end = ScreenToCanvasLocal(endScreenPos);

        Vector2 direction = end - start;
        float length = direction.magnitude;

        DragArrowRoot.gameObject.SetActive(true);

        ArrowLine.anchoredPosition = start;
        ArrowLine.sizeDelta = new Vector2(length, ArrowLine.sizeDelta.y);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        ArrowLine.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Vector2 ScreenToCanvasLocal(Vector2 screenPosition)
    {
        RectTransform canvasRect = RootCanvas.transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : RootCanvas.worldCamera,
            out Vector2 localPoint
        );

        return localPoint;
    }

    public void RemoveCard(CardInstance card)
    {
        CardView cardView = HandCards.Find(view => view.UsedCardInstance == card);

        if (cardView == null) return;

        HandCards.Remove(cardView);
        Destroy(cardView.gameObject);

        LayoutHandCards();
    }

    public void DrawCards(List<CardInstance> instances)
    {
        for (int i = 0; i < instances.Count; i++) {
            CardView cardView = Instantiate(CardPrefab, CardsRoot, false);
            cardView.Init(instances[i], CurrentCardRenderer, this);
            HandCards.Add(cardView);
        }
        LayoutHandCards();
    }

    void LayoutHandCards()
    {
        float spacing = 200f;

        float start = -(HandCards.Count - 1) * spacing * 0.5f;

        for (int i = 0; i < HandCards.Count; i++)
        {
            RectTransform rect = HandCards[i].GetComponent<RectTransform>();

            Vector2 basePosition =  new Vector2(start + i * spacing, -150);
            rect.anchoredPosition = basePosition;
            HandCards[i].SetBaseAnchoredPosition(basePosition);
        }
    }

    public void BeginCardDrag(CardView cardView, PointerEventData eventData)
    {
        SetArrow(cardView.transform.position, eventData.position);
        HandCanvasGroup.alpha = DraggingAlpha;
        EventsHandler.TriggerEvent(CardEvents.SHOW_CARD_RANGE, cardView.UsedCardInstance);
    }

    public void UpdateCardDrag(CardView cardView, PointerEventData eventData)
    {
        SetArrow(cardView.transform.position, eventData.position);

        Unit target = GetTargetUnderPointer(eventData.position);

        if (target == CurrentPreviewTarget)
        {
            return;
        }

        CurrentPreviewTarget = target;

        EventsHandler.TriggerEvent(CardEvents.PREVIEW_CARD_TARGET,
            new CardTargetPreviewRequest(cardView.UsedCardInstance, target));
    }

    public void EndCardDrag(CardView cardView, PointerEventData eventData)
    {
        DragArrowRoot.gameObject.SetActive(false);
        HandCanvasGroup.alpha = NormalAlpha;
        CurrentPreviewTarget = null;
        EventsHandler.TriggerEvent(CardEvents.PREVIEW_CARD_TARGET,
            new CardTargetPreviewRequest(cardView.UsedCardInstance, null));
        EventsHandler.TriggerEvent(CardEvents.CLEAR_CARD_RANGE);
        //layer cast
        Unit target = GetTargetUnderPointer(eventData.position);

        if (target != null)
        {
            EventsHandler.TriggerEvent(CardEvents.PLAY_CARD_REQUEST,
                new PlayCardRequest(cardView.UsedCardInstance, target));
        }
    }

    private Unit GetTargetUnderPointer(Vector2 screenPosition)
    {
        Ray ray = WorldCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, 1 << 7)) // 7 = target layer
        {
            Debug.Log($"aimed {hit.collider.name}");
            return hit.collider.GetComponentInParent<Unit>();
        }

        return null;
    }

    void OnEnable()
    {
        
    }

    void OnDisable()
    {
        
    }
}

public class CardTargetPreviewRequest
{
    public CardInstance Card;
    public Unit Target;

    public CardTargetPreviewRequest(CardInstance card, Unit target)
    {
        Card = card;
        Target = target;
    }
}

public class CardDescriptionPreview
{
    public CardInstance Card;
    public int Damage;

    public CardDescriptionPreview(CardInstance card, int damage)
    {
        Card = card;
        Damage = damage;
    }
}