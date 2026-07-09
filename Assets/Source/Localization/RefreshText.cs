using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RefreshText : MonoBehaviour
{
    [SerializeField] private TMP_Text Target;
    [SerializeField] private string Key;

    private bool subscribed;

    private void Awake()
    {
        if (Target == null)
        {
            Target = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        TryBindAndRefresh();
    }

    private void Start()
    {
        TryBindAndRefresh();
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.LanguageChanged -= Refresh;
        }

        subscribed = false;
    }

    private void TryBindAndRefresh()
    {
        if (Target == null)
        {
            Target = GetComponent<TMP_Text>();
        }

        if (Target == null || LocalizationManager.Instance == null)
        {
            return;
        }

        if (!subscribed)
        {
            LocalizationManager.Instance.LanguageChanged += Refresh;
            subscribed = true;
        }

        Refresh();
    }

    public void Refresh()
    {
        if (Target == null || LocalizationManager.Instance == null)
        {
            return;
        }

        Target.text = LocalizationManager.Instance.GetText(Key);
    }
}
