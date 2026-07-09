using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Card", menuName = "Card/Card Data")]
public class CardData : ScriptableObject
{
    public string CardId;

    public string CardNameKey; //i18n

    public int BaseCost;

    public string DescriptionKey;

    public GameObject ContentPrefab; // card show view

    public CardTargetingRule TargetingRule;

    public List<CardEffect> Effects;

    public List<CardCondition> Conditions;
}

public class CardRenderInfo
{
    public Rect UVRect;

    public int AtlasIndex;
}