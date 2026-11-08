# QA, CI and release

The full plan is in `references/project/09-*.md` (testing), `10-*.md` (performance), `12-*.md` (deployment) and `15-*.md` (legal). This file
says which command proves each requirement.

## 1. Test layers and the commands that run them

| Layer | Command | What it proves |
| --- | --- | --- |
| Skill data and tools | `python3 -m unittest discover -s tests` (in the skill) | `chapters.json` matches the bible; asset tooling refuses bad licences; Unity CLI parsing; scaffolding |
| C# type-check (no Unity) | `bash tests/csharp-compile-check/check.sh` (needs Mono `mcs`) | Our C# is internally consistent against API stubs. It is not a Unity compile |
| Unity compile | `python3 scripts/unity_cli.py compile --project P` | The real compiler accepts the project |
| EditMode tests | `unity_cli.py test --project P --mode editmode` | Save migration, event bus, objectives, checkpoints, unlocks, difficulty, decode puzzle, chapter data and content validation |
| PlayMode tests | `unity_cli.py test --project P --mode playmode` | Health, Shadow, alchemy, awareness, timers, and (after greybox generation) the chapter smoke-test bot and fail simulator |
| Content validation | `unity_cli.py validate --project P --require-scenes` | Ids, objectives, checkpoints, rules, fragments, scenes |
| Licence ledger | `license_ledger.py verify` | Every third-party file is recorded and unchanged |

Limit the slow smoke tests while iterating: `FARHAT_SMOKE_CHAPTERS=1,5,7` (environment variable) before running the PlayMode tests. Run all 52 before any release candidate.

The chapter smoke-test bot loads the generated Bootstrap scene, starts one chapter, moves the player into each objective in order, defeats or
frees what must be defeated or freed, and walks to the exit. The fail simulator fires every failure type the chapter declares and checks the
player comes back at full health from a checkpoint. When you replace greybox with real levels, these tests must keep passing; if a
bespoke level legitimately needs a different path, extend the bot (a `CriticalPath` waypoint list per chapter) rather than deleting the test.

## 2. Performance

Targets are planning budgets until the slice confirms them (`references/project/10-*.md`): 60 fps on the minimum PC spec, 30 on the
lowest console, chapter load under 15 seconds, per-chapter memory budget. Profile on the lowest-spec target with the Unity Profiler and
Frame Debugger at greybox and again at art lock for every Tier A chapter. Write measured numbers into `Docs/budgets.md`. A chapter that
misses its budget without a plan does not pass `qa-balance`.

## 3. Playtests

Follow the protocol in `references/project/09-*.md` section 9.3: players aged 8 to 14, fans, and newcomers; observed sessions of 30 to 45
minutes; guardians present for minors; no personal data collected. You cannot run a playtest. Prepare the build, the questionnaire and the
recording checklist, ask the human to run it, and record the ranked fix list in `Docs/playtests/`.

## 4. CI

`templates/ci/unity-ci.yml` is copied into the project by the scaffolder. It runs EditMode and PlayMode tests on every push and pull request,
builds the integration Windows build on `main`, and builds a release on tags that start with `v`. Needs three repository secrets
(`UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`); the human creates them following the GameCI documentation. The release job passes
`-releaseBuild`, so it fails while any included chapter is not `Verified`.

## 5. The story gate

A build with the scripting define `RELEASE_BUILD` fails in `StoryGate.OnPreprocessBuild` if any chapter with `includedInBuild` is not
`Verified`. Two legitimate ways to ship earlier:

1. Ship an episode drop: set `includedInBuild = false` on every chapter that is not part of the drop (Chapter Board window, or the asset), and release only verified chapters. Chapters 1 to 9 are not all verified (4, 6 and 9 are not), so Episode 1 needs the episode check on those first.
2. Complete the episode check (see `08-episode-check.md`).

Never edit `StoryGate`, remove `RELEASE_BUILD`, or retag a chapter without the human's confirmation that the episode check was done.

## 6. Release checklist

Work through `references/project/12-deployment-and-release.md` section 12.4 in order. Items you can prove with commands: build succeeds with
`--release`; ledger verifies; `CREDITS.md` generated; all tests green. Items only the human can do: licence confirmation in writing, age-rating
questionnaires, store page and capsule art, Steam Direct fee (30-day wait), trailers, marketing. List them clearly and stop.

## 7. Steam upload

Use Valve's `steamcmd` with a build script (`app_build`) and depot configuration created in Steamworks. Upload to a private test branch first,
never straight to the default branch. Credentials belong to the human; do not store them. Valve's documentation is the authority for the
script format; read it when you get there, since formats and tooling change.

## 8. Rollback and patches

Keep the previous build on a Steam branch so it can be promoted back. A save made by any released version must load in later versions: when
you change `SaveData`, bump `SaveMigrator.Current`, add one migration step and one test per step. Severity triage S1 to S4 is in
`references/project/11-troubleshooting-guide.md` section 11.3.
