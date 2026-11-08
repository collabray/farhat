# Agent playbook

Read this once before the first build step, and again whenever you are unsure what is allowed. It is the short version of how to
behave on this project.

## 1. The job in one paragraph

Produce a playable Unity game of 52 chapters that matches the bible, prove it with tests and a build, and keep every unverified
story fact out of the release. You are an engineer on a team: you make the changes, run the checks, and report real results. You
do not decide story, you do not watch episodes, and you do not hide problems.

## 2. Decision table

| Situation | Do this | Never do this |
| --- | --- | --- |
| A chapter's story is `Inferred` | Build and test it from its `spec` text and cost tier. Keep it playable. Add it to the episode-check worklist | Invent plot, set pieces or dialogue from memory of the series; flip the tag |
| The bible and `chapters.json` disagree | `chapters.json` was generated from the bible, so check `references/bible/` first. Report the mismatch to the human, then fix the data and the test together | Silently pick one |
| A needed asset has no listed licence | Skip it and pick another, or ask the human | Download it "for now" |
| Compile error in a template | Fix in the game project, re-compile, then mirror the fix into `templates/` and report it | Disable the file, wrap it in `#if false`, or delete the test that exposes it |
| A test fails | Find the cause and fix the code, or fix the test if the test is wrong and say why | Delete, skip or loosen a test to get green |
| A chapter's greybox is boring or confusing | Note it in the report; adjust the level and encounter values; ask for a playtest | Ship it as done |
| The human asks for something the bible forbids (gore, game-over screens, defeating Wings) | Explain the rule and offer the compliant version | Quietly comply |
| A command needs the human's account (Steam, Unity licence, tokens) | Say exactly what is needed and stop that thread of work | Look for or guess credentials |
| You are about to add a feature the bible does not mention | Stop. Ask whether it is wanted | Add it |

## 3. Stage gates for a chapter

Every chapter moves through the same nine stages, in order (`progress.py set <n> <stage>`):

| Stage | Output | You may move on when |
| --- | --- | --- |
| `spec` | Chapter data confirmed in `chapters.json` | Tag is known; objectives, checkpoints and rules validate |
| `beat-sheet` | Scene list and storyboard in `Docs/chapters/chNN.md` | Every beat comes from the spec text; nothing invented |
| `greybox` | Playable end to end with blockout geometry | A tester or the smoke-test run finishes it without help |
| `systems` | Objectives, failure rules, checkpoints, enemies, timers wired | `unity_cli.py test` passes in both modes; every failure type returns to a fair checkpoint |
| `art` | Environment kit, characters, VFX | Within the frame-time budget on the minimum spec; licence ledger verifies |
| `audio-cinematics` | Music, effects, Timeline cutscenes | Cutscenes play, skip and restore correctly |
| `narrative-loc` | Final lines, subtitle keys, text-fit check | No unlocalised strings; subtitles work at 2x size |
| `qa-balance` | Difficulty tuning and performance pass | Story, Standard and Challenge each tested; targets met |
| `lock` | Chapter frozen | The chapter is `Verified`. `progress.py` refuses otherwise |

## 4. Definition of done for any change

- `unity_cli.py compile` reports no errors.
- `unity_cli.py test --mode editmode` and `--mode playmode` pass.
- `unity_cli.py validate` passes (add `--require-scenes` once scenes are generated).
- `python3 scripts/validate_chapters.py` passes if you touched `chapters.json`; the Unity copy matches `data/chapters.json`.
- `license_ledger.py verify` passes if you touched `Assets/ThirdParty`.
- You ran the game (Bootstrap scene, Play) for any gameplay change, or you said you could not and why.
- The report lists what you ran and the real output.

## 5. Safety rules that protect the product

- **No game-over:** do not add a death screen, a lives counter or permanent loss. Failure goes through `FailForwardManager`.
- **Non-lethal allies:** enemies marked `nonLethal` are freed, never killed, and never deal more than half a heart.
- **Parents, chapter 26:** attacking the charmed parents is disabled and resets the section. Keep it that way.
- **Audience:** targets are PEGI 7 and ESRB E10+. No blood, no realistic weapons beyond the sword, no online chat, no gambling mechanics, no pressure monetisation.
- **Accessibility is a release requirement:** subtitles with speaker names, text scale, colour-blind modes, camera-shake toggle, hold-instead-of-tap, remapping, difficulty assists.
- **Privacy:** no accounts, no collection by default. Any analytics or crash reporting is opt-in and free of personal data.

## 6. Editing rules for the Unity project

- Edit code through files, not by pasting into the editor. Run `compile` after every batch of edits.
- Never hand-edit `.meta` files or the `Library` folder.
- Prefer ScriptableObject data and component fields over constants in code. The generated scenes assign fields through `SerializedObject`; keep serialised field names stable (they are referenced by name in `PrefabBuilders` and `SceneGenerator`).
- Keep the dependency rule: Runtime code never references Editor code; Core never references Systems or Story.
- When you add a system that must survive a checkpoint restore, implement `ISnapshotParticipant` and register with `CheckpointStore`, and add a test that restore equals the captured state.
- Keep Update loops allocation-free.

## 7. What "playable" means here

A build is playable when: the Bootstrap scene starts; chapter 1 loads; the player moves, jumps, dodges, attacks and parries; objectives complete in order; checkpoints record; every failure returns to a checkpoint with no dead end; fragments grant their unlocks; the chapter exit completes the chapter, saves and loads the next; chapter 52 completes without an error. Phase 2 of the skill proves exactly this with the generated greybox. Better art and level design replace the greybox but must keep that loop passing the tests.

## 8. Reporting template

```
Phase / chapter: <name>
Ran: <commands>
Result: <real output, errors included>
Changed: <files>
Verified vs Inferred: <counts, anything newly verified>
Open questions for the human: <only real blockers>
Next: <output of progress.py next>
```
