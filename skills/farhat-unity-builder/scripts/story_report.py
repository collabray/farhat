#!/usr/bin/env python3
"""Writes the episode-check worklist: every chapter that is not Verified, grouped by what the writers must confirm.

Usage: story_report.py [--out docs/episode-check.md] [--csv docs/story-report.csv]
"""
import argparse
import csv
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import load_chapters  # noqa: E402


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--chapters", default=None)
    parser.add_argument("--out", default=None)
    parser.add_argument("--csv", default=None)
    args = parser.parse_args()
    data = load_chapters(args.chapters)
    chapters = data["chapters"]
    unverified = [c for c in chapters if c["sourceTag"] != "Verified"]

    lines = ["# Episode check worklist", "",
             "Verified chapters: %d. Chapters still to check against the licensed episodes: %d." % (len(chapters) - len(unverified), len(unverified)), "",
             "For each chapter: watch the episode, then correct `data/chapters.json` (spec fields, objectives, location), set `sourceTag` to `Verified` and `bibleTag` to `V`, "
             "run `python3 scripts/validate_chapters.py`, and re-import the chapters in Unity.", "",
             "| Ch | Episode | Bible tag | Cost tier | Type | What to confirm |", "| --- | --- | --- | --- | --- | --- |"]
    for c in unverified:
        confirm = "Full story beats, location, antagonists"
        if c["bibleTag"] == "V/I" or c["number"] == 20:
            confirm = "Complete the partly verified synopsis and the exact resolution"
        if c.get("provisionalUnlocks"):
            confirm += "; fragment placement (" + ", ".join(c["provisionalUnlocks"]) + ")"
        lines.append("| %d | %s | [%s] | %s | %s | %s |" % (c["number"], c["episodeTitle"], c["bibleTag"], c["costTier"], c["chapterType"], confirm))
    text = "\n".join(lines) + "\n"
    if args.out:
        os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
        with open(args.out, "w", encoding="utf-8") as handle:
            handle.write(text)
        print("wrote " + args.out)
    else:
        print(text)
    if args.csv:
        os.makedirs(os.path.dirname(os.path.abspath(args.csv)), exist_ok=True)
        with open(args.csv, "w", newline="", encoding="utf-8") as handle:
            writer = csv.writer(handle)
            writer.writerow(["number", "episodeTitle", "sourceTag", "bibleTag", "costTier", "chapterType"])
            for c in chapters:
                writer.writerow([c["number"], c["episodeTitle"], c["sourceTag"], c["bibleTag"], c["costTier"], c["chapterType"]])
        print("wrote " + args.csv)
    return 0


if __name__ == "__main__":
    sys.exit(main())
