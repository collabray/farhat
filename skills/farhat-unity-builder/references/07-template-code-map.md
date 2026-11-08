# Template code map

Everything below is in `templates/unity/Assets/Game/`. Namespaces start with `Farhat`. Runtime code never references Editor code.

## Bootstrap (own assembly, no dependencies)

`PackageInstaller.cs` installs the packages by name and runs in batch mode before the rest of the game compiles. Keep this assembly free of references to `Farhat.Runtime`.

## Runtime/Core (no game rules)

| File | Purpose | Extend by |
| --- | --- | --- |
| `EventBus.cs` | Typed publish/subscribe bus; `EventBusRegistry.ClearAll` resets every bus on play start and in tests | New event structs in `Events.cs` |
| `Events.cs` | All event structs: chapter, objective, checkpoint, failure, fragments, unlocks, damage, shadow, awareness, potions, dialogue, light, hints | Add a struct, publish it, subscribe in OnEnable and unsubscribe in OnDisable |
| `ServiceLocator.cs` | Services registered once at start-up (`GameBootstrap`, `ChapterRunner`, `PlayerController`, `FailForwardManager`, ...) | Register new shared services in `GameBootstrap.Awake` |
| `StateMachine.cs` | Enum-keyed state machine used by `EnemyBrain` | Use for new brains and boss phases |
| `GameObjectPool.cs` | Pool for effects and projectiles | Use for any frequently spawned object |

## Runtime/Data (assets and records)

| File | Purpose |
| --- | --- |
| `Enums.cs` | `SourceTag`, `CostTier`, `ChapterType`, `FailureType`, `ShadowState`, `ShadowAbility`, `PotionFamily`, `DialogueTone`, `EnemyFamily`, `Difficulty`, `LightAbility` |
| `ChapterDefinition.cs` | One asset per chapter: tag, tier, type, objectives, checkpoints, failure rules, unlocks, scene name |
| `ChapterCatalog.cs` | The 52 definitions, loaded from `Resources/ChapterCatalog` |
| `ChapterJson.cs` | Transfer classes that mirror `chapters.json` |
| `EnemyDefinition.cs`, `PotionRecipe.cs`, `DialogueSequence.cs` | Data for enemies, recipes and dialogue |

## Runtime/Save

`SaveData` (version, chapter, checkpoint, fragment mask, hearts, unlocks, stars), `SaveMigrator` (one step per version bump),
`SaveSystem` (temp file, backup, fallback on corrupt read), `SettingsProfile` (difficulty and accessibility in PlayerPrefs) with
`DifficultyModifiers` (Story, Standard, Challenge).

## Runtime/Story (turns data into play)

| File | Purpose |
| --- | --- |
| `GameBootstrap.cs` | Registers services, loads the save, runs chapters in order, chapter select rule. Test hooks: `SuppressAutoStart`, `SaveDirectoryOverride` |
| `ChapterRunner.cs` | Loads the chapter scene, spawns the player, initialises objectives and checkpoints, completes chapters (unlocks, stars, save) |
| `ObjectiveTracker.cs` | Tracks tier-1/2/3 goals, publishes `ChapterGoalsMet`, snapshot participant |
| `CheckpointStore.cs` | Records and restores snapshots of every registered `ISnapshotParticipant` |
| `FailForwardManager.cs` | The only place that decides what failing means; `ApplyPenalty` for story-gated choices |
| `UnlockManager.cs` | Fragments, abilities, recipes, hearts (`hearts.plus1.chNN`), save bridging |
| `HintService.cs`, `ScreenFader.cs`, `DialogueRunner.cs`, `ChapterScoring.cs` | Hints after repeated failure, fades, dialogue playback, star rating |
| `LevelAnchors.cs` | `PlayerStart`, `CheckpointAnchor` (registries, no scene searches) |
| `LevelTriggers.cs` | `ObjectiveTrigger`, `CheckpointTrigger`, `ChapterExitTrigger`, `FragmentPickup`, `ObjectiveOnEnemiesDefeated`, `ObjectiveOnGapOpened`, `ObjectiveOnLightUsed`, `ObjectiveTimer`, `ImpulseChoiceTrigger`, `DialogueTrigger` |

## Runtime/Systems (reusable mechanics)

`PlayerController`, `ThirdPersonCamera`, `CombatController` (combo, heavy, parry, light abilities), `HealthComponent` (hearts of light, never dies),
`LightMeter` (capacity by fragments, Flash, Light Blade, Shield, `LightAbilityUsed` event), `ShadowController` (+ `ShadowRevealable`, `ShadowGap`),
`AlchemyInventory`, `DecodePuzzle`, `AwarenessMeter` + `AwarenessSensor`, `EnemyBrain`, `ProtectedAlly`, `CompanionFollower`,
`HazardZone` (quicksand, magma, wind, heat, Namirash), `SafePointRecorder` + `KillVolume`, `RuntimeNavMeshBaker`, `PlayerInputActions`, `Interfaces.cs`.

## Runtime/UI

`HudController` (hearts, light, Shadow cooldown), `SubtitleView` (speaker names, text scale), `QuestLogView` (three tiers), `ChapterTitleCard`.

## Editor

| File | Menu or command | Purpose |
| --- | --- | --- |
| `ChapterCatalogImporter.cs` | Farhat > Content > Import Chapters From JSON | Bible-to-data importer; never overwrites `includedInBuild` |
| `SceneGenerator.cs`, `PrefabBuilders.cs` | Farhat > Content > Generate Greybox Scenes | Player prefab, enemies, recipes, Bootstrap, 52 chapter scenes, build settings |
| `ContentValidator.cs` | Farhat > Content > Validate | Data and scene consistency |
| `StoryGate.cs` | Farhat > Content > Story Report | Blocks release builds with unverified chapters |
| `BuildScript.cs` | `unity_cli.py build` | Command-line builds |
| `ProjectSetup.cs` (and Bootstrap/`PackageInstaller.cs`) | Farhat > Setup | Linear colour space, folders, new Input System backend; packages by name |
| `ChapterBoardWindow.cs` | Farhat > Chapter Board | Chapter list with tags and an Open scene button |

## Tests

EditMode: `CoreTests` (bus, services, state machine, saves, unlocks, difficulty, scoring, checkpoints, objectives, decode puzzle) and
`ChapterDataTests` (52 chapters, verified list, fragments, hearts, key unlocks, rules, importer plus validation plus story gate).
PlayMode: `SystemTests` (health, Shadow, alchemy, awareness, timer) and `ChapterSmokeTests` (critical path and fail simulator per chapter).

## Known gaps (build these when their chapters come up)

- Vehicle controllers for the balloon (chapter 7) and the flying transport (Season 2).
- The Namirash controls-reversal effect inside `PlayerController`.
- Black-hole closing and the grain resource.
- Mini-games (ball game in chapter 14, Season 2 mini-game gauntlet).
- Cutscene (Timeline) wiring and the localisation tables.
These are chapter features, not foundation bugs. Add them through the stage gates, with tests.
