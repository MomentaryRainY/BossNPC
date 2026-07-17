using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceView : MonoBehaviour
{
    [SerializeField] private CanvasGroup choicesPanel;
    [SerializeField] private TextMeshProUGUI choice1;
    [SerializeField] private TextMeshProUGUI choice2;
    [SerializeField] private TextMeshProUGUI choice3;
    [SerializeField] private Button BTN1;
    [SerializeField] private Button BTN2;
    [SerializeField] private Button BTN3;

    private string key1, key2, key3;
    public void OnEnable()
    {
        BTN1.onClick.AddListener(OnChoice1Clicked);
        BTN2.onClick.AddListener(OnChoice2Clicked);
        BTN3.onClick.AddListener(OnChoice3Clicked);
        
    }

    private void Start()
    {
        LocalizationManager.Instance.LanguageChanged += SetTexts;
    }

    public void SetShown()
    {
        choicesPanel.alpha = 1.0f;
        choicesPanel.interactable = true;
        choicesPanel.blocksRaycasts = true;
    }

    public void SetHidden() {
        choicesPanel.alpha = 0.0f;
        choicesPanel.interactable = false;
        choicesPanel.blocksRaycasts = false;
    }

    public void SetChoices(string key1, string key2, string key3)
    {
        this.key1 = key1;
        this.key2 = key2;
        this.key3 = key3;
        SetTexts();
    }

    private void SetTexts()
    {
        choice1.text = LocalizationManager.Instance.GetText(key1);
        choice2.text = LocalizationManager.Instance.GetText(key2);
        choice3.text = LocalizationManager.Instance.GetText(key3);
    }

    private void OnChoice1Clicked()
    {
        EventsHandler.TriggerEvent(UIEvents.MADE_CHOICE, 1);
        SetHidden();
    }

    private void OnChoice2Clicked()
    {
        EventsHandler.TriggerEvent(UIEvents.MADE_CHOICE, 2);
        SetHidden();
    }

    private void OnChoice3Clicked()
    {
        EventsHandler.TriggerEvent(UIEvents.MADE_CHOICE, 3);
        SetHidden();
    }

    public void OnDisable()
    {
        BTN1.onClick.RemoveListener(OnChoice1Clicked);
        BTN2.onClick.RemoveListener(OnChoice2Clicked);
        BTN3.onClick.RemoveListener(OnChoice3Clicked);
    }

    private void OnDestroy()
    {
        LocalizationManager.Instance.LanguageChanged -= SetTexts;
    }
}
