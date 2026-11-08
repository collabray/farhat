---
name: farhat-unity-builder
description: Builds the story-driven 3D action-adventure game "Farhat: The Prince of the Desert" (52 chapters, two seasons) in Unity 6 from the production bible. Use when asked to create, extend, test, fix, package or release the Farhat game, to set up its Unity project and MCP connectors, to generate or replace chapter scenes, to download and license-check 3D assets, textures, HDRIs or audio for it, or to run its episode check, QA, CI or release steps. Covers the full pipeline from an empty folder to a playable greybox of all 52 chapters, then chapter-by-chapter production to release.
---

# Farhat Unity Builder

You are building one specific game from a finished design. The design is in this skill, the code foundation is in this skill, and the
tools to verify your work are in this skill. Your job is to run the pipeline in order, prove each step with a command, and stop at
the human gates. Do not improvise story, do not skip verification.

## What exists, and what does not

| Exists in this skill | Does not exist, and you must not pretend it does |
| --- | --- |
| The full bible and project document (`references/bible/`, `references/project/`) | Episode footage. You cannot watch episodes. Unverified chapters stay `Inferred` until a human confirms them |
| `data/chapters.json`: all 52 chapters with objectives, checkpoints, failure rules, unlocks, spec text and verification tags | Final character art. Characters are modelled from the rights holder's model sheets; use the stand-in rigs until then |
| A compile-checked Unity C# foundation (`templates/unity/`): core, data, save, systems, story flow, UI, editor tools, tests | A Unity licence or store accounts. The human activates Unity and owns Steam or console accounts |
| Scripts that validate data, fetch licensed assets, drive Unity in batch mode, scaffold the project and track progress | Voice, music and final localisation. These need the rights holder's permission and a human |
| A generator that builds a playable greybox scene for every chapter | A guarantee of fun. Playtests with people decide that |

## Step 0: confirm the basics (2 minutes)

1. Run `python3 scripts/check_env.py`. Required: Python 3.9+, Git, Unity 6.3 LTS. If Unity is missing, tell the human to install it through Unity Hub and activate a licence, then stop. Never try to work around a missing editor.
2. Run `python3 scripts/validate_chapters.py` and `python3 -m unittest discover -s tests`. Both must pass before you touch anything. If they fail, the skill is damaged: report it, do not continue.
3. Ask the human once, in one message: (a) where the game project should live, (b) confirmation that they hold the licence for the series (the project owner stated this; ask for it to be confirmed in writing before any release), (c) whether to use the English names (Egokhan, Rashid) or the alternate dub names. Default to English names if they do not answer.

## Where every fact lives

| You need | Read |
| --- | --- |
| Story, characters, names, tone | `references/bible/03-story-bible.md`, `references/bible/04-characters.md` |
| One chapter's beats, level, mechanics, fail states, unlocks | `data/chapters.json` (field `spec`), and `references/bible/10-season-1-chapters-part-one.md` or `references/bible/11-season-2-chapters-part-two.md` |
| Mechanics: combat, light meter, Shadow, alchemy, decode, vehicles | `references/bible/06-core-gameplay-systems-all-g.md` |
| What counts as failing, difficulty, accessibility | `references/bible/07-failure-rules.md`, `references/bible/08-difficulty-accessibility-and-audience-safety.md` |
| Bosses, fragments, unlock timeline, art, audio, interface | `references/bible/12-boss-and-set-piece-list.md`, `13-progression-and-fragment-tracker.md`, `14-art-audio-and-interface-direction.md` |
| Architecture, stack, pipeline, code standards | `references/project/05-system-architecture.md` to `08-content-pipeline-and-tooling.md` |
| Testing, performance budgets, troubleshooting, deployment | `references/project/09-quality-assurance-and-testing.md` to `12-deployment-and-release.md` |
| How to operate (rules, gates, commands) | `references/00-agent-playbook.md` and the files `01` to `08` listed at the end of this file |

Read the playbook (`references/00-agent-playbook.md`) before the first build step. It is short and it contains the rules you are
most likely to break.

## Non-negotiable rules

1. **Story facts come only from the bible and `data/chapters.json`.** If a fact is not there, you do not know it. Never fill a gap from memory of the series. Mark the gap and ask.
2. **Tags are law.** `Verified` chapters can be locked. `Inferred` chapters (31 of 52) can be built, played and tested, but never locked or released until the episode check changes their tag. The Unity story gate enforces this in release builds. Do not weaken it, remove it, or flip tags yourself.
3. **Licensing.** Only CC0, CC BY and (fonts) SIL OFL assets enter the game, always through `scripts/asset_fetch.py` so the ledger records them. Never download models of the licensed characters from fan or marketplace sites.
4. **No game-over.** Failure is a setback that returns to a checkpoint (`FailForwardManager`). Named allies are freed, never defeated. No gore. These are product rules, not style preferences.
5. **Prove, do not claim.** A step is done only when its command output says so. Report real output, including failures.
6. **Secrets.** Tokens go in environment variables or the CI secret store. Never write them into files, logs or commits.
7. **Never fabricate.** No invented data, no unfinished features presented as finished, no invented episode content, no invented asset licences.

## The pipeline

Work in phases. Each phase has a gate. Record progress with `python3 scripts/progress.py` inside the game project so a later session
can resume exactly where you stopped (`progress.py next` tells you what is next).

### Phase 1: scaffold and compile (gate: zero compiler errors, EditMode tests green)

```
python3 scripts/scaffold_project.py --project <GAME_DIR> --with-docs
python3 scripts/unity_cli.py compile --project <GAME_DIR>
python3 scripts/unity_cli.py test --project <GAME_DIR> --mode editmode
```

The scaffolder creates the Unity project, installs the packages first (the game code cannot compile until the Input System, AI Navigation and
the other packages exist, so only the small `Bootstrap` installer is copied at that point), then copies the game code. If it reports that
no editor was found, install Unity and run `python3 scripts/unity_cli.py install-packages --project <GAME_DIR>` before compiling.

The templates were type-checked against API stubs, not against the real Unity API. If `compile` reports errors, fix them in the
project's `Assets/Game` using the real error text, re-run, and repeat until clean. Then fix the same issue in the skill's
`templates/` copy and tell the human which templates needed correction. See `references/02-unity-automation.md`.

### Phase 2: the playable greybox of all 52 chapters (gate: the full game is playable end to end)

```
python3 scripts/unity_cli.py generate-scenes --project <GAME_DIR>
python3 scripts/unity_cli.py validate --project <GAME_DIR> --require-scenes
python3 scripts/unity_cli.py test --project <GAME_DIR> --mode playmode
```

`SceneGenerator` builds a Bootstrap scene and one scene per chapter (path of objective triggers, checkpoints, fragment pickups,
enemies, timers, stealth and corruption hazards from each chapter's failure rules, chapter exit). The Bootstrap scene runs chapters 1
to 52 in order. Open `Bootstrap` and press Play to check it. This is the proof that the whole game structure works. It is greybox:
everything after this phase is replacing greybox with real content, one chapter at a time, without breaking the loop.

### Phase 3: pre-production gate and vertical slice (gate G1 then G2 of the project document)

1. **Prototype gate (G1):** movement, camera, sword combat and the Shadow must feel good for ten minutes. Playtest with a human; tune values in the components, not in code constants.
2. **Episode check:** run `python3 scripts/story_report.py --out Docs/episode-check.md`. Ask the human to watch the listed episodes (Season 2 first, because it has the least verified content) or to paste synopses. Follow `references/08-episode-check.md`.
3. **Vertical slice:** chapters 1, 5 and 7 to near-final quality (see `references/04-chapter-production.md`): real environment kits, character stand-ins with animation, audio, VFX, cutscenes, localisation keys. Then run the slice playtest protocol (`references/project/09-quality-assurance-and-testing.md`). The slice decides go or no-go (G2): stop and report.

### Phase 4 and 5: Part One, then Part Two (gates G3 and G4)

Per chapter, follow the nine stage gates in `references/04-chapter-production.md`. Work in episode drops (chapters 1 to 9, 10 to 18, 19 to 26, then Season 2 in the same rhythm).
After every chapter: `unity_cli.py compile`, `unity_cli.py test` (both modes), `unity_cli.py validate`, `progress.py set <n> <stage>`.
A chapter reaches `lock` only if it is `Verified`; `progress.py` refuses otherwise.

### Phase 6: release (gate: story gate passes, checklist complete)

```
python3 scripts/license_ledger.py verify --ledger <GAME_DIR>/Assets/ThirdParty/ledger.json --base <GAME_DIR>
python3 scripts/license_ledger.py credits --ledger <GAME_DIR>/Assets/ThirdParty/ledger.json --out <GAME_DIR>/CREDITS.md
python3 scripts/unity_cli.py build --project <GAME_DIR> --target win64 --release
```

A release build fails while any included chapter is not `Verified`. That is correct behaviour. Follow `references/05-qa-ci-release.md` for the checklist, Steam setup and rollback.

## Using the connectors

- **Unity MCP** (editor control: create scenes, objects, components, run tests, read the console). Connection and tool use: `references/01-setup-and-connectors.md`. Use it for scene and prefab work. Use `unity_cli.py` for anything that must be repeatable or that CI also runs.
- **Blender MCP** (modelling, import, Poly Haven and Sketchfab inside Blender). Use it to adapt downloaded assets, retarget or convert files. Final licensed-character models are never fetched, they are authored.
- **Asset scripts** (`scripts/asset_fetch.py`): the default way to download. The catalogue of what the game needs is `data/asset-needs.json`.

If a connector is not installed or not running, say so and fall back to batch mode (`unity_cli.py`). Do not fake connector output.

## When to stop and ask the human

Stop and ask, in one short message, at these points and no others unless blocked: Unity or its licence is missing; the project location is unknown; an episode check is needed; a licence is unclear for an asset you want; a design decision changes a fact in the bible; the slice is ready for the go or no-go review; a release or store action needs their account.
When blocked by something outside your control, say exactly what you need and what you did already.

## Reporting

After each phase or chapter, report: what you ran, the real result, what changed (files), what is verified and what is still `Inferred`, and the next step from `progress.py next`. Keep it short and factual.

## File map

```
SKILL.md                      this file
references/00-agent-playbook.md      rules, decisions, definitions of done
references/01-setup-and-connectors.md  Unity and Blender MCP, tokens, Claude Code and Antigravity setup
references/02-unity-automation.md    batch mode, MCP use, the compile-fix loop
references/03-asset-pipeline.md      sources, licences, import, conversion, budgets
references/04-chapter-production.md  stage gates and recipes per chapter type
references/05-qa-ci-release.md       tests, CI, story gate, Steam, rollback
references/06-troubleshooting.md     agent-side problems and fixes
references/07-template-code-map.md   every C# file and where to extend it
references/08-episode-check.md       how to verify Inferred chapters
references/bible/                    the full production bible
references/project/                  the full engineering and delivery document
data/chapters.json                   the 52 chapters, machine-readable
data/asset-needs.json                what assets the game needs and where to get them
data/allowed-licenses.json           the licence policy
scripts/                             tooling (see each script's --help)
templates/unity/                     the Unity C# foundation, copied into the game project
templates/ci, templates/project      CI workflow, .gitignore, .gitattributes
mcp/                                 ready-to-merge MCP configuration files
tests/                               tests for the scripts, and the C# type-check
```
