using UnityEngine;
using Farhat.Data;

namespace Farhat.Save
{
    public enum ColorBlindMode { Off, Protanopia, Deuteranopia, Tritanopia }

    /// <summary>Player settings and accessibility options (bible section 8). Stored in PlayerPrefs, never in saves.</summary>
    public static class SettingsProfile
    {
        const string KDifficulty = "farhat.difficulty";
        const string KSubtitles = "farhat.subtitles";
        const string KSubtitleScale = "farhat.subtitleScale";
        const string KColorBlind = "farhat.colorBlind";
        const string KCameraShake = "farhat.cameraShake";
        const string KHoldInsteadOfTap = "farhat.holdInsteadOfTap";
        const string KAimAssist = "farhat.aimAssist";
        const string KMasterVolume = "farhat.masterVolume";
        const string KNarrateHints = "farhat.narrateHints";

        public static Difficulty Difficulty
        {
            get { return (Difficulty)PlayerPrefs.GetInt(KDifficulty, (int)Difficulty.Standard); }
            set { PlayerPrefs.SetInt(KDifficulty, (int)value); }
        }

        public static bool SubtitlesEnabled
        {
            get { return PlayerPrefs.GetInt(KSubtitles, 1) == 1; }
            set { PlayerPrefs.SetInt(KSubtitles, value ? 1 : 0); }
        }

        /// <summary>Text size multiplier, 0.75 to 2.0.</summary>
        public static float SubtitleScale
        {
            get { return Mathf.Clamp(PlayerPrefs.GetFloat(KSubtitleScale, 1f), 0.75f, 2f); }
            set { PlayerPrefs.SetFloat(KSubtitleScale, Mathf.Clamp(value, 0.75f, 2f)); }
        }

        public static ColorBlindMode ColorBlind
        {
            get { return (ColorBlindMode)PlayerPrefs.GetInt(KColorBlind, 0); }
            set { PlayerPrefs.SetInt(KColorBlind, (int)value); }
        }

        public static bool CameraShake
        {
            get { return PlayerPrefs.GetInt(KCameraShake, 1) == 1; }
            set { PlayerPrefs.SetInt(KCameraShake, value ? 1 : 0); }
        }

        public static bool HoldInsteadOfTap
        {
            get { return PlayerPrefs.GetInt(KHoldInsteadOfTap, 0) == 1; }
            set { PlayerPrefs.SetInt(KHoldInsteadOfTap, value ? 1 : 0); }
        }

        public static bool AimAssist
        {
            get { return PlayerPrefs.GetInt(KAimAssist, 1) == 1; }
            set { PlayerPrefs.SetInt(KAimAssist, value ? 1 : 0); }
        }

        public static bool NarrateHints
        {
            get { return PlayerPrefs.GetInt(KNarrateHints, 0) == 1; }
            set { PlayerPrefs.SetInt(KNarrateHints, value ? 1 : 0); }
        }

        public static float MasterVolume
        {
            get { return Mathf.Clamp01(PlayerPrefs.GetFloat(KMasterVolume, 0.8f)); }
            set { PlayerPrefs.SetFloat(KMasterVolume, Mathf.Clamp01(value)); }
        }

        public static void Apply() { PlayerPrefs.Save(); }
    }

    /// <summary>Difficulty tuning from bible section 8: Story, Standard, Challenge.</summary>
    public readonly struct DifficultyModifiers
    {
        public readonly float EnemyHealthMultiplier;
        public readonly float DamageTakenMultiplier;
        public readonly float ParryWindowSeconds;
        public readonly bool AutoParry;
        public readonly bool ReducedCheckpointsInSetPieces;

        DifficultyModifiers(float enemyHealth, float damageTaken, float parryWindow, bool autoParry, bool reducedCheckpoints)
        {
            EnemyHealthMultiplier = enemyHealth;
            DamageTakenMultiplier = damageTaken;
            ParryWindowSeconds = parryWindow;
            AutoParry = autoParry;
            ReducedCheckpointsInSetPieces = reducedCheckpoints;
        }

        public static DifficultyModifiers For(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Story: return new DifficultyModifiers(0.6f, 0.6f, 0.35f, true, false);
                case Difficulty.Challenge: return new DifficultyModifiers(1.4f, 1.25f, 0.12f, false, true);
                default: return new DifficultyModifiers(1f, 1f, 0.2f, false, false);
            }
        }
    }
}
