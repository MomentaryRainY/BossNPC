using System;
using System.Collections.Generic;

public sealed class OfflineRetrievalQueryDefinition
{
    public string Id { get; }
    public string TriggerGroup { get; }
    public string QueryText { get; }

    public OfflineRetrievalQueryDefinition(
        string id,
        string triggerGroup,
        string queryText)
    {
        Id = id;
        TriggerGroup = triggerGroup;
        QueryText = queryText;
    }
}

public static class OfflineRetrievalBenchmarkDataset
{
    private static readonly OfflineRetrievalQueryDefinition[] QueryDefinitions =
    {
        new OfflineRetrievalQueryDefinition(
            "Q1",
            "boss_intro",
            "What important actions and choices did the player make before meeting the boss?"),
        new OfflineRetrievalQueryDefinition(
            "Q2",
            "boss_turn_start",
            "What previous actions show how the player fights and behaves?"),
        new OfflineRetrievalQueryDefinition(
            "Q3",
            "player_turn_high_damage_and_hand_empty",
            "What memories relate to the player's strongest attacks and exhausting their hand?"),
        new OfflineRetrievalQueryDefinition(
            "Q4",
            "player_turn_high_damage",
            "What memories describe the player's strongest attacks and combat performance?"),
        new OfflineRetrievalQueryDefinition(
            "Q5",
            "player_turn_hand_empty",
            "What memories describe the player exhausting their hand during combat?"),
        new OfflineRetrievalQueryDefinition(
            "Q6",
            "first_minion_defeated",
            "What previous choices help Rowan judge the player's tactical decisions " +
            "and treatment of Rowan's forces?"),
        new OfflineRetrievalQueryDefinition(
            "Q7",
            "boss_hp_threshold",
            "What previous actions show how dangerous the player is to their enemies?"),
        new OfflineRetrievalQueryDefinition(
            "Q8",
            "player_hp_threshold",
            "What memories describe the player struggling or finishing a battle wounded?"),
        new OfflineRetrievalQueryDefinition(
            "Q9",
            "battle_end",
            "What important choices and battle performance should be remembered at the end of this fight?")
    };

    public static IReadOnlyList<OfflineRetrievalQueryDefinition> Queries =>
        QueryDefinitions;

    public static List<MemoryEventData> CreateMemories()
    {
        return new List<MemoryEventData>
        {
            MemoryEventFactory.CreateTurnEvent(
                "Battle1", 1, 70f, 0.70f, true, 1.00f),
            MemoryEventFactory.CreateTurnEvent(
                "Battle1", 2, 30f, 0.30f, false, 0.90f),
            MemoryEventFactory.CreateEncounterDuration("Battle1", 2),
            MemoryEventFactory.CreateFinalHealth("Battle1", 0.90f),
            CreateChoice(
                "Battle1",
                2,
                "Execute him",
                NarrativeConsequence.Irreversible),

            MemoryEventFactory.CreateTurnEvent(
                "Battle2", 1, 45f, 0.64f, true, 1.00f),
            MemoryEventFactory.CreateTurnEvent(
                "Battle2", 2, 25f, 0.36f, false, 0.86f),
            MemoryEventFactory.CreateEncounterDuration("Battle2", 2),
            MemoryEventFactory.CreateFinalHealth("Battle2", 0.86f),
            CreateChoice(
                "Battle2",
                1,
                "Ignore his situation and keep going.",
                NarrativeConsequence.Indirect),

            MemoryEventFactory.CreateTurnEvent(
                "Battle3", 1, 44f, 0.29f, false, 1.00f),
            MemoryEventFactory.CreateTurnEvent(
                "Battle3", 2, 52f, 0.35f, false, 0.92f),
            MemoryEventFactory.CreateTurnEvent(
                "Battle3", 3, 54f, 0.36f, false, 0.83f),
            MemoryEventFactory.CreateEncounterDuration("Battle3", 3),
            MemoryEventFactory.CreateFinalHealth("Battle3", 0.83f),
            CreateChoice(
                "Battle3",
                3,
                "Execute him",
                NarrativeConsequence.Irreversible)
        };
    }

    private static MemoryEventData CreateChoice(
        string battleId,
        int choiceIndex,
        string choiceText,
        NarrativeConsequence narrativeConsequence)
    {
        return new MemoryEventData
        {
            BattleId = battleId,
            Category = MemoryCategory.NarrativeChoice,
            Text = $"The player selected: {choiceText}.",
            Metrics = new MemoryEventMetrics
            {
                ChoiceIndex = choiceIndex,
                NarrativeConsequence = narrativeConsequence
            }
        };
    }
}
