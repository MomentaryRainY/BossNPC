using TMPro;
using UnityEngine;

public sealed class PerformanceSummaryView : MonoBehaviour
{
    [SerializeField] private TMP_Text SummaryText;
    [SerializeField] private bool CopyOnShow = true;

    private string summary;

    private void Start()
    {
        if (SummaryText == null)
        {
            SummaryText = GetComponent<TMP_Text>();
        }

        summary = DialoguePerformanceLogger.BuildCurrentSessionSummary();

        if (SummaryText != null)
        {
            SummaryText.text = summary;
        }

        if (CopyOnShow)
        {
            CopySummary();
        }
    }

    public void CopySummary()
    {
        if (string.IsNullOrEmpty(summary))
        {
            summary = DialoguePerformanceLogger.BuildCurrentSessionSummary();
        }

        GUIUtility.systemCopyBuffer = summary;
        Debug.Log("Dialogue performance summary copied to the clipboard.");
    }
}
