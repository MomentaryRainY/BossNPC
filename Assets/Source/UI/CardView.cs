using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CardInstance UsedCardInstance { get; private set; }

    private HandView UsedHandView;

    [SerializeField] private RawImage RawImage;

    private RectTransform rectTransform;
    private Vector2 baseAnchoredPosition;
    private bool isHovering;
    private bool isDragging;

    private readonly Vector2 hoverOffset = new Vector2(0f, 170f);

    public void Init(CardInstance instance, CardRenderer cardRenderer, HandView handView)
    {
        UsedCardInstance = instance;
        UsedHandView = handView;
        RawImage.texture = cardRenderer.CardsRenderTexture;
        RawImage.uvRect = cardRenderer.CardInstanceViewDictionary[UsedCardInstance].UVRect;
        rectTransform = GetComponent<RectTransform>();
        isDragging = false;
        isHovering = false;
    }
    public void SetBaseAnchoredPosition(Vector2 position)
    {
        baseAnchoredPosition = position;

        if (!isDragging)
        {
            rectTransform.anchoredPosition = isHovering
                ? baseAnchoredPosition + hoverOffset
                : baseAnchoredPosition;
        }
    }

    public void OnBeginDrag(PointerEventData pointerEventData)
    {
        RawImage.raycastTarget = false;
        isDragging = true;

        UsedHandView.BeginCardDrag(this, pointerEventData);
    }

    public void OnDrag(PointerEventData pointerEventData) {
        if (!isDragging) return;
        UsedHandView.UpdateCardDrag(this, pointerEventData);
    }

    public void OnEndDrag(PointerEventData pointerEventData)
    {
        if (!isDragging) return;

        isDragging = false;
        isHovering = false;

        rectTransform.anchoredPosition = baseAnchoredPosition;
        rectTransform.localScale = Vector3.one;
        RawImage.raycastTarget = true;
        UsedHandView.EndCardDrag(this, pointerEventData);
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        if (isDragging || isHovering) return;

        isHovering = true;
        rectTransform.anchoredPosition = baseAnchoredPosition + hoverOffset;
        rectTransform.localScale = Vector3.one * 1.2f;
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        if (isDragging || !isHovering) return;

        isHovering = false;
        rectTransform.anchoredPosition = baseAnchoredPosition;
        rectTransform.localScale = Vector3.one;
    }
}
