import glob
import json
import os
import re
import subprocess
import sys
import unittest

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, os.path.join(ROOT, "scripts"))
from _common import load_chapters  # noqa: E402

PATH_PATTERN = re.compile(r"`((?:references|scripts|data|templates|mcp|tests)/[A-Za-z0-9_./\-]+?)`")
BANNED = re.compile(r"\b(TODO|FIXME|TBD|lorem ipsum|placeholder|dummy data|fake data)\b", re.I)


def own_docs():
    files = [os.path.join(ROOT, "SKILL.md")]
    files += sorted(glob.glob(os.path.join(ROOT, "references", "0*.md")))
    return files


class IntegrityTests(unittest.TestCase):
    def test_every_path_named_in_the_docs_exists(self):
        missing = []
        for doc in own_docs():
            with open(doc, encoding="utf-8") as handle:
                text = handle.read()
            for match in PATH_PATTERN.finditer(text):
                path = match.group(1).rstrip(".,;:")
                if any(ch in path for ch in "<>*"):
                    continue
                if not os.path.exists(os.path.join(ROOT, path)):
                    missing.append("%s mentions %s" % (os.path.relpath(doc, ROOT), path))
        self.assertEqual(missing, [])

    def test_no_placeholder_wording_in_authored_files(self):
        offenders = []
        authored = own_docs()
        for pattern in ("scripts/*.py", "templates/unity/**/*.cs", "templates/ci/*", "templates/project/.git*", "mcp/*.json", "data/asset-needs.json", "data/allowed-licenses.json"):
            authored += glob.glob(os.path.join(ROOT, pattern), recursive=True)
        for path in authored:
            with open(path, encoding="utf-8") as handle:
                for number, line in enumerate(handle, 1):
                    if BANNED.search(line):
                        offenders.append("%s:%d: %s" % (os.path.relpath(path, ROOT), number, line.strip()[:80]))
        self.assertEqual(offenders, [])

    def test_mcp_configs_are_valid_and_consistent(self):
        claude = json.load(open(os.path.join(ROOT, "mcp", "claude-code.mcp.json")))
        anti = json.load(open(os.path.join(ROOT, "mcp", "antigravity.mcp_config.json")))
        self.assertEqual(sorted(claude["mcpServers"]), sorted(anti["mcpServers"]))
        self.assertEqual(claude["mcpServers"]["unityMCP"]["url"], anti["mcpServers"]["unityMCP"]["serverUrl"])
        self.assertEqual(claude["mcpServers"]["blender"], anti["mcpServers"]["blender"])

    def test_asset_needs_reference_real_chapters_and_sources(self):
        needs = json.load(open(os.path.join(ROOT, "data", "asset-needs.json")))
        chapter_numbers = {c["number"] for c in load_chapters()["chapters"]}
        for category in needs["categories"]:
            self.assertTrue(set(category["chapters"]) <= chapter_numbers, category["id"])
            for source in category["sources"]:
                self.assertIn(source, needs["sources"], category["id"])
            self.assertTrue(category["queries"], category["id"])
        ids = [c["id"] for c in needs["categories"]]
        self.assertEqual(len(ids), len(set(ids)))

    def test_every_script_prints_help(self):
        for script in sorted(glob.glob(os.path.join(ROOT, "scripts", "*.py"))):
            name = os.path.basename(script)
            if name in ("_common.py", "ledger.py", "blender_gltf_to_fbx.py", "check_env.py"):
                continue
            result = subprocess.run([sys.executable, script, "--help"], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, "%s --help failed: %s" % (name, result.stderr))

    def test_csharp_files_have_matching_meta_free_layout(self):
        sources = glob.glob(os.path.join(ROOT, "templates", "unity", "Assets", "Game", "**", "*.cs"), recursive=True)
        self.assertGreater(len(sources), 50)
        for path in sources:
            with open(path, encoding="utf-8") as handle:
                text = handle.read()
            self.assertIn("namespace Farhat", text, path)

    def test_bible_and_project_references_are_complete(self):
        bible = sorted(os.listdir(os.path.join(ROOT, "references", "bible")))
        project = sorted(os.listdir(os.path.join(ROOT, "references", "project")))
        self.assertEqual(len(bible), 19)
        self.assertEqual(len(project), 19)
        total_words = 0
        for folder in ("bible", "project"):
            for name in os.listdir(os.path.join(ROOT, "references", folder)):
                with open(os.path.join(ROOT, "references", folder, name), encoding="utf-8") as handle:
                    total_words += len(handle.read().split())
        self.assertGreater(total_words, 20000)


if __name__ == "__main__":
    unittest.main()
