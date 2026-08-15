using System;
using UnityEngine;

public enum MemoryCategory
{
    NarrativeChoice,
    EncounterDuration,
    FinalHealth,
    TurnEvent
}

[Serializable]
public sealed class MemoryEventMetrics
{
    public int ChoiceIndex = -1;
    public int TurnCount = -1;
    public float RemainingHealthPercent = -1f;
    public int TurnIndex = -1;
    public float TurnDamage = -1f;
    public float TurnDamagePercent = -1f;
    public bool HandExhausted;
    public float PlayerHealthPercentAfterTurn = -1f;

    public MemoryEventMetrics Clone()
    {
        return (MemoryEventMetrics)MemberwiseClone();
    }
}

[Serializable]
public sealed class MemoryEventData
{
    public string BattleId;
    public MemoryCategory Category;
    public string Text;
    public MemoryEventMetrics Metrics = new();
}

public static class MemoryEventFactory
{
    public static MemoryEventData CreateChoice(string battleId, int choiceIndex, BattleChoiceOption option)
    {
        if (option == null)
        {
            return null;
        }

        string resolvedText = ResolveEnglishText(option.ChoiceTextKey);

        return new MemoryEventData
        {
            BattleId = battleId,
            Category = MemoryCategory.NarrativeChoice,
            Text = $"The player selected: {resolvedText}.",
            Metrics = new MemoryEventMetrics
            {
                ChoiceIndex = choiceIndex
            }
        };
    }

    public static MemoryEventData CreateEncounterDuration(string battleId, int turnCount)
    {
        return new MemoryEventData
        {
            BattleId = battleId,
            Category = MemoryCategory.EncounterDuration,
            Text = $"The player completed {battleId} in {turnCount} turns.",
            Metrics = new MemoryEventMetrics
            {
                TurnCount = turnCount
            }
        };
    }

    public static MemoryEventData CreateFinalHealth(
        string battleId,
        float remainingHealthPercent)
    {
        float health = Mathf.Clamp01(remainingHealthPercent);
        int healthPercentage = Mathf.RoundToInt(health * 100f);
        string healthStatus;

        if (health <= 0.25f)
        {
            healthStatus = "critically wounded";
        }
        else if (health <= 0.5f)
        {
            healthStatus = "wounded";
        }
        else
        {
            healthStatus = "in good condition";
        }

        return new MemoryEventData
        {
            BattleId = battleId,
            Category = MemoryCategory.FinalHealth,
            Text = $"The player finished {battleId} {healthStatus} with " +
                $"{healthPercentage}% health.",
            Metrics = new MemoryEventMetrics
            {
                RemainingHealthPercent = health
            }
        };
    }

    public static MemoryEventData CreateTurnEvent(
        string battleId,
        int turnIndex,
        float turnDamage,
        float turnDamagePercent,
        bool handExhausted,
        float playerHealthPercentAfterTurn)
    {
        float damage = Mathf.Max(0f, turnDamage);
        float damagePercent = Mathf.Max(0f, turnDamagePercent);
        float health = Mathf.Clamp01(playerHealthPercentAfterTurn);
        int roundedDamage = Mathf.RoundToInt(damage);
        int roundedDamagePercent = Mathf.RoundToInt(damagePercent * 100f);
        int roundedHealthPercent = Mathf.RoundToInt(health * 100f);

        string handText = handExhausted
            ? "exhausted every card in hand"
            : "did not exhaust the hand";

        return new MemoryEventData
        {
            BattleId = battleId,
            Category = MemoryCategory.TurnEvent,
            Text = $"On player turn {turnIndex} of {battleId}, the player dealt " +
                $"{roundedDamage} damage, equal to {roundedDamagePercent}% of the enemies' " +
                $"combined maximum health, {handText}, and ended the turn with " +
                $"{roundedHealthPercent}% health.",
            Metrics = new MemoryEventMetrics
            {
                TurnIndex = turnIndex,
                TurnDamage = damage,
                TurnDamagePercent = damagePercent,
                HandExhausted = handExhausted,
                PlayerHealthPercentAfterTurn = health
            }
        };
    }

    private static string ResolveEnglishText(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "an unspecified choice";
        }

        return LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetENText(key)
            : key;
    }
}
