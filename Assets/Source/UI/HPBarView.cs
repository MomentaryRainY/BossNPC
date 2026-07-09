using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HPBarView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI HealthText;
    [SerializeField] private Image StaminaFillImage;
    [SerializeField] private Image StaminaBufferImage;

    private Vector3 worldOffset = new Vector3(0, 100f, 0);
    private Unit Target;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void LateUpdate()
    {
        if (Target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(Target.transform.position);

        bool isBehindCamera = screenPos.z < 0f;
        canvasGroup.alpha = isBehindCamera ? 0f : 1f;

        if (isBehindCamera)
        {
            return;
        }

        rectTransform.position = screenPos + worldOffset;
    }

    public void Bind(Unit unit)
    {
        Target = unit;
    }

    public void SetHealth(float current, float max)
    {
        HealthText.text = $"{current}/{max}";
        float percentage = current / max;
        StaminaFillImage.fillAmount = Mathf.Clamp01(percentage);
        StartCoroutine(BufferDamage(percentage));
    }

    private IEnumerator BufferDamage(float percentage)
    {
        yield return new WaitForSeconds(0.3f);

        StaminaBufferImage.fillAmount = Mathf.Clamp01(percentage);
    }
}
