using TMPro;
using UnityEngine;

public sealed class PerformanceSummaryView : MonoBehaviour
{
    [SerializeField] private bool CopyOnShow = true;

    private string summary;

    private void Start()
    {
        summary = DialoguePerformanceLogger.BuildCurrentSessionSummary();

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
