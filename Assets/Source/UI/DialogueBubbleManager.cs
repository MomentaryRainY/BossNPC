using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DialogueBubbleManager : MonoBehaviour
{
    public static DialogueBubbleManager Instance;

    [SerializeField] DialogueBubbleView BubblePrefab;

    private Dictionary<Unit, DialogueBubbleView> dic = new();
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowBubble(Unit unit, string content, float duration = 3f)
    {
        if (!dic.TryGetValue(unit, out DialogueBubbleView view))
        {
            view = Instantiate(BubblePrefab, transform, false);
            dic.Add(unit, view);
        }

        view.SetText(content);
        view.show();
        UpdatePosition(unit, view);

        StartCoroutine(ShowPagedBubble(unit, view, duration));
    }

    private IEnumerator ShowPagedBubble(Unit unit, DialogueBubbleView view, float pageDuration)
    {
        while (true)
        {
            yield return new WaitForSeconds(pageDuration);

            if (!view.HasNextPage())
            {
                SetHidden(unit);
                yield break;
            }

            view.ShowNextPage();
        }
    }

    private void SetHidden(Unit unit)
    {
        if (dic.TryGetValue(unit, out DialogueBubbleView view))
        {
            view.hide();
        }
    }

    public void GenerateInstance(Unit unit)
    {
        DialogueBubbleView view = Instantiate(BubblePrefab, this.transform, false);

        dic.Add(unit, view);
    }

    private void UpdatePosition(Unit unit, DialogueBubbleView view)
    {
        Vector2 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);

        Vector2 pos = new Vector2(screenPos.x, screenPos.y) + new Vector2(0f, 180f);

        float halfW = view.rect.sizeDelta.x * 0.5f;
        float halfH = view.rect.sizeDelta.y * 0.5f;

        pos.x = Mathf.Clamp(pos.x, halfW, Screen.width - halfW);
        pos.y = Mathf.Clamp(pos.y, halfH, Screen.height - halfH);

        view.rect.position = pos;
    }

    public void FreeInstances()
    {
        dic.Clear();
    }

    public void LateUpdate()
    {
        foreach (KeyValuePair<Unit, DialogueBubbleView> item in dic) {
            if(item.Key != null)
            {
                UpdatePosition(item.Key, item.Value);
            }
        }
    }

    public void RemoveBubble(Unit unit)
    {
        if (!dic.TryGetValue(unit, out DialogueBubbleView view))
        {
            return;
        }

        Destroy(view.gameObject);
        dic.Remove(unit);
    }
}
