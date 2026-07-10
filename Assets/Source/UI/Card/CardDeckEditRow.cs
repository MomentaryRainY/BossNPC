using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardDeckEditRow : MonoBehaviour
{
    [SerializeField] private Button PlusBtn;
    [SerializeField] private Button MinusBtn;
    [SerializeField] private TMP_Text CountText;
    [SerializeField] private TMP_Text CardName;

    private CardData card;
    private Transition transition;

    public void Init(CardData card, Transition transition)
    {
        this.card = card;
        this.transition = transition;

        PlusBtn.onClick.AddListener(() => transition.OnPlus(card));
        MinusBtn.onClick.AddListener(() => transition.OnMinus(card));

        Refresh();
    }

    public void Refresh()
    {
        if (card == null) return;

        int count = GameManager.Instance.GetDeckCardCount(card);
        CountText.text = count.ToString();
        CardName.text = LocalizationManager.Instance.GetText(card.CardNameKey);
    }

    public void OnEnable()
    {
        LocalizationManager.Instance.LanguageChanged += Refresh;
    }

    public void OnDisable() {
        LocalizationManager.Instance.LanguageChanged -= Refresh;
    }
}
