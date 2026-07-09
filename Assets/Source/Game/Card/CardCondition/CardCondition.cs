using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CardCondition : ScriptableObject
{
    public abstract bool IsSatisfied(CardPlayContext context);
}
