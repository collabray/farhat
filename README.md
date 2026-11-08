# Farhat Unity Builder: an AI skill that builds the game

An installable skill for AI coding agents (Claude Code, Antigravity and similar). It teaches the agent to build
**Farhat: The Prince of the Desert**, a story-driven 3D action-adventure of 52 chapters, in Unity 6, from a finished production bible.

## Install: paste this repository's URL to your AI

Open Claude Code or Antigravity in the folder where you want to work and say:

> Install the skill from `https://github.com/collabray/farhat.git`, then use it to build the Farhat game in `./FarhatGame`.

The agent clones the repository, reads `AGENTS.md`, runs `python3 install.py`, and follows
`skills/farhat-unity-builder/SKILL.md`. To do it by hand: `python3 install.py` (see `INSTALL.md` for options, user-wide install and the Claude Code plugin route).

## What you provide

| You provide | Why |
| --- | --- |
| Unity 6.3 LTS through Unity Hub, with a licence activated | Nothing can build a Unity game without the editor |
| Confirmation of your licence for the series, in writing, before any release | The game adapts a licensed series; the skill asks for this |
| Episode checks (watching episodes or supplying synopses) | 31 of the 52 chapters are not verified against the episodes; only a person can verify them |
| Optional: Git LFS, `uv`, Blender, a Sketchfab token | Version control, MCP servers, model conversion, Sketchfab downloads |

## What the skill does

1. **Scaffolds** the Unity project, installs the needed packages, and copies a complete C# foundation (core, data, save, systems, story flow, UI, editor tools, tests).
2. **Generates a playable greybox of all 52 chapters** from the chapter data: objectives, checkpoints, fragments, enemies, timers, stealth and corruption hazards, chapter exits, and a Bootstrap scene that runs chapters 1 to 52 in order.
3. **Proves it** with Unity tests, a chapter smoke-test bot (walks every chapter's critical path), a fail simulator (fires every failure type), content validation and a story gate.
4. **Guides production** chapter by chapter through nine stage gates, with a recipe per chapter type and the special requirements from the bible.
5. **Fetches licensed assets** (Poly Haven, Sketchfab, direct CC0 packs) through scripts that refuse non-commercial, no-derivatives, share-alike and unknown licences, refuse fan art of the licensed characters, verify checksums, and keep a ledger that generates `CREDITS.md`.
6. **Releases** through CI (GameCI), a release build that fails while any included chapter is not verified, and a checklist for Steam.

It carries the full bible (`references/bible/`) and project document (`references/project/`) so the agent never has to guess.

## Honest status

| Part | Status |
| --- | --- |
| Python tooling (31 tests plus installer tests) | Runs and passes. Asset downloads are tested against a local mock of the Poly Haven and Sketchfab API shapes, not the live services |
| C# foundation (66 files) | Type-checked against API stubs with Mono's compiler (`tests/csharp-compile-check`). **Not yet compiled in a real Unity editor.** Expect to fix a few errors on the first compile; the skill's first phase is exactly that loop |
| Unity tests, smoke-test bot, scene generator | Written to the Unity Test Framework and editor APIs; they run only inside Unity, so they have not been run here |
| MCP configuration | Based on the vendors' published setup (Unity MCP by CoplayDev, Blender MCP); not run against live editors here |
| Chapter data | 52 chapters. 21 verified from synopses, 31 inferred from titles or partial synopses. The data and tests keep the 31 from being locked or released |
| Final art, voices, music, licensed character models | Not included and not downloaded. Characters are modelled from the rights holder's model sheets; the skill uses CC0 stand-ins meanwhile |
| Fun | Decided by playtests with people, not by tests |

## Layout

```
AGENTS.md  CLAUDE.md  INSTALL.md  install.py  NOTICE.md  LICENSE
.claude-plugin/                  Claude Code plugin and marketplace metadata
.github/workflows/validate.yml   CI for this repository
tests/                           installer tests
skills/farhat-unity-builder/
  SKILL.md                       the skill (read this first)
  references/                    playbook, setup, automation, assets, chapters, QA, troubleshooting, code map, episode check
  references/bible/              the full production bible (19 files)
  references/project/            the full engineering and delivery document (19 files)
  data/                          chapters.json (52 chapters), asset-needs.json, allowed-licenses.json
  scripts/                       scaffold_project, unity_cli, asset_fetch, license_ledger, validate_chapters, story_report, progress, check_env
  templates/unity/               the Unity C# foundation, tests, editor tools
  templates/ci, templates/project  CI workflow, .gitignore, .gitattributes
  mcp/                           MCP server configuration for Claude Code and Antigravity
  tests/                         script tests, integrity tests, the C# type-check
```

## Verify this repository

```
python3 -m unittest discover -s tests
python3 -m unittest discover -s skills/farhat-unity-builder/tests
python3 skills/farhat-unity-builder/scripts/validate_chapters.py
bash skills/farhat-unity-builder/tests/csharp-compile-check/check.sh     # needs mono-mcs
```

## Intellectual property

The series belongs to its rights holders; this repository contains none of its footage, images, audio or scripts. See `NOTICE.md`.
The repository's own code and documents are MIT licensed (`LICENSE`).
