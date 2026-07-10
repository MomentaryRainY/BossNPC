using TMPro;
using UnityEngine;

public class DialogueBubbleView : MonoBehaviour
{
    [SerializeField] private RectTransform bubbleRect;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Vector2 padding = new(32f, 20f);
    [SerializeField] private float maxWidth = 420f;
    [SerializeField] private CanvasGroup alpha;

    public RectTransform rect => bubbleRect;
    public CanvasGroup Alpha => alpha;

    public void SetText(string content)
    {
        text.text = content;

        text.enableWordWrapping = true;

        Vector2 preferred = text.GetPreferredValues(content, maxWidth - padding.x, 0f);

        float width = Mathf.Min(preferred.x + padding.x, maxWidth);
        float height = preferred.y + padding.y;

        bubbleRect.sizeDelta = new Vector2(width, height);
    }

    public void show()
    {
        alpha.alpha = 1;
    }

    public void hide() {
        alpha.alpha = 0;
    }
}