using TMPro;
using UnityEngine;

public class DialogueBubbleView : MonoBehaviour
{
    [SerializeField] private RectTransform bubbleRect;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private CanvasGroup alpha;
    [SerializeField] private Vector2 padding;
    [SerializeField] private float maxWidth = 220f;
    [SerializeField] private float maxHeight = 220f;
    [SerializeField] private float minWidth = 160f;
    [SerializeField] private float minHeight = 80f;

    public RectTransform rect => bubbleRect;
    public CanvasGroup Alpha => alpha;

    private int currentPage = 1;
    private int pageCount = 1;

    public void SetText(string content)
    {
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Page;

        float textMaxWidth = maxWidth - padding.x;
        float textMaxHeight = maxHeight - padding.y;

        Vector2 preferred = text.GetPreferredValues(content, textMaxWidth, 0f);

        float bubbleWidth = Mathf.Clamp(preferred.x + padding.x, minWidth, maxWidth);
        float bubbleHeight = Mathf.Clamp(preferred.y + padding.y, minHeight, maxHeight);

        bubbleRect.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);

        text.rectTransform.sizeDelta = new Vector2(
            bubbleWidth - padding.x,
            bubbleHeight - padding.y
        );

        text.text = content;
        text.pageToDisplay = 1;
        text.ForceMeshUpdate();

        pageCount = text.textInfo.pageCount;
        currentPage = 1;
    }

    public bool HasNextPage()
    {
        return currentPage < pageCount;
    }

    public void ShowNextPage()
    {
        if (!HasNextPage()) return;

        currentPage++;
        text.pageToDisplay = currentPage;
    }

    public void show()
    {
        alpha.alpha = 1;
    }

    public void hide() {
        alpha.alpha = 0;
    }
}