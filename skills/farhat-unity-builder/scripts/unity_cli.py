#!/usr/bin/env python3
"""Runs Unity in batch mode for the common tasks of this project and turns the logs into short, actionable output.

  unity_cli.py create-project  --project PATH
  unity_cli.py compile         --project PATH
  unity_cli.py install-packages --project PATH
  unity_cli.py import-chapters --project PATH
  unity_cli.py generate-scenes --project PATH
  unity_cli.py validate        --project PATH [--require-scenes]
  unity_cli.py test            --project PATH --mode editmode|playmode
  unity_cli.py build           --project PATH --target win64|linux64|macos [--release] [--output FILE]
  unity_cli.py run-method      --project PATH --method Namespace.Type.Method [-- extra args]

Add --dry-run to print the exact Unity command instead of running it. The editor is found from --unity, then UNITY_EDITOR_PATH,
then the standard Unity Hub install folders (the highest 6000.3.x version wins).
Exit codes: 0 success, 1 problems found in Unity's output, 2 editor not found, 3 Unity failed without a recognisable error.
"""
import argparse
import glob
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

COMPILER_ERROR = re.compile(r"^(?P<file>.+?)\((?P<line>\d+),(?P<col>\d+)\): error (?P<code>CS\d+): (?P<message>.*)$", re.M)
EXCEPTION_LINE = re.compile(r"^(?:\[Farhat\] .*(?:failed|FAILED).*|.*Exception: .*|.*BuildFailedException.*)$", re.M)
TARGETS = {"win64": "BuildWindows64", "linux64": "BuildLinux64", "macos": "BuildMacOS"}


def candidate_paths():
    patterns = [
        r"C:\Program Files\Unity\Hub\Editor\*\Editor\Unity.exe",
        "/Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity",
        os.path.expanduser("~/Unity/Hub/Editor/*/Editor/Unity"),
        "/opt/unity/*/Editor/Unity",
        "/opt/Unity/Editor/Unity",
    ]
    found = []
    for pattern in patterns:
        found.extend(glob.glob(pattern))
    return found


def version_key(path):
    match = re.search(r"(\d+)\.(\d+)\.(\d+)", path)
    if not match:
        return (0, 0, 0, 0)
    major, minor, patch = (int(x) for x in match.groups())
    preferred = 1 if (major, minor) == (6000, 3) else 0
    return (preferred, major, minor, patch)


def find_unity(explicit=None):
    for candidate in (explicit, os.environ.get("UNITY_EDITOR_PATH"), os.environ.get("UNITY_PATH")):
        if candidate and os.path.exists(candidate):
            return candidate
    options = candidate_paths()
    if options:
        return sorted(options, key=version_key)[-1]
    return None


def build_command(unity, project, method=None, extra=None, log="Logs/unity-batch.log", quit_after=True, nographics=True):
    command = [unity, "-batchmode"]
    if nographics:
        command.append("-nographics")
    if quit_after:
        command.append("-quit")
    command += ["-projectPath", os.path.abspath(project), "-logFile", log]
    if method:
        command += ["-executeMethod", method]
    command += list(extra or [])
    return command


def parse_log(text):
    """Returns (errors, summary). errors is a list of dicts: file, line, code, message."""
    errors = [{"file": m.group("file"), "line": int(m.group("line")), "code": m.group("code"), "message": m.group("message")}
              for m in COMPILER_ERROR.finditer(text)]
    seen, unique = set(), []
    for e in errors:
        key = (e["file"], e["line"], e["code"])
        if key not in seen:
            seen.add(key)
            unique.append(e)
    others = [m.group(0).strip() for m in EXCEPTION_LINE.finditer(text)][:10]
    return unique, others


def parse_test_results(xml_path):
    """Parses NUnit 3 results written by -testResults. Returns dict(total, passed, failed, failures)."""
    root = ET.parse(xml_path).getroot()
    failures = []
    for case in root.iter("test-case"):
        if case.get("result") == "Failed":
            message = case.findtext("failure/message") or ""
            failures.append({"name": case.get("fullname"), "message": message.strip()})
    return {"total": int(root.get("total", 0)), "passed": int(root.get("passed", 0)), "failed": int(root.get("failed", 0)), "failures": failures}


def run(command, log_path, dry_run):
    print(" ".join('"%s"' % c if " " in c else c for c in command))
    if dry_run:
        return 0, ""
    os.makedirs(os.path.dirname(os.path.abspath(log_path)), exist_ok=True)
    completed = subprocess.run(command)
    text = ""
    if os.path.exists(log_path):
        with open(log_path, "r", encoding="utf-8", errors="replace") as handle:
            text = handle.read()
    return completed.returncode, text


def report(code, text):
    errors, others = parse_log(text)
    if errors:
        print("\n%d compiler error(s):" % len(errors))
        for e in errors[:50]:
            print("  %s(%d): %s %s" % (e["file"], e["line"], e["code"], e["message"]))
        return 1
    if code != 0:
        print("\nUnity exited with code %d." % code)
        for line in others:
            print("  " + line)
        print("  Full log: see the -logFile path above.")
        return 3 if not others else 1
    print("\nOK")
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("command", choices=["create-project", "compile", "install-packages", "import-chapters", "generate-scenes", "validate", "test", "build", "run-method"])
    parser.add_argument("--project", required=True)
    parser.add_argument("--unity", default=None)
    parser.add_argument("--mode", choices=["editmode", "playmode"], default="editmode")
    parser.add_argument("--target", choices=sorted(TARGETS), default="win64")
    parser.add_argument("--release", action="store_true")
    parser.add_argument("--output", default=None)
    parser.add_argument("--method", default=None)
    parser.add_argument("--require-scenes", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("extra", nargs="*")
    args = parser.parse_args()

    unity = find_unity(args.unity)
    if unity is None and not args.dry_run:
        print("Unity editor not found. Install Unity 6.3 LTS through Unity Hub, or set UNITY_EDITOR_PATH to the editor executable.")
        return 2
    unity = unity or "<unity-editor>"
    log = os.path.join(args.project, "Logs", "unity-%s.log" % args.command)
    ns = "Farhat.EditorTools."

    if args.command == "create-project":
        command = [unity, "-batchmode", "-nographics", "-quit", "-createProject", os.path.abspath(args.project), "-logFile", log]
    elif args.command == "compile":
        command = build_command(unity, args.project, None, None, log)
    elif args.command == "install-packages":
        command = build_command(unity, args.project, ns + "PackageInstaller.InstallAll", None, log, quit_after=False)
    elif args.command == "import-chapters":
        command = build_command(unity, args.project, ns + "ChapterCatalogImporter.ImportAll", None, log)
    elif args.command == "generate-scenes":
        command = build_command(unity, args.project, ns + "SceneGenerator.GenerateAllCli", None, log, quit_after=False)
    elif args.command == "validate":
        command = build_command(unity, args.project, ns + "ContentValidator.RunCli", ["-requireScenes"] if args.require_scenes else [], log, quit_after=False)
    elif args.command == "run-method":
        if not args.method:
            print("--method is required")
            return 1
        command = build_command(unity, args.project, args.method, args.extra, log, quit_after=False)
    elif args.command == "build":
        output = args.output or {"win64": "Build/Win64/Farhat.exe", "linux64": "Build/Linux64/Farhat.x86_64", "macos": "Build/macOS/Farhat.app"}[args.target]
        extra = ["-buildOutput", output] + (["-releaseBuild"] if args.release else [])
        command = build_command(unity, args.project, ns + "BuildScript." + TARGETS[args.target], extra, log, quit_after=False)
    else:  # test
        results = os.path.abspath(os.path.join(args.project, "Logs", "results-%s.xml" % args.mode))
        platform = "EditMode" if args.mode == "editmode" else "PlayMode"
        command = [unity, "-batchmode"] + (["-nographics"] if args.mode == "editmode" else []) + [
            "-projectPath", os.path.abspath(args.project), "-runTests", "-testPlatform", platform, "-testResults", results, "-logFile", log]
        code, text = run(command, log, args.dry_run)
        if args.dry_run:
            return 0
        if os.path.exists(results):
            summary = parse_test_results(results)
            print("\n%s tests: %d total, %d passed, %d failed" % (platform, summary["total"], summary["passed"], summary["failed"]))
            for f in summary["failures"]:
                print("  FAILED %s\n    %s" % (f["name"], f["message"]))
            return 0 if summary["failed"] == 0 and summary["total"] > 0 else 1
        return report(code, text) or 1

    code, text = run(command, log, args.dry_run)
    if args.dry_run:
        return 0
    return report(code, text)


if __name__ == "__main__":
    sys.exit(main())
