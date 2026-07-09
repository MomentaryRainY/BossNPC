using System;
using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private Transform TutorialRoot;

    public bool IsPlaying { get; private set; }

    private TutorialMaskView currentPage;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Play(TutorialSequence sequence, Action onComplete = null)
    {
        if (sequence == null || IsPlaying) return;

        if (sequence.PlayOnce && !string.IsNullOrEmpty(sequence.PlayOnceKey))
        {
            if (PlayerPrefs.GetInt(sequence.PlayOnceKey, 0) == 1)
            {
                onComplete?.Invoke();
                return;
            }
        }

        StartCoroutine(PlayRoutine(sequence, onComplete));
    }

    private IEnumerator PlayRoutine(TutorialSequence sequence, Action onComplete)
    {
        IsPlaying = true;

        foreach (TutorialMaskView prefab in sequence.Pages)
        {
            if (prefab == null) continue;

            currentPage = Instantiate(prefab, TutorialRoot);
            currentPage.Show();

            yield return WaitForSpace();

            currentPage.Hide();
            Destroy(currentPage.gameObject);
            currentPage = null;
        }

        if (sequence.PlayOnce && !string.IsNullOrEmpty(sequence.PlayOnceKey))
        {
            PlayerPrefs.SetInt(sequence.PlayOnceKey, 1);
        }

        IsPlaying = false;
        onComplete?.Invoke();
    }

    private IEnumerator WaitForSpace()
    {
        yield return null;

        while (!Input.GetKeyDown(KeyCode.Space))
        {
            yield return null;
        }
    }
}