# Installing the skill

## The easy way: let your AI do it

Paste the repository URL into Claude Code or Antigravity and say:

> Install the skill from this repository, then use it to build the Farhat game in ./FarhatGame.

The agent clones the repository, reads `AGENTS.md`, runs `python3 install.py`, and starts the build with
`skills/farhat-unity-builder/SKILL.md`.

## Claude Code plugin marketplace

If your Claude Code supports plugin marketplaces:

```
/plugin marketplace add <owner>/<repository>
/plugin install farhat-unity-builder@farhat-game
```

This repository contains `.claude-plugin/marketplace.json` and `.claude-plugin/plugin.json`. If your version expects a different
format, use the installer below.

## The installer

```
git clone <repository url>
cd <repository>
python3 install.py                    # current folder, Claude Code and Antigravity
python3 install.py --project ./Game   # another project folder
python3 install.py --target user      # all projects for this user
python3 install.py --dry-run          # show what would happen
python3 install.py --uninstall        # remove the installed skill folders
```

## What you provide

| You provide | Why |
| --- | --- |
| Unity 6.3 LTS installed through Unity Hub, with a licence activated | The game is built in Unity; no tool can do this for you |
| The licence for the series (the repository requires you to confirm it in writing before any release) | The game adapts a licensed series |
| Optional: Git and Git LFS, `uv`, Blender, a Sketchfab API token | Version control, the MCP servers, model conversion, Sketchfab downloads |

## Connecting the editors

The Unity MCP server connects while the Unity editor is open with the "MCP for Unity" package installed; the Blender MCP server connects while
Blender is open with its add-on running. Setup steps are in `skills/farhat-unity-builder/references/01-setup-and-connectors.md`.

## Checking the installation

```
python3 skills/farhat-unity-builder/scripts/check_env.py
python3 -m unittest discover -s skills/farhat-unity-builder/tests
```
