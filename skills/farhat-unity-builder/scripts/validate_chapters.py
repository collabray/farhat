#!/usr/bin/env python3
"""Validates data/chapters.json against the facts in the production bible. Exit code 0 means valid.

Usage: validate_chapters.py [path/to/chapters.json]
"""
import sys
import os

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import load_chapters, EXPECTED_VERIFIED  # noqa: E402

VALID_TAGS = {"Verified", "Inferred"}
VALID_TIERS = {"A", "B", "C"}
FAILURE_TYPES = {"Health", "Objective", "Stealth", "Chase", "Puzzle", "ImpulseTrap", "Corruption"}
KEY_UNLOCKS = {
    "shadow.scout": 1, "shadow.swap": 5, "shadow.shape": 7,
    "potion.heal": 2, "potion.boost": 6, "potion.transform": 11, "potion.cure": 14, "potion.weather": 22,
    "meter.tier1": 4, "meter.tier2": 15, "meter.tier3": 18, "meter.max": 25,
    "item.anti-reversal": 31, "system.black-hole-closing": 33, "shadow.evolved": 48,
}
HEART_CHAPTERS = [4, 10, 15, 21, 25]


def validate(data):
    errors = []
    chapters = data.get("chapters", [])
    if len(chapters) != 52:
        errors.append("expected 52 chapters, found %d" % len(chapters))
    numbers = [c.get("number") for c in chapters]
    if numbers != list(range(1, len(chapters) + 1)):
        errors.append("chapter numbers must run 1..N in order")
    scenes, ids = set(), set()
    owners = {}
    verified = []
    unlock_where = {}
    hearts = []
    for c in chapters:
        tag = "chapter %s" % c.get("number")
        for key in ("id", "episodeTitle", "sourceTag", "costTier", "chapterType", "sceneName", "locPrefix", "objectives", "checkpointIds", "failureRules", "unlocks", "storyBeats", "spec"):
            if key not in c:
                errors.append("%s: missing field %s" % (tag, key))
        if c.get("sourceTag") not in VALID_TAGS:
            errors.append("%s: bad sourceTag %r" % (tag, c.get("sourceTag")))
        if c.get("costTier") not in VALID_TIERS:
            errors.append("%s: bad costTier %r" % (tag, c.get("costTier")))
        if c.get("id") in ids:
            errors.append("%s: duplicate id" % tag)
        ids.add(c.get("id"))
        if c.get("sceneName") in scenes:
            errors.append("%s: duplicate scene name" % tag)
        scenes.add(c.get("sceneName"))
        if c.get("sourceTag") == "Verified":
            verified.append(c.get("number"))
        if c.get("season") != (1 if c.get("number", 0) <= 26 else 2):
            errors.append("%s: wrong season" % tag)
        prefix = c.get("locPrefix", "")
        objective_ids = set()
        tier1 = 0
        for o in c.get("objectives", []):
            if not o.get("id", "").startswith(prefix):
                errors.append("%s: objective id %r must start with %s" % (tag, o.get("id"), prefix))
            if o.get("id") in objective_ids:
                errors.append("%s: duplicate objective %s" % (tag, o.get("id")))
            objective_ids.add(o.get("id"))
            if o.get("tier") not in (1, 2, 3):
                errors.append("%s: objective %s has bad tier" % (tag, o.get("id")))
            if o.get("tier") == 1:
                tier1 += 1
            if not o.get("text"):
                errors.append("%s: objective %s has no text" % (tag, o.get("id")))
        if tier1 == 0:
            errors.append("%s: needs at least one tier-1 objective" % tag)
        checkpoints = c.get("checkpointIds", [])
        if not checkpoints or checkpoints[0] != "start":
            errors.append("%s: first checkpoint must be 'start'" % tag)
        has_health = False
        for r in c.get("failureRules", []):
            if r.get("type") not in FAILURE_TYPES:
                errors.append("%s: unknown failure type %r" % (tag, r.get("type")))
            target = r.get("checkpointId", "")
            if target and target not in checkpoints:
                errors.append("%s: rule %s points at undeclared checkpoint %r" % (tag, r.get("type"), target))
            if r.get("type") == "Health":
                has_health = True
            if r.get("type") == "ImpulseTrap" and (c.get("number") != 17 or not r.get("lockShadowNextArea")):
                errors.append("%s: ImpulseTrap belongs only to chapter 17 and must lock the Shadow" % tag)
        if c.get("failureRules") and not has_health:
            errors.append("%s: chapters that can fail need a Health rule" % tag)
        for u in c.get("unlocks", []):
            if u.startswith("fragment."):
                if u in owners:
                    errors.append("%s: %s already granted by chapter %s" % (tag, u, owners[u]))
                owners[u] = c.get("number")
            if u.startswith("hearts.plus1."):
                hearts.append(c.get("number"))
            unlock_where[u] = c.get("number")
        spec = c.get("spec", {})
        if not spec.get("storyBeats"):
            errors.append("%s: spec.storyBeats is empty" % tag)
    for i in range(1, 8):
        if "fragment.%d" % i not in owners:
            errors.append("fragment.%d is not granted by any chapter" % i)
    if owners.get("fragment.5") != 18:
        errors.append("fragment.5 must be granted in chapter 18 (Stonehenge)")
    if owners.get("fragment.6") != 21:
        errors.append("fragment.6 must be granted in chapter 21")
    if verified != EXPECTED_VERIFIED:
        errors.append("Verified chapters %s differ from the bible's list %s" % (verified, EXPECTED_VERIFIED))
    if sorted(hearts) != HEART_CHAPTERS:
        errors.append("heart upgrades must be in chapters %s, found %s" % (HEART_CHAPTERS, sorted(hearts)))
    for key, chapter in KEY_UNLOCKS.items():
        if unlock_where.get(key) != chapter:
            errors.append("unlock %s should be in chapter %d, found %s" % (key, chapter, unlock_where.get(key)))
    return errors


def main(argv):
    if len(argv) > 1 and argv[1] in ("-h", "--help"):
        print(__doc__)
        return 0
    path = argv[1] if len(argv) > 1 else None
    errors = validate(load_chapters(path))
    if errors:
        print("chapters.json is INVALID (%d problems):" % len(errors))
        for e in errors:
            print(" - " + e)
        return 1
    print("chapters.json is valid: 52 chapters, 21 Verified, 31 Inferred, fragments 1-7 placed once.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
