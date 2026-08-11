using TMPro;
using UnityEngine;

public sealed class ExperimentModeInputView : TutorialMaskView
{
    [SerializeField] private TMP_InputField ModeInput;
    [SerializeField] private TMP_Text ValidationText;

    private string originalValidationText;

    public override void Show()
    {
        base.Show();

        if (ModeInput == null)
        {
            Debug.LogError("ExperimentModeInputView requires a TMP_InputField.");
            return;
        }

        originalValidationText = ValidationText != null
            ? ValidationText.text
            : string.Empty;
        ModeInput.characterLimit = 1;
        ModeInput.text = string.Empty;
        ModeInput.ActivateInputField();
    }

    public override bool TryContinue()
    {
        if (ModeInput == null || GameManager.Instance == null)
        {
            Debug.LogError("Cannot confirm experiment mode because required references are missing.");
            return false;
        }

        if (!GameManager.Instance.TrySetExperimentModeCode(
                ModeInput.text,
                out string normalizedCode))
        {
            if (ValidationText != null)
            {
                ValidationText.text = "Please enter A, B, C, or D before continuing.";
            }

            ModeInput.text = string.Empty;
            ModeInput.ActivateInputField();
            return false;
        }

        ModeInput.text = normalizedCode;
        return true;
    }

    public override void Hide()
    {
        if (ValidationText != null && !string.IsNullOrEmpty(originalValidationText))
        {
            ValidationText.text = originalValidationText;
        }

        base.Hide();
    }
}
