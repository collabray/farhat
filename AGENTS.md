# Instructions for AI agents (Claude Code, Antigravity and similar)

This repository is an installable skill: `skills/farhat-unity-builder/`. It guides an AI agent through building the Unity game
"Farhat: The Prince of the Desert" (52 chapters) from a finished design.

## If the user asked you to install it (for example by pasting this repository's URL)

1. Clone the repository if you do not have it yet, and work from its root.
2. Run `python3 install.py` (Windows: `py install.py`). It installs the skill for Claude Code and Antigravity into the current project,
   merges the Unity and Blender MCP configuration without overwriting existing servers, and checks the machine.
   - Use `--target user` to install for all projects, `--client claude` or `--client antigravity` for one client, `--dry-run` to preview.
3. Tell the user what was installed and where, and which checks failed (the usual one is a missing Unity 6.3 LTS editor).
4. Ask the user to restart the tool if the skill is not listed, then read `skills/farhat-unity-builder/SKILL.md` and follow it.

If you cannot run Python, copy `skills/farhat-unity-builder` to `.claude/skills/farhat-unity-builder` (Claude Code) and
`.agents/skills/farhat-unity-builder` (Antigravity), and copy the entries of `skills/farhat-unity-builder/mcp/*.json` into
`.mcp.json` / `.agents/mcp_config.json`.

## If the user asked you to build the game

Do not start from this file. Read `skills/farhat-unity-builder/SKILL.md` (or the installed copy) and follow its pipeline. In short:
confirm Unity is installed, scaffold the project, compile, generate the playable greybox of all 52 chapters, run the tests, then
produce chapters through the stage gates. Never invent story, never weaken the story gate, never download character art,
and stop at the human gates the skill lists.

## If you are changing this repository

Run before every commit:

```
python3 -m unittest discover -s tests
python3 -m unittest discover -s skills/farhat-unity-builder/tests
bash skills/farhat-unity-builder/tests/csharp-compile-check/check.sh    # needs mono-mcs
python3 skills/farhat-unity-builder/scripts/validate_chapters.py
```

The C# type-check uses API stubs. It does not replace a real Unity compile. Do not claim the C# has been compiled in Unity unless you ran
`scripts/unity_cli.py compile` with a real editor and saw it succeed.
