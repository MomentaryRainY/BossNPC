using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CardEffect : ScriptableObject
{
    public abstract void BuildActions(CardPlayContext context, BattleActionQueue queue);
}

public class CardPlayContext
{
    public CardInstance Card { get; private set; }
    public Unit Caster { get; private set; }
    public Unit Target { get; private set; }
    public Board Board { get; private set; }
    public RuntimeBattleState BattleState { get; private set; }
    public CardManager CardManager { get; private set; }

    public DamageResolver DMGResolver { get; private set; }

    public CardPlayContext(
        CardInstance card,
        Unit caster,
        Unit target,
        Board board,
        RuntimeBattleState battleState,
        CardManager cardManager,
        DamageResolver dMGResolver)
    {
        Card = card;
        Caster = caster;
        Target = target;
        Board = board;
        BattleState = battleState;
        CardManager = cardManager;
        DMGResolver = dMGResolver;
    }
}