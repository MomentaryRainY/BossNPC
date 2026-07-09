using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardCardPresenter : MonoBehaviour
{
    [SerializeField] private CardRenderer CardRenderer;
    [SerializeField] private RawImage RewardCardImage;

    private void Start()
    {
        CardData reward = GameManager.Instance.CurrentTransitionContext.RewardCard;
        
        if(reward == null)
        {
            return;
        }

        CardInstance instance = new CardInstance
        {
            Data = reward,
            CurrentCost = reward.BaseCost
        };

        List<CardInstance> cards = new() { instance };

        CardRenderer.Init(cards);

        RewardCardImage.texture = CardRenderer.CardsRenderTexture;
        RewardCardImage.uvRect = CardRenderer.CardInstanceViewDictionary[instance].UVRect;
    }
}
