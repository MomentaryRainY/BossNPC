using System.Collections.Generic;
using System.Text;

public class ModelAssistedImportanceRetriever : IMemoryRetriever
{
    public const string ScoringRubric =
        "Score memory importance from 0 to 2 using these game-specific references:\n" +
        "0 = trivial or not useful for judging the player's behaviour; " +
        "1 = supporting evidence that may affect a response; " +
        "2 = decisive evidence of strategy, risk, or narrative intent.\n" +
        "Turn count: 1-3 turns indicates an efficient victory; 4-6 is typical; " +
        "7 or more indicates a prolonged encounter.\n" +
        "Remaining health: 75-100% indicates a strong finish; 50-74% a stable finish; " +
        "25-49% a wounded finish; below 25% a critical finish.\n" +
        "Empty hand turns: because cards are constrained by range and stamina, exhausting " +
        "the available hand is evidence of deliberate resource use and should not be " +
        "described as foolish. Repeated empty-hand turns indicate consistently using all " +
        "available options, but do not call the overall strategy successful unless damage " +
        "or encounter outcome also supports that conclusion.\n" +
        "Highest damage in one turn: at least 25% of combined enemy maximum health is a " +
        "high-impact turn; 10-24% is meaningful; below 10% is limited impact.\n" +
        "Narrative choices may receive importance 2 when they directly concern mercy, " +
        "loyalty, betrayal, justice, or Rowan's forces.\n" +
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
        prompt.AppendLine();
        prompt.AppendLine(
            "Return JSON only: {\"importance\":0,\"reason\":\"brief evidence-based reason\"}");
        return prompt.ToString();
    }
}
