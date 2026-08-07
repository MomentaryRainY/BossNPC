using System.Collections.Generic;
using UnityEngine;

public sealed class BattleMemoryTracker
{
    private readonly string battleId;
    private readonly float totalEnemyMaxHealth;

    private float currentTurnDamage;
    private float highestTurnDamage;
    private int emptyHandTurnCount;
    private bool currentTurnActive;
    private bool currentTurnCompleted;

    public BattleMemoryTracker(RuntimeBattleState state)
    {
        battleId = state.BattleId;

        foreach (UnitRuntime enemy in state.Enemies)
        {
            if (enemy?.Config != null)
            {
                totalEnemyMaxHealth += Mathf.Max(0f, enemy.Config.MaxHealth);
            }
        }
    }

    public void StartPlayerTurn()
    {
        currentTurnDamage = 0f;
        currentTurnActive = true;
        currentTurnCompleted = false;
    }

    public void RecordPlayerAction(BattleActionResult result)
    {
        if (!currentTurnActive || currentTurnCompleted || result == null || !result.IsPlayerAction)
        {
            return;
        }

        currentTurnDamage += Mathf.Max(0f, result.TotalDamageDealt);
    }

    public void CompletePlayerTurn(bool handIsEmpty)
    {
        if (!currentTurnActive || currentTurnCompleted)
        {
            return;
        }

        highestTurnDamage = Mathf.Max(highestTurnDamage, currentTurnDamage);

        if (handIsEmpty)
        {
            emptyHandTurnCount++;
        }

        currentTurnCompleted = true;
    }

    public List<MemoryEventData> BuildVictoryMemories(
        int turnCount,
        float remainingHealthPercent)
    {
        float highestTurnDamagePercent = totalEnemyMaxHealth > 0f
            ? highestTurnDamage / totalEnemyMaxHealth
            : 0f;

        return new List<MemoryEventData>
        {
            MemoryEventFactory.CreateEncounterOutcome(
                battleId,
                turnCount,
                remainingHealthPercent),
            MemoryEventFactory.CreateCombatPattern(
                battleId,
                emptyHandTurnCount,
                highestTurnDamage,
                highestTurnDamagePercent)
        };
    }
}
