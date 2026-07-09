using UnityEngine;

public class TutorialPlayer : MonoBehaviour
{
    [SerializeField] private TutorialSequence TeachingSequence;

    private void Start()
    {
        TutorialManager.Instance.Play(TeachingSequence);
    }
}