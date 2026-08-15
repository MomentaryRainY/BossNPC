using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDialogueDirector : MonoBehaviour
{
    [SerializeField] private DialogueController bossDialogue;
    [SerializeField] private float dialogueCooldown = 3f;

    private BossDialogueCondition dialogueCondition;
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
    private IBossDialogueOutput dialogueOutput;

    private void Awake()
    {
        if (bossDialogue == null)
        {
            bossDialogue = GetComponent<DialogueController>();
        }
    }

    public void Configure(GameRunMode runMode, BossDialogueCondition condition)
    {
        ResetEncounterState();

        if (runMode == GameRunMode.Scripted)
        {
            ScriptedBossDialogue scriptedDialogue =
                GetComponent<ScriptedBossDialogue>();

            if (scriptedDialogue == null)
            {
                scriptedDialogue = gameObject.AddComponent<ScriptedBossDialogue>();
            }

            dialogueOutput = new ScriptedDialogueOutput(scriptedDialogue);
            return;
        }

        if (runMode == GameRunMode.Experiment)
        {
            dialogueCondition = condition;
        }

        dialogueOutput = ApplyRetrievalStrategy(runMode)
            ? new GeneratedDialogueOutput(bossDialogue)
            : null;
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

        if (dialogueOutput == null || !dialogueOutput.IsAvailable)
        {
            yield break;
        }

        lastDialogueTime = Time.time;
        yield return dialogueOutput.SpeakAndWait(intent, workingMemories);
    }

    private bool CanSpeak()
    {
        return Time.time - lastDialogueTime >= dialogueCooldown;
    }

    private bool Speak(DialogueTriggerType trigger, string intent, bool ignoreCooldown = false)
    {
        if (dialogueOutput == null || !dialogueOutput.IsAvailable)
        {
            Debug.LogWarning("BossDialogueDirector has no configured dialogue output.");
            return false;
        }

        if (!ignoreCooldown && !CanSpeak()) return false;

        lastDialogueTime = Time.time;
        dialogueOutput.Speak(intent, workingMemories);
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

    private bool ApplyRetrievalStrategy(GameRunMode runMode)
    {
        if (MemorySystem.Instance == null)
        {
            Debug.LogError("Cannot configure retrieval because MemorySystem is missing.");
            return false;
        }

        if (runMode == GameRunMode.FullMemory)
        {
            MemorySystem.Instance.SetRetrievalStrategy(
                RetrievalStrategy.FullMemory);
            return true;
        }

        RetrievalStrategy retrievalStrategy;
        switch (dialogueCondition)
        {
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

    private interface IBossDialogueOutput
    {
        bool IsAvailable { get; }

        void Speak(string intent, IReadOnlyList<string> workingMemory);

        IEnumerator SpeakAndWait(
            string intent,
            IReadOnlyList<string> workingMemory);
    }

    private sealed class GeneratedDialogueOutput : IBossDialogueOutput
    {
        private readonly DialogueController controller;

        public bool IsAvailable => controller != null;

        public GeneratedDialogueOutput(DialogueController controller)
        {
            this.controller = controller;
        }

        public void Speak(string intent, IReadOnlyList<string> workingMemory)
        {
            controller.SpeakFromMemory(intent, workingMemory);
        }

        public IEnumerator SpeakAndWait(
            string intent,
            IReadOnlyList<string> workingMemory)
        {
            yield return controller.SpeakFromMemoryAndWait(intent, workingMemory);
        }
    }

    private sealed class ScriptedDialogueOutput : IBossDialogueOutput
    {
        private readonly ScriptedBossDialogue scriptedDialogue;

        public bool IsAvailable => scriptedDialogue != null;

        public ScriptedDialogueOutput(ScriptedBossDialogue scriptedDialogue)
        {
            this.scriptedDialogue = scriptedDialogue;
        }

        public void Speak(string intent, IReadOnlyList<string> workingMemory)
        {
            scriptedDialogue.Speak(intent);
        }

        public IEnumerator SpeakAndWait(
            string intent,
            IReadOnlyList<string> workingMemory)
        {
            yield return scriptedDialogue.SpeakAndWait(intent);
        }
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
