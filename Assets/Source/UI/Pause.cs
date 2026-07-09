using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Pause : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI LanguageText;
    [SerializeField] private TextMeshProUGUI OneHitText;

    [SerializeField] private Toggle LanguageToggle;
    [SerializeField] private Toggle OneHitToggle;
    [SerializeField] private Slider AudioSlider;

    [SerializeField] private Button ExitBtn;

    public static bool OneHitEnabled { get; private set; }

    private void Awake()
    {
        AudioSlider.onValueChanged.AddListener(OnAudioChanged);
        LanguageToggle.onValueChanged.AddListener(OnLanguageToggleChanged);
        OneHitToggle.onValueChanged.AddListener(OnOneHitToggleChanged);
        ExitBtn.onClick.AddListener(OnExitClicked);
    }

    private void Start()
    {
        float value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioSlider.SetValueWithoutNotify(value);

        bool isZh = LocalizationManager.Instance.CurrentLanguage == Language.Zh;
        LanguageToggle.SetIsOnWithoutNotify(isZh);
        RefreshLanguageToggleText(isZh);

        OneHitEnabled = PlayerPrefs.GetInt("OneHitEnabled", 0) == 1;
        OneHitToggle.SetIsOnWithoutNotify(OneHitEnabled);
        RefreshOneHitToggleText(OneHitEnabled);
    }

    private void OnAudioChanged(float value)
    {
        AudioManager.Instance.SetMasterVolume(value);
    }

    private void OnLanguageToggleChanged(bool isOn)
    {
        LocalizationManager.Instance.SetLanguage(isOn ? Language.Zh : Language.En);
        RefreshLanguageToggleText(isOn);
    }

    private void OnOneHitToggleChanged(bool isOn)
    {
        OneHitEnabled = isOn;
        RefreshOneHitToggleText(isOn);
    }

    private void RefreshLanguageToggleText(bool isZh)
    {
        LanguageText.text = isZh ? "ZH" : "EN";
    }

    private void RefreshOneHitToggleText(bool isOn)
    {
        OneHitText.text = isOn ? "ON" : "OFF";
    }

    private void OnDestroy()
    {
        AudioSlider.onValueChanged.RemoveListener(OnAudioChanged);
        LanguageToggle.onValueChanged.RemoveListener(OnLanguageToggleChanged);
        OneHitToggle.onValueChanged.RemoveListener(OnOneHitToggleChanged);
        ExitBtn.onClick.RemoveListener(OnExitClicked);
    }

    private void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
