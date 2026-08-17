using TMPro;
using UnityEngine;

public class DialogueBubbleView : MonoBehaviour
{
    [SerializeField] private RectTransform bubbleRect;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private CanvasGroup alpha;
    [Tooltip("Left, top, right, and bottom safe-space inside the bubble.")]
    [SerializeField] private Vector4 padding = new(45f, 60f, 45f, 90f);
    [SerializeField] private float maxWidth = 380f;
    [SerializeField] private float maxHeight = 280f;
    [SerializeField] private float minWidth = 260f;
    [SerializeField] private float minHeight = 180f;

    public RectTransform rect => bubbleRect;
    public CanvasGroup Alpha => alpha;

    private int currentPage = 1;
    private int pageCount = 1;

    public void SetText(string content)
    {
        PrewarmCharacters(content);

        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Page;

        float horizontalPadding = padding.x + padding.z;
        float verticalPadding = padding.y + padding.w;
        float textMaxWidth = Mathf.Max(1f, maxWidth - horizontalPadding);

        Vector2 preferred = text.GetPreferredValues(content, textMaxWidth, 0f);

        float bubbleWidth = Mathf.Clamp(preferred.x + horizontalPadding, minWidth, maxWidth);
        float bubbleHeight = Mathf.Clamp(preferred.y + verticalPadding, minHeight, maxHeight);

        bubbleRect.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);

        text.rectTransform.sizeDelta = new Vector2(
            Mathf.Max(1f, bubbleWidth - horizontalPadding),
            Mathf.Max(1f, bubbleHeight - verticalPadding)
        );
        text.rectTransform.anchoredPosition = new Vector2(
            (padding.x - padding.z) * 0.5f,
            (padding.w - padding.y) * 0.5f
        );

        text.text = content;
        text.pageToDisplay = 1;
        text.ForceMeshUpdate();

        pageCount = text.textInfo.pageCount;
        currentPage = 1;
    }

    private void PrewarmCharacters(string content)
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.PrewarmChineseCharacters(content);
            return;
        }

        TMP_FontAsset fontAsset = text.font;
        if (fontAsset == null || string.IsNullOrEmpty(content))
        {
            return;
        }

        fontAsset.isMultiAtlasTexturesEnabled = true;

        if (!fontAsset.TryAddCharacters(content, out string missingCharacters)
            && !string.IsNullOrEmpty(missingCharacters))
        {
            Debug.LogWarning(
                $"Dialogue font '{fontAsset.name}' could not provide these characters: " +
                missingCharacters);
        }
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
