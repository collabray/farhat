"""Asset licence ledger. Every third-party file that enters the game is recorded here with its source, author and licence.
Standard library only."""
import datetime
import json
import os
import re

from _common import DATA_DIR, load_json, save_json, sha256_file

ALLOWED = load_json(os.path.join(DATA_DIR, "allowed-licenses.json"))


def normalize_license(label):
    """Map a licence label or slug from a source site to 'cc0', 'cc-by' or None (refused)."""
    if not label:
        return None
    text = str(label).lower().replace("_", "-")
    tokens = set(re.split(r"[^a-z0-9]+", text))
    if {"nc", "nd", "sa"} & tokens or "noncommercial" in text or "no derivatives" in text or "sharealike" in text or "share alike" in text:
        return None
    if "editorial" in tokens or "standard" in tokens:
        return None
    if "ofl" in tokens or "open font license" in text:
        return "ofl"
    if "cc0" in tokens or "public domain" in text or "publicdomain" in text:
        return "cc0"
    if "by" in tokens or "attribution" in text:
        return "cc-by"
    return None


def load_ledger(path):
    if os.path.exists(path):
        return load_json(path)
    return {"schemaVersion": 1, "entries": []}


def add_entry(path, entry):
    data = load_ledger(path)
    entry = dict(entry)
    entry["downloadedAt"] = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    data["entries"] = [e for e in data["entries"] if not (e["source"] == entry["source"] and e["id"] == entry["id"])]
    data["entries"].append(entry)
    save_json(path, data)
    return entry


def verify(path, base_dir):
    """Returns a list of problems. Empty means the ledger is consistent with the files on disk."""
    problems = []
    data = load_ledger(path)
    required = ("id", "name", "source", "sourceUrl", "author", "license", "files")
    for e in data["entries"]:
        label = "%s/%s" % (e.get("source"), e.get("id"))
        for key in required:
            if not e.get(key):
                problems.append("%s: missing %s" % (label, key))
        if e.get("license") not in ("cc0", "cc-by", "ofl"):
            problems.append("%s: licence %r is not allowed" % (label, e.get("license")))
        if e.get("license") == "cc-by" and not e.get("attribution"):
            problems.append("%s: CC BY needs an attribution line" % label)
        for f in e.get("files", []):
            full = os.path.join(base_dir, f["path"])
            if not os.path.exists(full):
                problems.append("%s: file missing %s" % (label, f["path"]))
            elif sha256_file(full) != f["sha256"]:
                problems.append("%s: checksum changed for %s" % (label, f["path"]))
    return problems


def credits_markdown(path):
    data = load_ledger(path)
    entries = sorted(data["entries"], key=lambda e: (e["license"], e["name"].lower()))
    lines = ["# Third-party asset credits", "",
             "Generated from the asset ledger. Do not edit by hand; run `python3 scripts/license_ledger.py credits`.", ""]
    for lic, title in (("cc-by", "Creative Commons Attribution (credit required)"), ("cc0", "CC0 public domain (credit given as courtesy)"), ("ofl", "SIL Open Font License (fonts)")):
        group = [e for e in entries if e["license"] == lic]
        if not group:
            continue
        lines += ["## " + title, ""]
        for e in group:
            line = e.get("attribution") or "%s by %s (%s)" % (e["name"], e["author"], e["sourceUrl"])
            lines.append("- " + line)
        lines.append("")
    if any(e["source"] == "polyhaven" for e in entries):
        lines += ["Powered by Poly Haven (https://polyhaven.com).", ""]
    return "\n".join(lines)
