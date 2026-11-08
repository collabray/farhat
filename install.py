#!/usr/bin/env python3
"""Installs the farhat-unity-builder skill for Claude Code and Antigravity, and merges the MCP connector configuration.

Run from the repository root (an AI agent can run this for you):

    python3 install.py                       install into the current project for both clients
    python3 install.py --project ./MyGame    install into another project folder
    python3 install.py --target user         install for the current user (all projects) instead
    python3 install.py --target both         project and user
    python3 install.py --client claude       only Claude Code (or: antigravity)
    python3 install.py --no-mcp              skip the MCP configuration files
    python3 install.py --dry-run             show what would happen, change nothing
    python3 install.py --uninstall           remove what this script installed

Where things go
  Claude Code     <project>/.claude/skills/farhat-unity-builder          or  ~/.claude/skills/farhat-unity-builder
                  <project>/.mcp.json                                     (MCP servers, merged)
  Antigravity     <project>/.agents/skills/farhat-unity-builder          or  the user skills folder under ~/.gemini
                  <project>/.agents/mcp_config.json                       (MCP servers, merged)

Existing MCP servers are never overwritten (use --force-mcp to replace servers with the same name). Existing config files are backed up
to <name>.bak before they are changed. Nothing is sent over the network.
"""
import argparse
import json
import os
import shutil
import subprocess
import sys

SKILL_NAME = "farhat-unity-builder"
HERE = os.path.dirname(os.path.abspath(__file__))
SKILL_SOURCE = os.path.join(HERE, "skills", SKILL_NAME)
MCP_CLAUDE = os.path.join(SKILL_SOURCE, "mcp", "claude-code.mcp.json")
MCP_ANTIGRAVITY = os.path.join(SKILL_SOURCE, "mcp", "antigravity.mcp_config.json")
IGNORE = shutil.ignore_patterns("__pycache__", "*.pyc", ".pytest_cache", "*.part", "*.tmp")


def antigravity_user_skills_dir(home):
    """Antigravity has moved its global folder between versions: use the one that exists, else the newer location."""
    for candidate in (os.path.join(home, ".gemini", "config", "skills"), os.path.join(home, ".gemini", "antigravity", "skills")):
        if os.path.isdir(os.path.dirname(candidate)):
            return candidate
    return os.path.join(home, ".gemini", "config", "skills")


def plan(project, target, client, home):
    """Returns the list of skill destination folders."""
    destinations = []
    if target in ("project", "both"):
        if client in ("claude", "all"):
            destinations.append(os.path.join(project, ".claude", "skills", SKILL_NAME))
        if client in ("antigravity", "all"):
            destinations.append(os.path.join(project, ".agents", "skills", SKILL_NAME))
    if target in ("user", "both"):
        if client in ("claude", "all"):
            destinations.append(os.path.join(home, ".claude", "skills", SKILL_NAME))
        if client in ("antigravity", "all"):
            destinations.append(os.path.join(antigravity_user_skills_dir(home), SKILL_NAME))
    return destinations


def merge_mcp(existing, addition, force=False):
    """Merges mcpServers without dropping anything that is already configured. Returns (merged, added, kept)."""
    merged = dict(existing) if isinstance(existing, dict) else {}
    servers = dict(merged.get("mcpServers", {}))
    added, kept = [], []
    for name, config in addition.get("mcpServers", {}).items():
        if name in servers and not force:
            kept.append(name)
        else:
            servers[name] = config
            added.append(name)
    merged["mcpServers"] = servers
    return merged, added, kept


def write_mcp(path, source, force, dry_run):
    with open(source, "r", encoding="utf-8") as handle:
        addition = json.load(handle)
    existing = {}
    if os.path.exists(path):
        with open(path, "r", encoding="utf-8") as handle:
            text = handle.read().strip()
        existing = json.loads(text) if text else {}
    merged, added, kept = merge_mcp(existing, addition, force)
    summary = "%s: add %s%s" % (path, ", ".join(added) or "nothing", ("; keep existing " + ", ".join(kept)) if kept else "")
    if dry_run:
        return "[dry-run] " + summary
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    if os.path.exists(path) and added:
        shutil.copy2(path, path + ".bak")
    if added or not os.path.exists(path):
        with open(path, "w", encoding="utf-8") as handle:
            json.dump(merged, handle, indent=2)
            handle.write("\n")
    return summary


def install_skill(destination, dry_run):
    if dry_run:
        return "[dry-run] copy skill to " + destination
    if os.path.exists(destination):
        shutil.rmtree(destination)
    os.makedirs(os.path.dirname(destination), exist_ok=True)
    shutil.copytree(SKILL_SOURCE, destination, ignore=IGNORE)
    return "installed skill: " + destination


def uninstall(destinations, project, dry_run):
    messages = []
    for destination in destinations:
        if os.path.exists(destination):
            messages.append(("[dry-run] remove " if dry_run else "removed ") + destination)
            if not dry_run:
                shutil.rmtree(destination)
    messages.append("MCP configuration files were left in place; remove the unityMCP and blender entries by hand if you want them gone.")
    return messages


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--project", default=os.getcwd())
    parser.add_argument("--target", choices=["project", "user", "both"], default="project")
    parser.add_argument("--client", choices=["claude", "antigravity", "all"], default="all")
    parser.add_argument("--no-mcp", action="store_true")
    parser.add_argument("--force-mcp", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--uninstall", action="store_true")
    parser.add_argument("--home", default=os.path.expanduser("~"), help=argparse.SUPPRESS)
    args = parser.parse_args(argv)

    project = os.path.abspath(args.project)
    if not os.path.isfile(os.path.join(SKILL_SOURCE, "SKILL.md")):
        print("error: the skill folder is missing at %s. Run this script from a complete clone of the repository." % SKILL_SOURCE)
        return 1
    destinations = plan(project, args.target, args.client, args.home)

    if args.uninstall:
        for line in uninstall(destinations, project, args.dry_run):
            print(line)
        return 0

    for destination in destinations:
        print(install_skill(destination, args.dry_run))
    if not args.no_mcp and args.target in ("project", "both"):
        if args.client in ("claude", "all"):
            print(write_mcp(os.path.join(project, ".mcp.json"), MCP_CLAUDE, args.force_mcp, args.dry_run))
        if args.client in ("antigravity", "all"):
            print(write_mcp(os.path.join(project, ".agents", "mcp_config.json"), MCP_ANTIGRAVITY, args.force_mcp, args.dry_run))
    elif not args.no_mcp:
        print("MCP files are project-scoped. To register the servers for your user, copy the entries from "
              "skills/%s/mcp/ into your client's global MCP configuration." % SKILL_NAME)

    if args.dry_run:
        return 0

    print("\nChecking this machine (the build needs Unity 6.3 LTS):")
    check = os.path.join(SKILL_SOURCE, "scripts", "check_env.py")
    subprocess.run([sys.executable, check])
    print("\nInstalled. Next, tell your AI tool:\n"
          '  "Use the %s skill to build the Farhat game in ./FarhatGame."\n'
          "Restart the tool first if it does not list the skill. The Unity and Blender MCP servers only connect while their editors are running "
          "(see skills/%s/references/01-setup-and-connectors.md)." % (SKILL_NAME, SKILL_NAME))
    return 0


if __name__ == "__main__":
    sys.exit(main())
