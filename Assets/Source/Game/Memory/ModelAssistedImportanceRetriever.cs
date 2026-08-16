using System.Collections.Generic;
using System.Text;

public class ModelAssistedImportanceRetriever : IMemoryRetriever
{
    public const string ScoringRubric =
        "Score memory importance from 0 to 2 using these game-specific references:\n" +
        "0 = trivial or not useful for judging the player's behaviour; " +
        "1 = supporting evidence that may affect a response; " +
        "2 = decisive evidence of strategy, risk, or narrative intent.\n" +
        "Each record is atomic: score only the fact in the supplied record and do not " +
        "infer an unrecorded encounter summary.\n" +
        "Turn count: 1-2 turns indicates an exceptionally fast victory; 3-4 is typical; " +
        "5-6 is slower than expected; 7 or more is exceptionally prolonged.\n" +
        "Remaining health: 90-100% indicates a strong finish worth remembering; " +
        "50-89% is stable, 25-49% is wounded, and below 25% is critical.\n" +
        "Turn events: because cards are constrained by range and stamina, exhausting the " +
        "available hand is evidence of deliberate resource use and should not be described " +
        "as foolish. Do not call the turn successful unless its damage supports that claim.\n" +
        "Turn damage is the total damage across the entire player turn, not damage from " +
        "one card. At least 50% of combined enemy maximum health is exceptional; " +
        "25-49% is meaningful; below 25% is ordinary damage.\n" +
        "Narrative choices may receive importance 2 when they strongly affect Rowan's " +
        "judgement through mercy, loyalty, betrayal, justice, or treatment of Rowan's forces.\n" +
        "Judge importance independently from semantic similarity to the current query.";

    public List<MemoryRecord> Retrieve(List<MemoryRecord> memories, MemoryQuery query, int topK)
    {
        throw new System.NotImplementedException(
            "Model-assisted retrieval still requires an asynchronous scoring client and score fusion.");
    }

    public static string BuildScoringPrompt(MemoryRecord memory)
    {
        StringBuilder prompt = new StringBuilder();
        prompt.AppendLine("You are evaluating one factual gameplay memory for an NPC memory system.");
        prompt.AppendLine(ScoringRubric);
        prompt.AppendLine();
        prompt.AppendLine("[MEMORY]");
        prompt.AppendLine($"Battle: {memory?.BattleId}");
        prompt.AppendLine($"Category: {memory?.Category}");
        prompt.AppendLine($"Text: {memory?.Text}");
        prompt.AppendLine($"Structured metrics: {UnityEngine.JsonUtility.ToJson(memory?.Metrics)}");
        prompt.AppendLine();
        prompt.AppendLine(
            "Return JSON only: {\"importance\":0,\"reason\":\"brief evidence-based reason\"}");
        return prompt.ToString();
    }
}
