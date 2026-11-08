#!/usr/bin/env python3
"""Checks that this machine can run the build, and says exactly what is missing.

  check_env.py [--json]
Required: Python 3.9+, Git, a Unity 6.3 LTS editor. Recommended: Git LFS, uv (for the Unity and Blender MCP servers), Node.js.
Optional: Blender (asset conversion and the Blender MCP), Mono mcs (C# type-check), steamcmd (Steam uploads).
Exit code 0 when every required tool is present, 1 otherwise.
"""
import json
import os
import platform
import shutil
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import unity_cli  # noqa: E402


def version_of(command, args=("--version",)):
    try:
        out = subprocess.run([command] + list(args), capture_output=True, text=True, timeout=20)
        if out.returncode != 0:
            return None
        return (out.stdout or out.stderr).strip().splitlines()[0]
    except Exception:
        return None


def blender_path():
    for name in ("blender", "Blender"):
        found = shutil.which(name)
        if found:
            return found
    for pattern in ("C:/Program Files/Blender Foundation/Blender */blender.exe", "/Applications/Blender.app/Contents/MacOS/Blender"):
        import glob
        hits = glob.glob(pattern)
        if hits:
            return sorted(hits)[-1]
    return None


def collect():
    items = []

    def add(name, required, found, detail, fix):
        items.append({"name": name, "required": required, "ok": bool(found), "detail": detail or "", "fix": fix})

    add("Python 3.9+", True, sys.version_info >= (3, 9), platform.python_version(), "Install Python 3.9 or newer from python.org.")
    git = shutil.which("git")
    add("Git", True, git, version_of("git") if git else "", "Install Git from git-scm.com.")
    lfs = git and version_of("git", ("lfs", "version"))
    add("Git LFS", False, lfs, lfs, "Install Git LFS (git-lfs.com) and run 'git lfs install'. Large binary assets need it.")
    unity = unity_cli.find_unity()
    add("Unity editor", True, unity, unity, "Install Unity 6.3 LTS through Unity Hub, or set UNITY_EDITOR_PATH.")
    add("uv / uvx", False, shutil.which("uvx"), shutil.which("uvx"), "Install uv (docs.astral.sh/uv). The Unity MCP and Blender MCP servers run through uvx.")
    add("Node.js", False, shutil.which("node"), version_of("node"), "Install Node.js LTS if you want MCP servers that run through npx.")
    blender = blender_path()
    add("Blender", False, blender, blender, "Install Blender (blender.org) for model conversion and the Blender MCP.")
    add("Mono mcs", False, shutil.which("mcs"), shutil.which("mcs"), "Install mono-mcs to run tests/csharp-compile-check/check.sh.")
    add("steamcmd", False, shutil.which("steamcmd"), shutil.which("steamcmd"), "Install the Steamworks SDK / steamcmd for Steam uploads.")
    secrets = [("SKETCHFAB_API_TOKEN", "Needed to download Sketchfab models."), ("UNITY_EDITOR_PATH", "Optional explicit editor path.")]
    for name, why in secrets:
        add("env " + name, False, os.environ.get(name), "set" if os.environ.get(name) else "", why)
    return items


def main():
    as_json = "--json" in sys.argv
    items = collect()
    if as_json:
        print(json.dumps(items, indent=2))
    else:
        for i in items:
            mark = "ok " if i["ok"] else ("MISSING" if i["required"] else "no  ")
            print("%-8s %-22s %s" % (mark, i["name"], i["detail"] if i["ok"] else i["fix"]))
    return 0 if all(i["ok"] for i in items if i["required"]) else 1


if __name__ == "__main__":
    sys.exit(main())
