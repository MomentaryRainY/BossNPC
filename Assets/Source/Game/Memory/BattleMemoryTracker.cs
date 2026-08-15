using System.Collections.Generic;
using UnityEngine;

public sealed class BattleMemoryTracker
{
    private readonly string battleId;
    private readonly float totalEnemyMaxHealth;
    private readonly List<PlayerTurnMemory> completedTurns = new();

    private float currentTurnDamage;
    private int currentTurnIndex;
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
        currentTurnIndex++;
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

    public void CompletePlayerTurn(bool handIsEmpty, float playerHealthPercent)
    {
        if (!currentTurnActive || currentTurnCompleted)
        {
            return;
        }

        float damagePercent = totalEnemyMaxHealth > 0f
            ? currentTurnDamage / totalEnemyMaxHealth
            : 0f;

        completedTurns.Add(new PlayerTurnMemory
        {
            TurnIndex = currentTurnIndex,
            Damage = currentTurnDamage,
            DamagePercent = damagePercent,
            HandExhausted = handIsEmpty,
            PlayerHealthPercent = Mathf.Clamp01(playerHealthPercent)
        });

        currentTurnCompleted = true;
        currentTurnActive = false;
    }

    public List<MemoryEventData> BuildVictoryMemories(
        int turnCount,
        float remainingHealthPercent)
    {
        List<MemoryEventData> memories = new List<MemoryEventData>();

        foreach (PlayerTurnMemory turn in completedTurns)
        {
            memories.Add(MemoryEventFactory.CreateTurnEvent(
                battleId,
                turn.TurnIndex,
                turn.Damage,
                turn.DamagePercent,
                turn.HandExhausted,
                turn.PlayerHealthPercent));
        }

        memories.Add(MemoryEventFactory.CreateEncounterDuration(battleId, turnCount));
        memories.Add(MemoryEventFactory.CreateFinalHealth(battleId, remainingHealthPercent));
        return memories;
    }

    private sealed class PlayerTurnMemory
    {
        public int TurnIndex;
        public float Damage;
        public float DamagePercent;
        public bool HandExhausted;
        public float PlayerHealthPercent;
    }
}
