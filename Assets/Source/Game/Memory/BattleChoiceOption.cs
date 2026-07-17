using UnityEngine;

[CreateAssetMenu(menuName = "Choice/Choice Config")]
public class BattleChoiceOption : ScriptableObject
{
    public string ChoiceTextKey;
    public string MemoryTextKey;
    public string EventType;
    public string RelatedCharacter;
    public string RelationToBoss;
    public int Importance;
}
