using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Transition : MonoBehaviour
{
    [SerializeField] private Button ContinueBtn;
    [SerializeField] private Button AgainBtn;
    [SerializeField] private Button ConfirmBtn;

    [SerializeField] private TextMeshProUGUI ContinueText;
    [SerializeField] private TextMeshProUGUI AgainText;

    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TextMeshProUGUI healthText;

    [SerializeField] private CanvasGroup LevelUpPage;
    [SerializeField] private CanvasGroup RewardCardPage;
    [SerializeField] private CanvasGroup CardsPage;

    [SerializeField] private Transform DeckListRoot;
    [SerializeField] private CardDeckEditRow CardDeckEditPrefab;

    private readonly List<CardDeckEditRow> rows = new();

    private readonly List<CanvasGroup> ActivePages = new();

    private int currentIndex;

    private float inputEnableTime = .3f;

    private void Awake()
    {
        ContinueBtn.onClick.AddListener(OnContinueClicked);
        AgainBtn.onClick.AddListener(OnAgainClicked);
        ConfirmBtn.onClick.AddListener(OnConfirmClicked);
    }

    private void Start()
    {
        HidePage(LevelUpPage);
        HidePage(RewardCardPage);
        HidePage(CardsPage);

        TransitionContext context = GameManager.Instance.CurrentTransitionContext;

        if (context.ShowLevelUpPage)
        {
            ActivePages.Add(LevelUpPage);
            RefreshLevelUp();
        }

        if (context.ShowRewardCardPage && context.RewardCard != null)
        {
            ActivePages.Add(RewardCardPage);
        }

        if(context.ShowCardsPage)
        {
            BuildDeckEditor();
            ActivePages.Add(CardsPage);
        }

        if (ActivePages.Count == 0)
        {
            SetChoiceButtonsEnabled(true);
            return;
        }
        inputEnableTime = Time.unscaledTime + .3f;
        currentIndex = 0;
        ShowPage(currentIndex);
    }


    private void Update()
    {
        if (Time.unscaledTime < inputEnableTime) return;

        if (Input.anyKeyDown)
        {
            CanvasGroup currentPage = GetCurrentPage();

            if (currentPage == CardsPage)
            {
                return;
            }

            ShowNextPage();
        }
    }

    private void HidePage(CanvasGroup page)
    {
        if (page == null) return;

        page.alpha = 0f;
        page.blocksRaycasts = false;
        page.interactable = false;
    }

    private CanvasGroup GetCurrentPage()
    {
        if (currentIndex < 0 || currentIndex >= ActivePages.Count)
        {
            return null;
        }

        return ActivePages[currentIndex];
    }

    private void ShowNextPage()
    {
        if (currentIndex >= ActivePages.Count)
        {
            return;
        }

        currentIndex++;
        ShowPage(currentIndex);
    }

    private void ShowPage(int index)
    {
        for (int i = 0; i < ActivePages.Count; i++)
        {
            bool active = i == index;
            CanvasGroup page = ActivePages[i];

            page.alpha = active ? 1f : 0f;
            page.blocksRaycasts = active;
            page.interactable = active;
        }

        SetChoiceButtonsEnabled(index >= ActivePages.Count);
    }

    private void SetChoiceButtonsEnabled(bool enabled)
    {
        ContinueBtn.interactable = enabled;
        AgainBtn.interactable = enabled;

        if (!enabled && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void RefreshLevelUp()
    {
        LevelUpInfo info = GameManager.Instance.CurrentTransitionContext.LevelUp;
        if (info == null) return;
        staminaText.text = $"{LocalizationManager.Instance.GetText("ui.stamina")}: {info.OldStamina} -> {info.NewStamina}";
        healthText.text = $"{LocalizationManager.Instance.GetText("ui.health")}: {info.OldHealth} -> {info.NewHealth}";
    }
    private void OnConfirmClicked()
    {
        if (GetCurrentPage() != CardsPage)
        {
            return;
        }

        GameManager.Instance.ConfirmDeck();
        ShowNextPage();
    }

    private void BuildDeckEditor()
    {
        foreach (Transform child in DeckListRoot)
        {
            Destroy(child.gameObject);
        }

        rows.Clear();

        foreach (CardData card in GameManager.Instance.CurrentRun.OwnedCards)
        {
            CardDeckEditRow row = Instantiate(CardDeckEditPrefab, DeckListRoot);
            row.Init(card, this);
            rows.Add(row);
        }

        RefreshDeckEditor();
    }

    public void OnPlus(CardData card)
    {
        int current = GameManager.Instance.GetDeckCardCount(card);

        if (GameManager.Instance.TrySetDeckCount(card, current + 1))
        {
            RefreshDeckEditor();
        }
    }

    public void OnMinus(CardData card)
    {
        int current = GameManager.Instance.GetDeckCardCount(card);

        if (GameManager.Instance.TrySetDeckCount(card, current - 1))
        {
            RefreshDeckEditor();
        }
    }

    private void RefreshDeckEditor()
    {
        foreach (CardDeckEditRow row in rows)
        {
            row.Refresh();
        }
    }

    private void OnContinueClicked()
    {
        EventsHandler.TriggerEvent(SceneEvents.NEXT_SCENE);
    }

    private void OnAgainClicked()
    {
        //  EventsHandler.TriggerEvent(SceneEvents.TRY_AGAIN);
    }

    private void OnDestroy()
    {
        ContinueBtn.onClick.RemoveListener(OnContinueClicked);
        AgainBtn.onClick.RemoveListener(OnAgainClicked);
        ConfirmBtn.onClick.RemoveListener(OnConfirmClicked);
    }

    private void OnEnable()
    {
        LocalizationManager.Instance.LanguageChanged += RefreshLevelUp;
    }

    private void OnDisable()
    {
        LocalizationManager.Instance.LanguageChanged -= RefreshLevelUp;
    }
}

public class LevelUpInfo
{
    public int OldStamina;
    public int NewStamina;

    public int OldHealth;
    public int NewHealth;
}