using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial/Tutorial Sequence")]
public class TutorialSequence : ScriptableObject
{
    public string PlayOnceKey;
    public bool PlayOnce = true;

    public List<TutorialMaskView> Pages;
}