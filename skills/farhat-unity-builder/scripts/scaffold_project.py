#!/usr/bin/env python3
"""Creates (or updates) the Unity game project from this skill's templates.

  scaffold_project.py --project ./FarhatGame [--no-create] [--skip-packages] [--force] [--with-docs]

What it does
  1. Creates the Unity project with the editor in batch mode if the folder is not a Unity project yet (skip with --no-create).
  2. Copies only Assets/Game/Bootstrap first (the package installer, in its own assembly) and installs the packages the game needs, because
     the game code cannot compile until those packages exist. Skipped with --skip-packages or when no editor is found.
  3. Copies the rest of templates/unity/Assets/Game into Assets/Game (existing files are kept unless --force).
  4. Copies data/chapters.json into Assets/Game/Content/Data/chapters.json (the latest copy always wins).
  5. Adds .gitignore, .gitattributes and the GitHub Actions workflow when the project has none.
  6. With --with-docs, copies the full bible and project document into Docs/.
  7. Creates .farhat/progress.json so work can be resumed.
Next step printed at the end: compile, generate the greybox scenes, run the tests.
"""
import argparse
import os
import shutil
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import SKILL_ROOT  # noqa: E402
import progress  # noqa: E402
import unity_cli  # noqa: E402


def copy_tree(source, destination, force):
    copied, kept = 0, 0
    for folder, _, files in os.walk(source):
        relative = os.path.relpath(folder, source)
        target_folder = os.path.join(destination, relative) if relative != "." else destination
        os.makedirs(target_folder, exist_ok=True)
        for name in files:
            target = os.path.join(target_folder, name)
            if os.path.exists(target) and not force:
                kept += 1
                continue
            shutil.copy2(os.path.join(folder, name), target)
            copied += 1
    return copied, kept


def copy_file(source, destination, force):
    if os.path.exists(destination) and not force:
        return False
    os.makedirs(os.path.dirname(os.path.abspath(destination)), exist_ok=True)
    shutil.copy2(source, destination)
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--project", required=True)
    parser.add_argument("--unity", default=None)
    parser.add_argument("--no-create", action="store_true")
    parser.add_argument("--skip-packages", action="store_true")
    parser.add_argument("--force", action="store_true")
    parser.add_argument("--with-docs", action="store_true")
    args = parser.parse_args()
    project = os.path.abspath(args.project)

    is_unity_project = os.path.exists(os.path.join(project, "ProjectSettings", "ProjectVersion.txt"))
    if not is_unity_project and not args.no_create:
        unity = unity_cli.find_unity(args.unity)
        if unity is None:
            print("Unity editor not found, so the project cannot be created automatically.\n"
                  "Create an empty 'Universal 3D' project with Unity Hub in %s, then run this script again with --no-create." % project)
            return 2
        os.makedirs(project, exist_ok=True)
        print("Creating the Unity project (this takes a few minutes the first time)...")
        code = subprocess.run([unity, "-batchmode", "-nographics", "-quit", "-createProject", project,
                               "-logFile", os.path.join(project, "create-project.log")]).returncode
        if code != 0 or not os.path.exists(os.path.join(project, "ProjectSettings", "ProjectVersion.txt")):
            print("Unity could not create the project (exit code %d). See create-project.log." % code)
            return 1
    os.makedirs(os.path.join(project, "Assets"), exist_ok=True)

    templates = os.path.join(SKILL_ROOT, "templates")
    game_source = os.path.join(templates, "unity", "Assets", "Game")
    game_target = os.path.join(project, "Assets", "Game")

    # Stage 1: the package installer lives in its own assembly so it can run before the game code can compile.
    copy_tree(os.path.join(game_source, "Bootstrap"), os.path.join(game_target, "Bootstrap"), args.force)
    unity = unity_cli.find_unity(args.unity)
    if unity and not args.skip_packages:
        print("Installing the Unity packages the game needs (Input System, URP, AI Navigation, Cinemachine, ...)...")
        code = subprocess.run([sys.executable, os.path.join(os.path.dirname(os.path.abspath(__file__)), "unity_cli.py"),
                               "install-packages", "--project", project, "--unity", unity]).returncode
        if code != 0:
            print("Package installation failed (exit code %d). Fix the problem shown above and run this script again." % code)
            return 1
    elif not args.skip_packages:
        print("No Unity editor found, so the packages were not installed. Run 'unity_cli.py install-packages' before compiling.")

    # Stage 2: everything else.
    copied, kept = copy_tree(game_source, game_target, args.force)
    print("Assets/Game: %d files written, %d existing files kept." % (copied, kept))
    shutil.copy2(os.path.join(SKILL_ROOT, "data", "chapters.json"), os.path.join(project, "Assets", "Game", "Content", "Data", "chapters.json"))
    print("Assets/Game/Content/Data/chapters.json updated.")

    for name in (".gitignore", ".gitattributes"):
        if copy_file(os.path.join(templates, "project", name), os.path.join(project, name), args.force):
            print("wrote " + name)
    if copy_file(os.path.join(templates, "ci", "unity-ci.yml"), os.path.join(project, ".github", "workflows", "unity-ci.yml"), args.force):
        print("wrote .github/workflows/unity-ci.yml")
    if args.with_docs:
        docs = os.path.join(project, "Docs")
        for folder in ("bible", "project"):
            copy_tree(os.path.join(SKILL_ROOT, "references", folder), os.path.join(docs, folder), True)
        shutil.copy2(os.path.join(SKILL_ROOT, "data", "chapters.json"), os.path.join(docs, "chapters.json"))
        print("Docs/ written.")

    progress_file = os.path.join(project, ".farhat", "progress.json")
    if not os.path.exists(progress_file):
        progress.init(progress_file)
        print("created .farhat/progress.json")

    print("\nNext:\n"
          "  python3 scripts/unity_cli.py install-packages --project %s\n"
          "  python3 scripts/unity_cli.py compile --project %s\n"
          "  python3 scripts/unity_cli.py generate-scenes --project %s\n"
          "  python3 scripts/unity_cli.py test --project %s --mode editmode" % (project, project, project, project))
    return 0


if __name__ == "__main__":
    sys.exit(main())
