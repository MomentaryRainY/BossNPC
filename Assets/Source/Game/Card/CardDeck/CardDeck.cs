using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Card Deck", menuName = "Card/Card Deck")]
public class CardDeck : ScriptableObject
{
    [SerializeField] public List<CardData> Cards;
}