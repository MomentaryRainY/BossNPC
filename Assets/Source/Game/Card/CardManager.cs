using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public List<CardInstance> instances { get; private set; }

    private List<CardInstance> HandCards;
    private List<CardInstance> DrawPile;
    private List<CardInstance> DiscardPile;

    private RuntimeBattleState CurrentBattleState;

    private int HandCardsCountLimit;

    void Awake()
    {
        instances = new();
        HandCards = new();
        DrawPile = new();
        DiscardPile = new();
    }

    public void PlayCard(CardInstance card)
    {
        if(!HandCards.Remove(card))
        {
            Debug.LogWarning("Played an inexist hand card");
        }
        
        DiscardPile.Add(card);

        EventsHandler.TriggerEvent(CardEvents.PLAY_CARD, card);
    }

    public void Init(RuntimeBattleState RTBattleState)
    {
        CurrentBattleState = RTBattleState;
        HandCardsCountLimit = RTBattleState.MaxHandCount;
        foreach (CardData cardData in RTBattleState.CurrentCardDeck)
        {
            CardInstance instance = new();
            instance.Data = cardData;
            instance.CurrentCost = cardData.BaseCost;

            instances.Add(instance);
            DrawPile.Add(instance);
        }

    }
    
    public void DrawRandomCards(int N, BattleActionQueue Queue)
    {
        List<CardInstance> DrawnCards = new();

        int count = Mathf.Min(N, HandCardsCountLimit - HandCards.Count);

        if (count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            if(DrawPile.Count == 0)
            {
                ShuffleAndReplenishCards(Queue);
            }

            if (DrawPile.Count == 0)
            {
                break;
            }

            int index = Random.Range(0, DrawPile.Count);
            CardInstance card = DrawPile[index];

            DrawnCards.Add(card);
            // waiting to add animation
            //Queue.Enqueue(new DrawCardAction(card));
            DrawPile.RemoveAt(index);
        }

        if (DrawnCards.Count == 0)
        {
            return;
        }

        HandCards.AddRange(DrawnCards);
        EventsHandler.TriggerEvent(UIEvents.DRAW_CARD, DrawnCards);
    }

    private void ShuffleAndReplenishCards(BattleActionQueue Queue)
    {
        DrawPile.AddRange(DiscardPile);
        DiscardPile.Clear();

        Shuffle(DrawPile);
        // waiting to add animation
        //Queue.Enqueue(new ReplenishCardsAction());
    }

    private void Shuffle(List<CardInstance> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            CardInstance temp = cards[i];
            cards[i] = cards[j];
            cards[j] = temp;
        }
    }
}
