using System;
using UnityEngine;

public enum MemoryCategory
{
    NarrativeChoice,
    EncounterOutcome,
    CombatPattern
}

[Serializable]
public sealed class MemoryEventMetrics
{
    public int ChoiceIndex = -1;
    public int TurnCount = -1;
    public float RemainingHealthPercent = -1f;
    public int EmptyHandTurnCount = -1;
    public float HighestTurnDamage = -1f;
    public float HighestTurnDamagePercent = -1f;

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

    public static MemoryEventData CreateEncounterOutcome(
        string battleId,
        int turnCount,
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
            Category = MemoryCategory.EncounterOutcome,
            Text = $"The player completed {battleId} in {turnCount} turns and finished " +
                $"{healthStatus} with {healthPercentage}% health.",
            Metrics = new MemoryEventMetrics
            {
                TurnCount = turnCount,
                RemainingHealthPercent = health
            }
        };
    }

    public static MemoryEventData CreateCombatPattern(string battleId, int emptyHandTurnCount,
        float highestTurnDamage, float highestTurnDamagePercent)
    {
        float damagePercent = Mathf.Max(0f, highestTurnDamagePercent);
        int roundedDamage = Mathf.RoundToInt(Mathf.Max(0f, highestTurnDamage));
        int roundedDamagePercent = Mathf.RoundToInt(damagePercent * 100f);

        string handText = emptyHandTurnCount == 0
            ? "The player did not exhaust their hand during the encounter."
            : $"The player exhausted their hand on {emptyHandTurnCount} turn(s).";

        return new MemoryEventData
        {
            BattleId = battleId,
            Category = MemoryCategory.CombatPattern,
            Text = $"{handText} Their strongest turn dealt {roundedDamage} damage, " +
                $"equal to {roundedDamagePercent}% of the enemies' combined maximum health.",
            Metrics = new MemoryEventMetrics
            {
                EmptyHandTurnCount = emptyHandTurnCount,
                HighestTurnDamage = Mathf.Max(0f, highestTurnDamage),
                HighestTurnDamagePercent = damagePercent
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
