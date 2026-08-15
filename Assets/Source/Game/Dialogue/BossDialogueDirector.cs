using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDialogueDirector : MonoBehaviour
{
    [SerializeField] private DialogueController bossDialogue;
    [SerializeField] private float dialogueCooldown = 3f;
    [SerializeField] private BossDialogueCondition dialogueCondition =
        BossDialogueCondition.SimilarityOnly;

    private bool introPlayed;
    private bool bossHp75Played;
    private bool bossHp25Played;
    private bool playerHp50Played;
    private bool playerHp25Played;
    private float lastDialogueTime = -999f;
    private int nextBossTurnToSpeak = 1;
    private int nextBossTurnInterval = 1;
    private readonly List<string> workingMemories = new List<string>();
    private bool firstTacticalMinionDefeatRecorded;

    private void Awake()
    {
        if (bossDialogue == null)
        {
            bossDialogue = GetComponent<DialogueController>();
        }
    }

    public void Configure(BossDialogueCondition condition)
    {
        dialogueCondition = condition;
        ResetEncounterState();
        ApplyRetrievalStrategy();
    }

    public void OnBossEncounterStart(Unit boss, RuntimeBattleState state)
    {
        if (introPlayed) return;

        introPlayed = Speak(DialogueTriggerType.BossEncounterStart, "boss_intro");
    }

    public void OnBossTurnStart(Unit boss, Unit player, RuntimeBattleState state)
    {
        if (!CanSpeak()) return;
        if (state.CurrentTurn < nextBossTurnToSpeak)
        {
            return;
        }

        if (Speak(DialogueTriggerType.BossTurnStart, "boss_turn_start"))
        {
            nextBossTurnToSpeak = state.CurrentTurn + nextBossTurnInterval;
            nextBossTurnInterval = nextBossTurnInterval == 1 ? 2 : 1;
        }
    }

    public void OnPlayerTurnEnd(float damageToBoss, Unit boss, bool playedAllCards)
    {
        if (boss == null || boss.MaxHP <= 0f) return;

        bool dealtQuarterHealth = damageToBoss >= boss.MaxHP * 0.25f;
        if (!dealtQuarterHealth && !playedAllCards) return;

        string intent;
        if (dealtQuarterHealth && playedAllCards)
        {
            intent = "player_turn_high_damage_and_hand_empty";
        }
        else
        {
            intent = dealtQuarterHealth
                ? "player_turn_high_damage"
                : "player_turn_hand_empty";
        }

        Speak(DialogueTriggerType.PlayerTurnSummary, intent);
    }

    public void CheckBossHpThreshold(Unit boss)
    {
        if (boss == null || boss.State == UnitState.Dead) return;

        if (!bossHp25Played && boss.HPPercent <= 0.25f)
        {
            if (Speak(DialogueTriggerType.BossHpBelow25, "boss_hp_below_25"))
            {
                bossHp25Played = true;
                bossHp75Played = true;
            }
            return;
        }

        if (!bossHp75Played && boss.HPPercent <= 0.75f)
        {
            bossHp75Played = Speak(
                DialogueTriggerType.BossHpBelow75,
                "boss_hp_below_75");
        }
    }

    public void CheckPlayerHpThreshold(Unit player)
    {
        if (player == null || player.State == UnitState.Dead) return;

        if (!playerHp25Played && player.HPPercent <= 0.25f)
        {
            if (Speak(DialogueTriggerType.PlayerHpBelow25, "player_hp_below_25"))
            {
                playerHp25Played = true;
                playerHp50Played = true;
            }
            return;
        }

        if (!playerHp50Played && player.HPPercent <= 0.5f)
        {
            playerHp50Played = Speak(
                DialogueTriggerType.PlayerHpBelow50,
                "player_hp_below_50");
        }
    }

    public void OnBossMinionDefeated(EnemyType defeatedRole)
    {
        if (firstTacticalMinionDefeatRecorded)
        {
            return;
        }

        string intent;
        string memory;

        switch (defeatedRole)
        {
            case EnemyType.RANGED_MINION:
                intent = "devil_first";
                memory = "During the current boss fight, the player defeated the ranged " +
                    "Devil before the melee Monster. Removing the ranged threat first was " +
                    "a sound tactical priority because it limited attacks from a distance.";
                break;

            case EnemyType.MELEE_MINION:
                intent = "monster_first";
                memory = "During the current boss fight, the player defeated the melee " +
                    "Monster before the ranged Devil. This left the ranged threat active " +
                    "and was a less efficient tactical priority.";
                break;

            default:
                return;
        }

        firstTacticalMinionDefeatRecorded = true;
        workingMemories.Add(memory);
        Speak(DialogueTriggerType.FirstBossMinionDefeated, intent, ignoreCooldown: true);
    }

    public IEnumerator PlayBattleEndDialogue(BattleManager.BattleResult result)
    {
        string intent = result == BattleManager.BattleResult.Victory
            ? "boss_defeat"
            : "player_defeat";

        if (bossDialogue == null)
        {
            yield break;
        }

        if (ApplyRetrievalStrategy())
        {
            lastDialogueTime = Time.time;
            yield return bossDialogue.SpeakFromMemoryAndWait(intent, workingMemories);
        }
    }

    private bool CanSpeak()
    {
        return Time.time - lastDialogueTime >= dialogueCooldown;
    }

    private bool Speak(DialogueTriggerType trigger, string intent, bool ignoreCooldown = false)
    {
        if (bossDialogue == null)
        {
            Debug.LogWarning("BossDialogueDirector has no DialogueController.");
            return false;
        }

        if (!ignoreCooldown && !CanSpeak()) return false;

        if (!ApplyRetrievalStrategy())
        {
            return false;
        }

        lastDialogueTime = Time.time;
        bossDialogue.SpeakFromMemory(intent, workingMemories);
        return true;
    }

    private void ResetEncounterState()
    {
        introPlayed = false;
        bossHp75Played = false;
        bossHp25Played = false;
        playerHp50Played = false;
        playerHp25Played = false;
        lastDialogueTime = -999f;
        nextBossTurnToSpeak = 1;
        nextBossTurnInterval = 1;
        firstTacticalMinionDefeatRecorded = false;
        workingMemories.Clear();
    }

    private bool ApplyRetrievalStrategy()
    {
        if (MemorySystem.Instance == null)
        {
            Debug.LogError("Cannot configure retrieval because MemorySystem is missing.");
            return false;
        }

        RetrievalStrategy retrievalStrategy;
        switch (dialogueCondition)
        {
            case BossDialogueCondition.FullMemory:
                retrievalStrategy = RetrievalStrategy.FullMemory;
                break;

            case BossDialogueCondition.SimilarityOnly:
                retrievalStrategy = RetrievalStrategy.SimilarityOnly;
                break;

            case BossDialogueCondition.RuleBasedImportance:
                retrievalStrategy = RetrievalStrategy.RuleBasedImportance;
                break;

            case BossDialogueCondition.ModelAssistedImportance:
                retrievalStrategy = RetrievalStrategy.ModelAssistedImportance;
                break;

            default:
                return false;
        }

        MemorySystem.Instance.SetRetrievalStrategy(retrievalStrategy);
        return true;
    }

}

public enum DialogueTriggerType
{
    BossEncounterStart,
    BossTurnStart,
    PlayerTurnSummary,
    BossHpBelow75,
    BossHpBelow25,
    PlayerHpBelow50,
    PlayerHpBelow25,
    FirstBossMinionDefeated,
    BattleEnd
}
