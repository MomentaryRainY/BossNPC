using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalUIManager : MonoBehaviour
{
    public static GlobalUIManager Instance;

    [SerializeField] private CanvasGroup PausePanel;

    [SerializeField] private ChoiceView ChoicePanel;

    private bool paused;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        HidePause();
        HideChoicePanel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        paused = !paused;

        if (paused)
        {
            ShowPause();
        }
        else
        {
            HidePause();
        }
    }

    public void ShowChoicePanel()
    {
        ChoicePanel.SetShown();
    }

    public void HideChoicePanel()
    {
        ChoicePanel.SetHidden();
    }

    public void SetChoicesText(string key1, string key2, string key3, string key4)
    {
        ChoicePanel.SetChoices(key1, key2, key3, key4);
    }

    private void ShowPause()
    {
        PausePanel.alpha = 1f;
        PausePanel.interactable = true;
        PausePanel.blocksRaycasts = true;
        Time.timeScale = 0f;
    }

    private void HidePause()
    {
        PausePanel.alpha = 0f;
        PausePanel.interactable = false;
        PausePanel.blocksRaycasts = false;
        Time.timeScale = 1f;
    }
}
