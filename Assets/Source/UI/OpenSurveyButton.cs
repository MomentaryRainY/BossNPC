using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class OpenSurveyButton : MonoBehaviour
{
    private Button surveyButton;

    private void Awake()
    {
        surveyButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        surveyButton.onClick.AddListener(OpenSurvey);
    }

    private void OnDisable()
    {
        surveyButton.onClick.RemoveListener(OpenSurvey);
    }

    private void OpenSurvey()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("Cannot open survey because GameManager is missing.");
            return;
        }

        GameManager.Instance.OpenExperimentSurvey();
    }
}
