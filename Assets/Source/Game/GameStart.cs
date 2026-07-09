using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameStart : MonoBehaviour
{
    [SerializeField] private Button StartBtn;
    [SerializeField] private Button ExitBtn;
    [SerializeField] private Button SettingBtn;
    [SerializeField] private TextMeshProUGUI StartText;
    [SerializeField] private TextMeshProUGUI ExitText;
    [SerializeField] private TextMeshProUGUI SettingText;
    [SerializeField] private TextMeshProUGUI TitleText;
    [SerializeField] private CanvasGroup SettingPanel;

    private void Awake()
    {
        StartBtn.onClick.AddListener(OnStartClicked);
        ExitBtn.onClick.AddListener(OnExitClicked);
        SettingBtn.onClick.AddListener(OnSettingClicked);
    }

    private void OnStartClicked()
    {
        GameManager.Instance.StartNewRun();
    }

    private void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnSettingClicked()
    {
        GlobalUIManager.Instance.TogglePause();
    }

    private void OnDestroy()
    {
        StartBtn.onClick.RemoveListener(OnStartClicked);
        ExitBtn.onClick.RemoveListener(OnExitClicked);
        SettingBtn.onClick.RemoveListener(OnSettingClicked);
    }
}
