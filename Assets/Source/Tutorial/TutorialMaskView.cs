using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialMaskView : MonoBehaviour
{
    [SerializeField] private RectTransform Root;
    [SerializeField] private CanvasGroup RootCanvas;

    [SerializeField] private RectTransform HighlightMarker;

    [SerializeField] private RectTransform ShadowTop;
    [SerializeField] private RectTransform ShadowBottom;
    [SerializeField] private RectTransform ShadowLeft;
    [SerializeField] private RectTransform ShadowRight;

    [SerializeField] private RectTransform FullShadow;

    [SerializeField] private float Padding = 12f;

    private int lastScreenWidth;
    private int lastScreenHeight;
    private Vector2 lastRootSize;
    private bool refreshPending;

    private void OnEnable()
    {
        CacheCurrentSize();
        RequestRefresh();
    }

    private void LateUpdate()
    {
        if (Screen.width != lastScreenWidth ||
            Screen.height != lastScreenHeight ||
            Root.rect.size != lastRootSize)
        {
            CacheCurrentSize();
            RequestRefresh();
        }

        if (refreshPending)
        {
            refreshPending = false;

            Canvas.ForceUpdateCanvases();
            RefreshMask();
        }
    }

    private void CacheCurrentSize()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (Root != null)
        {
            lastRootSize = Root.rect.size;
        }
    }

    private void RequestRefresh()
    {
        refreshPending = true;
    }


    public void RefreshMask()
    {
        bool hasHighlight = HighlightMarker != null && HighlightMarker.gameObject.activeInHierarchy;

        if (hasHighlight)
        {
            ShowCutoutShadow(HighlightMarker);
        }
        else
        {
            ShowFullShadow();
        }
    }

    private void ShowFullShadow()
    {
        SetActiveIfExists(FullShadow, true);

        SetActiveIfExists(ShadowTop, false);
        SetActiveIfExists(ShadowBottom, false);
        SetActiveIfExists(ShadowLeft, false);
        SetActiveIfExists(ShadowRight, false);
    }

    private void SetActiveIfExists(RectTransform rect, bool active)
    {
        if (rect == null) return;
        rect.gameObject.SetActive(active);
    }

    private void ShowCutoutShadow(RectTransform target)
    {
        if (ShadowTop == null || ShadowBottom == null ||
            ShadowLeft == null || ShadowRight == null)
        {
            Debug.LogError($"{name} has HighlightMarker, but T/B/L/R shadows are not assigned.");
            return;
        }

        SetActiveIfExists(FullShadow, false);

        SetActiveIfExists(ShadowTop, true);
        SetActiveIfExists(ShadowBottom, true);
        SetActiveIfExists(ShadowLeft, true);
        SetActiveIfExists(ShadowRight, true);

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 min = WorldToRootLocal(corners[0]);
        Vector2 max = WorldToRootLocal(corners[2]);

        min -= Vector2.one * Padding;
        max += Vector2.one * Padding;

        Rect rootRect = Root.rect;

        SetRect(ShadowBottom, rootRect.xMin, rootRect.yMin, rootRect.xMax, min.y);
        SetRect(ShadowTop, rootRect.xMin, max.y, rootRect.xMax, rootRect.yMax);
        SetRect(ShadowLeft, rootRect.xMin, min.y, min.x, max.y);
        SetRect(ShadowRight, max.x, min.y, rootRect.xMax, max.y);
    }

    private Vector2 WorldToRootLocal(Vector3 worldPos)
    {
        return Root.InverseTransformPoint(worldPos);
    }

    private void SetRect(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
    {
        if (rect == null) return;

        Rect rootRect = Root.rect;

        float left = Mathf.Min(xMin, xMax);
        float right = Mathf.Max(xMin, xMax);
        float bottom = Mathf.Min(yMin, yMax);
        float top = Mathf.Max(yMin, yMax);

        left = Mathf.Clamp(left, rootRect.xMin, rootRect.xMax);
        right = Mathf.Clamp(right, rootRect.xMin, rootRect.xMax);
        bottom = Mathf.Clamp(bottom, rootRect.yMin, rootRect.yMax);
        top = Mathf.Clamp(top, rootRect.yMin, rootRect.yMax);

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;

        rect.anchoredPosition = new Vector2(
            left - rootRect.xMin,
            bottom - rootRect.yMin
        );

        rect.sizeDelta = new Vector2(
            Mathf.Max(0f, right - left),
            Mathf.Max(0f, top - bottom)
        );
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);

        if (RootCanvas != null)
        {
            RootCanvas.alpha = 1f;
            RootCanvas.interactable = true;
            RootCanvas.blocksRaycasts = true;
        }

        CacheCurrentSize();
        RequestRefresh();
    }

    public virtual void Hide()
    {
        if (RootCanvas != null)
        {
            RootCanvas.alpha = 0f;
            RootCanvas.interactable = false;
            RootCanvas.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }
}
