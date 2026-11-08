## 6. Chapter design pipeline

### 6.1 Anatomy of a chapter

Every chapter is one Chapter Definition asset plus its scenes. The asset is the single source of truth that connects the bible to the build.

| Field | Meaning |
| --- | --- |
| id and episode title | For example, 05 and In the Great Darkness |
| sourceTag | Verified, Inferred or Game design (mirrors the bible's [V], [I], [G]) |
| costTier | A, B or C (section 3) |
| scenes | Addressable scene references loaded in order |
| objectives | The three-tier goal list |
| checkpoints | Named autosave points, including before bosses |
| failureRules | Which failure types apply and their penalties |
| cinematics | Timeline assets with their trigger conditions |
| unlocks | Fragments, abilities, recipes, cosmetics given on completion |
| partyRules | Active companions, playable segments, scripted absences |
| locKeys | Prefix for all strings in the chapter |

### 6.2 Chapter types and reuse

| Type | Used for | Reusable kit |
| --- | --- | --- |
| Hub | Palace, Oxford, Mayan village, camps | Hub controller, NPC schedule, shop and craft stations |
| Set piece | Chase, balloon, mine, labyrinth | Spline-driven path tools, hazard volumes |
| Puzzle | Decode, lab, antidote | Puzzle base class, hint timer |
| Stealth or cover-up | Miss Keyhole, Rashid disguise | Awareness meter, guard AI |
| Boss | Giant robot, horseman, Egokhan | Phase component, weak-point and telegraph tools |
| Rest | Calm Weekend at Oxford, Sweet Coast | Free-roam hub with side activities |

The planned reuse is the cost lever: a Mexican, English and Emirates hub all use the same Hub controller, a chase in the balloon and a chase in the flying transport share the same path and hazard tools.

### 6.3 Production workflow and stage gates

| Stage | Output | Gate to pass |
| --- | --- | --- |
| 0. Spec | Chapter Definition filled from the bible, with the episode check done | Source tag is Verified for release chapters |
| 1. Beat sheet | Storyboard and scene list | Director and narrative lead sign-off |
| 2. Greybox | Playable end to end with blockout geometry and greybox art | A tester finishes it without help |
| 3. Systems pass | Objectives, failure rules, checkpoints, AI | No soft-locks in the smoke-test bot run |
| 4. Art pass | Environment, characters, VFX | Art review |
| 5. Audio and cinematics | Music, effects, Timeline cutscenes | Audio and cinematic review |
| 6. Narrative and localisation | Final lines, subtitles, string tables | Locale check (text fits, right-to-left works) |
| 7. QA and balance | Difficulty tuning, performance pass | Meets the performance and stability targets |
| 8. Lock | Chapter frozen | Producer approval; no further story changes |

**Definition of Done for any chapter**

- Plays from the chapter-select menu and from a cold start.

- Every failure type has a fair checkpoint and no hard fail.

- All text is localised and subtitled; accessibility options work.

- Meets the frame-time budget on minimum spec.

- Smoke-test bot completes it with no soft-lock.

- Source tag is Verified.

### 6.4 Chapter runtime flow

chapter runtime flow · 7 states, 1 loop

**Reading the diagram.** A chapter loads, plays its objectives and ends in completion. Any failure routes through the fail-forward manager back to a checkpoint, never to a game-over state. Story-gated penalties (for example, the impulse trap) are applied by the manager from the chapter's declared rules.
