# Chapter production

## 1. What the generator gives you

`SceneGenerator` produces, for every chapter, a scene named in `chapters.json` (`sceneName`) with: `PlayerStart`, `CheckpointAnchor`
and `CheckpointTrigger` objects for each declared checkpoint, one trigger per objective (in order, each requiring the ones before it),
`FragmentPickup` where an objective is `collect-fragment-N`, enemies for combat-style chapters, an `ObjectiveTimer` for timed chapter
types, an `AwarenessMeter` and watcher where a Stealth rule exists, a Namirash `HazardZone` where a Corruption rule exists, a
`KillVolume` below the level, and a `ChapterExitTrigger`. Chapter 17 gets the impulse choice and chapter 26 gets the protected, light-freed parents.

This greybox is the contract: **objective ids, checkpoint ids and scene names must stay as they are**. You replace everything else.

## 2. Per chapter, step by step

1. **Spec.** Read the chapter's `spec` in `data/chapters.json` and the matching section of the bible. Note the tag. Write `Docs/chapters/chNN.md` with: tag, beats (copied from the spec, not invented), level plan, objectives, mechanics, enemies, failure rules, unlocks.
2. **Beat sheet.** Convert the beats to a scene list: cutscene, playable segment, cutscene. Mark every beat's source (`[V]` verified text or `[G]` design).
3. **Greybox.** Start from the generated scene. Rearrange the blockout so the path matches the level plan. Keep the triggers working.
4. **Systems.** Wire the mechanics the chapter needs (table below). Make sure each failure rule has a fair checkpoint placed before the hazard, and that no point of the level is a dead end.
5. **Art.** Replace blockout with the region kit and props from licensed assets. Keep triggers invisible. Bake or runtime-bake the NavMesh.
6. **Audio and cinematics.** Music state, ambience, effects; Timeline cutscenes for the beats the spec calls cutscenes; skip and restore must work.
7. **Narrative and localisation.** Dialogue sequences with speaker ids, tone tags and keys under `locPrefix`; subtitles work at 2x size; text fits in the longest language.
8. **QA and balance.** Test Story, Standard and Challenge. Check hint offers after repeated failures. Check frame time on the lowest spec.
9. **Lock.** Only if `Verified`. Otherwise leave at `qa-balance` and list it in the episode-check worklist.

Record each stage with `progress.py set <n> <stage>` after its evidence exists.

## 3. Components by chapter type

| Chapter type (in data) | Use | Chapters |
| --- | --- | --- |
| Hub | `ObjectiveTrigger` per errand, NPC dialogue via `DialogueTrigger`, shop and craft stations built on `AlchemyInventory`, rest chapters have no failure rules | 1, 14, 19, 27, 29, 39, 49, 43, 46 |
| Training, Combat, Defence, Duel | `EnemyBrain` with `EnemyDefinition`, `ObjectiveOnEnemiesDefeated`, parry windows from `DifficultyModifiers` | 2, 6, 16, 41, 44 |
| SetPiece, Escape, Rescue, Trek | `ObjectiveTimer`, `HazardZone` (quicksand, magma, wind, heat), `SafePointRecorder` and `KillVolume` | 4, 5, 22, 25, 20, 33, 48, 50 |
| Chase | Path-following vehicle or run segment, gates as `ObjectiveTrigger`, hits through `HealthComponent`; failure type Chase | 7, 18, 23, 38 |
| Puzzle, Lore | `DecodePuzzle`, `ShadowRevealable`, `ShadowGap`, timed alignment puzzles | 3, 9, 10, 24, 28, 31, 36, 40, 45, 47 |
| Stealth | `AwarenessMeter` and `AwarenessSensor`; failure type Stealth | 8, 11 |
| Dungeon | Web or maze traversal with hazards and an enemy boss; alchemy can be disabled with `AlchemyInventory.SetCraftingEnabled(false)` | 13 |
| Boss | Phases as separate `EnemyDefinition`s or health thresholds; a telegraph is always readable; non-lethal where the bible says so | 15, 17, 21, 26, 32, 35, 42, 51, 52 |
| Survival | A heat or sinking meter with shade or ledge safety; `HazardZone` | 12 |
| Challenge, Minigames | Rule-bending sections; mini-game failures never cost hearts | 30, 34 |

## 4. Chapters with special requirements (from the bible)

| Chapter | Requirement | How |
| --- | --- | --- |
| 5 | Parallel play: Farhat below, Tilda/Wings/Ben above drilling; Shadow locked; dissolve meter | Two playable threads switched by a trigger; `ShadowController.SetState(Captured)` on start and `Available` on rescue; `ObjectiveTimer` on the rescue objective |
| 7 | Balloon chase; shadow contest | A vehicle controller built from `PlayerController` patterns plus path gates; the contest is a short mini-game controlling the Shadow |
| 8 | Cover-up: she must not notice anything supernatural | `AwarenessMeter` raised by `PlayerController.NoiseLevel`, visible magic and combat in sight |
| 11 | Disguise, identity loss, memory echoes | Suspicion meter (reuse awareness); an identity meter that distorts the HUD and offers only villainous options (a feeling, not a branch); memory-echo collectibles that finish the chapter |
| 13 | Alchemy disabled, spot the disguised cousin | `AlchemyInventory.SetCraftingEnabled(false)` until the objective `restore-alchemy` |
| 14 | Antidote with two stations and a timer | Two interactables (Tilda analysis, Shulena herbs) feeding a crafting timer; failing resets the crafting only |
| 17 | Impulse trap | `ImpulseChoiceTrigger` (keep going or turn back). The impulsive option calls `FailForwardManager.ApplyPenalty(ImpulseTrap)`: Shadow disabled for the area and a material lost. The story continues identically |
| 20 | Emotional decision with Ben | Tone choice only; the timed cousin rescue uses `ObjectiveTimer` |
| 22 | Draining health meter for Wings | A timer-like meter on the trek; reaching zero is an Objective failure with a soft reset |
| 23 | Power suppression zone | A zone that disables `LightMeter` abilities and the Shadow while inside |
| 25 | Rising magma | `HazardZone` (Magma) with a rising collider |
| 26 | Non-lethal parents, freed with light | `ProtectedAlly` (any strike resets the section) and `ObjectiveOnLightUsed` (Flash near them completes the objective) |
| 27 to 52 | Namirash zones (reversed controls) | `HazardZone` of kind Namirash raises corruption; add a controls-reversal effect in `PlayerController` guarded by a zone flag; the anti-reversal item (chapter 31) cancels it |
| 33, 35, 38, 52 | Black-hole closing with a Shariman grain | A timing mini-game at a core object; consumes one `resource.grain` |
| 48 | The Shadow evolves | `ShadowController.SetState(Evolved)` at the end |

## 5. Quality bar for "done"

- Plays from Chapter Select and from a cold start.
- Every failure returns to a fair checkpoint; the fail simulator (all `FailureType`s at every checkpoint) shows no stuck state.
- No hard fail and no dead end; the smoke-test run reaches the exit.
- Frame time within budget on the minimum spec; memory within the per-chapter budget.
- Localisation keys complete; subtitles legible at 2x.
- Verified tag, or explicitly not locked.
