using Farhat.Data;

namespace Farhat.Core
{
    public readonly struct ChapterStarted
    {
        public readonly int Number;
        public ChapterStarted(int number) { Number = number; }
    }

    public readonly struct ChapterGoalsMet
    {
        public readonly int Number;
        public ChapterGoalsMet(int number) { Number = number; }
    }

    public readonly struct ChapterCompleted
    {
        public readonly int Number;
        public ChapterCompleted(int number) { Number = number; }
    }

    public readonly struct ObjectiveCompleted
    {
        public readonly string Id;
        public ObjectiveCompleted(string id) { Id = id; }
    }

    public readonly struct CheckpointReached
    {
        public readonly string Id;
        public CheckpointReached(string id) { Id = id; }
    }

    public readonly struct PlayerFailed
    {
        public readonly FailureType Type;
        public PlayerFailed(FailureType type) { Type = type; }
    }

    public readonly struct PlayerRestored
    {
        public readonly string CheckpointId;
        public readonly int Attempts;
        public PlayerRestored(string checkpointId, int attempts) { CheckpointId = checkpointId; Attempts = attempts; }
    }

    public readonly struct FragmentCollected
    {
        public readonly int Index;
        public FragmentCollected(int index) { Index = index; }
    }

    public readonly struct UnlockGranted
    {
        public readonly string Id;
        public UnlockGranted(string id) { Id = id; }
    }

    public readonly struct DamageDealt
    {
        public readonly float Amount;
        public readonly bool TargetIsPlayer;
        public DamageDealt(float amount, bool targetIsPlayer) { Amount = amount; TargetIsPlayer = targetIsPlayer; }
    }

    public readonly struct EnemyDefeated
    {
        public readonly string EnemyId;
        public EnemyDefeated(string enemyId) { EnemyId = enemyId; }
    }

    public readonly struct ShadowStateChanged
    {
        public readonly ShadowState State;
        public ShadowStateChanged(ShadowState state) { State = state; }
    }

    public readonly struct ShadowAbilityUsed
    {
        public readonly ShadowAbility Ability;
        public ShadowAbilityUsed(ShadowAbility ability) { Ability = ability; }
    }

    public readonly struct AwarenessChanged
    {
        public readonly float Value;
        public AwarenessChanged(float value) { Value = value; }
    }

    public readonly struct PotionUsed
    {
        public readonly PotionFamily Family;
        public PotionUsed(PotionFamily family) { Family = family; }
    }

    public readonly struct DialogueLineShown
    {
        public readonly string SpeakerId;
        public readonly string LocKey;
        public readonly string Text;
        public readonly DialogueTone Tone;
        public DialogueLineShown(string speakerId, string locKey, string text, DialogueTone tone) { SpeakerId = speakerId; LocKey = locKey; Text = text; Tone = tone; }
    }

    public readonly struct LightAbilityUsed
    {
        public readonly LightAbility Ability;
        public readonly UnityEngine.Vector3 Position;
        public LightAbilityUsed(LightAbility ability, UnityEngine.Vector3 position) { Ability = ability; Position = position; }
    }

    public readonly struct HintOffered
    {
        public readonly string ObjectiveId;
        public HintOffered(string objectiveId) { ObjectiveId = objectiveId; }
    }
}
