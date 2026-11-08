#!/usr/bin/env python3
"""Tracks production progress in .farhat/progress.json so an agent can resume exactly where the last session stopped.

Commands:
  init     create the progress file with all 52 chapters at stage 0
  status   print the phase and a table of chapter stages
  set      set a chapter stage:  progress.py set 5 greybox
  phase    set the project phase: progress.py phase slice
  next     print the next unit of work and why
Stages follow the bible's stage gates, in order.
"""
import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import load_chapters, load_json, save_json  # noqa: E402

STAGES = ["spec", "beat-sheet", "greybox", "systems", "art", "audio-cinematics", "narrative-loc", "qa-balance", "lock"]
PHASES = ["preproduction", "slice", "part-one", "part-two", "release"]
SLICE_CHAPTERS = [1, 5, 7]


def default_path():
    return os.path.join(os.getcwd(), ".farhat", "progress.json")


def init(path, force=False):
    if os.path.exists(path) and not force:
        raise SystemExit("progress file exists; use --force to overwrite")
    chapters = load_chapters()["chapters"]
    data = {"phase": "preproduction", "chapters": {str(c["number"]): {"stage": "spec", "verified": c["sourceTag"] == "Verified"} for c in chapters}}
    save_json(path, data)
    return data


def next_item(data):
    phase = data["phase"]
    chapters = data["chapters"]
    if phase == "preproduction":
        unverified = [int(n) for n, c in chapters.items() if not c["verified"]]
        return "Pre-production: run the prototype gate (movement, combat, Shadow), then the episode check. %d chapters are not Verified." % len(unverified)
    order = SLICE_CHAPTERS if phase == "slice" else sorted(int(n) for n in chapters)
    if phase == "part-two":
        order = [n for n in order if n >= 27]
    if phase == "part-one":
        order = [n for n in order if n <= 26]
    for n in order:
        c = chapters[str(n)]
        index = STAGES.index(c["stage"])
        if index < len(STAGES) - 1:
            blocker = "" if c["verified"] or STAGES[index + 1] != "lock" else " (cannot lock: chapter is not Verified)"
            return "Chapter %d is at stage '%s'; next stage is '%s'%s." % (n, c["stage"], STAGES[index + 1], blocker)
    return "All chapters in phase '%s' are locked. Advance the phase." % phase


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--file", default=default_path())
    sub = parser.add_subparsers(dest="cmd", required=True)
    p_init = sub.add_parser("init")
    p_init.add_argument("--force", action="store_true")
    sub.add_parser("status")
    p_set = sub.add_parser("set")
    p_set.add_argument("chapter", type=int)
    p_set.add_argument("stage", choices=STAGES)
    p_phase = sub.add_parser("phase")
    p_phase.add_argument("phase", choices=PHASES)
    sub.add_parser("next")
    args = parser.parse_args()

    if args.cmd == "init":
        init(args.file, args.force)
        print("created " + args.file)
        return 0
    data = load_json(args.file)
    if args.cmd == "status":
        print("phase: " + data["phase"])
        for n in sorted(data["chapters"], key=int):
            c = data["chapters"][n]
            print("  chapter %2d  %-18s %s" % (int(n), c["stage"], "verified" if c["verified"] else "NOT verified"))
    elif args.cmd == "set":
        entry = data["chapters"][str(args.chapter)]
        if args.stage == "lock" and not entry["verified"]:
            print("refusing to lock chapter %d: it is not Verified. Run the episode check first." % args.chapter)
            return 1
        entry["stage"] = args.stage
        save_json(args.file, data)
        print("chapter %d -> %s" % (args.chapter, args.stage))
    elif args.cmd == "phase":
        data["phase"] = args.phase
        save_json(args.file, data)
        print("phase -> " + args.phase)
    elif args.cmd == "next":
        print(next_item(data))
    return 0


if __name__ == "__main__":
    sys.exit(main())
