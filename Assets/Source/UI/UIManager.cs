using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UIManager : MonoBehaviour
{
    [SerializeField] private HandView CurrentHandView;

    [SerializeField] private TurnBannerView CurrentTurnBannerView;

    [SerializeField] private HPBarView HPBarPrefab;

    [SerializeField] private RectTransform BarRoot;

    [SerializeField] private Image StaminaImage;

    [SerializeField] private TextMeshProUGUI textMeshProUGUI;

    private CardRenderer CurrentCardRenderer;

    public void Init(CardRenderer cardRenderer)
    {
        CurrentCardRenderer = cardRenderer;
        CurrentHandView.Init(CurrentCardRenderer);
    }


    public HPBarView CreateHPBar(Unit unit)
    {
        HPBarView view = Instantiate(HPBarPrefab, BarRoot, false);
        view.Bind(unit);
        return view;
    }

    public void UpdateUnitHealth()
    {

    }

    public IEnumerator ShowTurnBanner(string text, float duration)
    {
        yield return CurrentTurnBannerView.ShowTurnBanner(text, duration);
    }

    public void SetCardDescriptionPreview(CardDescriptionPreview preview)
    {
        CurrentCardRenderer.SetDescriptionPreview(preview);
    }

    public void ClearCardDescriptionPreview(CardInstance card)
    {
        CurrentCardRenderer.ClearDescriptionPreview(card);
    }

    private void OnDrawCard(List<CardInstance> instances)
    {
        //draw card
        CurrentHandView.DrawCards(instances);
    }

    private void OnPlayCard(CardInstance instances) {
        CurrentHandView.RemoveCard(instances);
    }

    public void SetStaminaProgress(StaminaChangedData data)
    {
        if (StaminaImage == null)
        {
            Debug.LogWarning("Stamina fill image is not assigned.");
            return;
        }

        textMeshProUGUI.SetText($"{data.Current}/{data.Max}");

        StaminaImage.fillAmount = Mathf.Clamp01(data.Ratio);
    }

    void OnEnable()
    {
        EventsHandler.RegisterEvent<List<CardInstance>>(UIEvents.DRAW_CARD, OnDrawCard);
        EventsHandler.RegisterEvent<CardInstance>(CardEvents.PLAY_CARD, OnPlayCard);
        EventsHandler.RegisterEvent<StaminaChangedData>(UIEvents.STAMINA_CHANGE, SetStaminaProgress);
    }

    void OnDisable()
    {
        EventsHandler.UnregisterEvent<List<CardInstance>>(UIEvents.DRAW_CARD, OnDrawCard);
        EventsHandler.UnregisterEvent<CardInstance>(CardEvents.PLAY_CARD, OnPlayCard);
        EventsHandler.UnregisterEvent<StaminaChangedData>(UIEvents.STAMINA_CHANGE, SetStaminaProgress);
    }
}

public class StaminaChangedData
{
    public int Current;
    public int Max;
    public float Ratio => Max <= 0 ? 0f : Current / (float)Max;
}