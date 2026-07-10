using System.Collections;
using System.Collections.Generic;
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
        UpdatePosition(unit, view);

        StartCoroutine(HideAfter(unit, duration));
    }

    private IEnumerator HideAfter(Unit unit, float duration)
    {
        yield return new WaitForSeconds(duration);

        SetHidden(unit);
    }

    private void SetHidden(Unit unit)
    {
        dic.TryGetValue(unit, out DialogueBubbleView view);
        view.hide();
    }

    public void GenerateInstance(Unit unit)
    {
        DialogueBubbleView view = Instantiate(BubblePrefab, this.transform, false);

        dic.Add(unit, view);
    }

    private void UpdatePosition(Unit unit, DialogueBubbleView view)
    {
        Vector2 pos = Camera.main.WorldToScreenPoint(unit.transform.position + Vector3.up * 2f);

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
            UpdatePosition(item.Key, item.Value);
        }
    }
}
