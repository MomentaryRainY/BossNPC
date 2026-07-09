using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TurnBannerView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;

    [SerializeField] private CanvasGroup CanvasG;

    private Coroutine DemonstrationCoroutine;

    public IEnumerator ShowTurnBanner(string text, float duration)
    {
        gameObject.SetActive(true);
        textMeshProUGUI.text = text;

        CanvasG.alpha = 1f;

        yield return new WaitForSeconds(duration);

        CanvasG.alpha = 0f;
        gameObject.SetActive(false);
    }
}
