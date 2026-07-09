using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardRenderView : MonoBehaviour
{
    [SerializeField] private TMP_Text cardName;
    [SerializeField] private TMP_Text cardCost;
    [SerializeField] private TMP_Text description;
    [SerializeField] private Transform ContentRoot;
    public Renderer CardRenderer => CRenderer;

    [SerializeField] private Renderer CRenderer;

    private CardInstance UsedCardInstance;

    private GameObject spawnedContent;

    public void Bind(CardInstance card)
    {
        UsedCardInstance = card;
        RefreshText();

        if (spawnedContent != null)
        {
            Destroy(spawnedContent);
        }

        if (card.Data.ContentPrefab != null)
        {
            spawnedContent = Instantiate(card.Data.ContentPrefab, ContentRoot);
        }
    }

    private void OnEnable()
    {
        LocalizationManager.Instance.LanguageChanged += RefreshText;
    }

    private void OnDisable()
    {
        LocalizationManager.Instance.LanguageChanged -= RefreshText;
    }

    private void RefreshText()
    {
        // debug
        if (UsedCardInstance == null)
        {
            Debug.LogWarning("UsedCardInstance is null");
            return;
        }

        if (UsedCardInstance.Data == null)
        {
            Debug.LogWarning("UsedCardInstance.Data is null");
            return;
        }

        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("LocalizationManager.Instance is null");
            return;
        }

        if (cardName == null || description == null || cardCost == null)
        {
            Debug.LogWarning("TMP references are not assigned on CardRenderView");
            return;
        }

        cardName.text = CardTextBuilder.GetName(UsedCardInstance.Data);
        description.text = CardTextBuilder.GetDescription(UsedCardInstance.Data);
        cardCost.text = UsedCardInstance.CurrentCost.ToString();
    }

    public void RefreshPreviewText(CardDescriptionPreview preview)
    {
        cardName.text = CardTextBuilder.GetName(UsedCardInstance.Data);
        description.text = CardTextBuilder.GetDescription(UsedCardInstance.Data, preview);
        cardCost.text = UsedCardInstance.CurrentCost.ToString();
    }

    public void ClearPreviewText()
    {
        RefreshText();
    }
}
