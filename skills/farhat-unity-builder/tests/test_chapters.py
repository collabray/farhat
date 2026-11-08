import copy
import os
import subprocess
import sys
import tempfile
import unittest

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, os.path.join(ROOT, "scripts"))

import validate_chapters  # noqa: E402
import progress  # noqa: E402
from _common import load_chapters, load_json  # noqa: E402


class ChapterDataTests(unittest.TestCase):
    def setUp(self):
        self.data = load_chapters()

    def test_shipped_data_is_valid(self):
        self.assertEqual(validate_chapters.validate(self.data), [])

    def test_counts(self):
        chapters = self.data["chapters"]
        self.assertEqual(len(chapters), 52)
        self.assertEqual(sum(1 for c in chapters if c["sourceTag"] == "Verified"), 21)
        self.assertEqual(sum(1 for c in chapters if c["season"] == 1), 26)

    def test_validator_catches_a_wrong_fragment_placement(self):
        bad = copy.deepcopy(self.data)
        chapter18 = bad["chapters"][17]
        chapter18["unlocks"] = [u for u in chapter18["unlocks"] if u != "fragment.5"]
        errors = validate_chapters.validate(bad)
        self.assertTrue(any("fragment.5" in e for e in errors))

    def test_validator_catches_an_unverified_chapter_marked_verified(self):
        bad = copy.deepcopy(self.data)
        bad["chapters"][3]["sourceTag"] = "Verified"  # chapter 4 is title-only in the bible
        errors = validate_chapters.validate(bad)
        self.assertTrue(any("Verified chapters" in e for e in errors))

    def test_validator_catches_a_rule_pointing_at_an_undeclared_checkpoint(self):
        bad = copy.deepcopy(self.data)
        bad["chapters"][1]["failureRules"][0]["checkpointId"] = "nowhere"
        errors = validate_chapters.validate(bad)
        self.assertTrue(any("undeclared checkpoint" in e for e in errors))

    def test_every_chapter_spec_has_the_bible_text(self):
        for c in self.data["chapters"]:
            self.assertTrue(len(c["spec"]["storyBeats"]) > 20, c["id"])
            if c["number"] <= 26:
                for key in ("levelAndObjectives", "mechanicsEnemiesBoss", "failStates", "cutscenesAndUnlocks"):
                    self.assertTrue(c["spec"].get(key), "%s missing %s" % (c["id"], key))
            else:
                for key in ("design", "failAndUnlocks"):
                    self.assertTrue(c["spec"].get(key), "%s missing %s" % (c["id"], key))

    def test_unity_copy_of_the_data_matches(self):
        copy_path = os.path.join(ROOT, "templates", "unity", "Assets", "Game", "Content", "Data", "chapters.json")
        self.assertEqual(load_json(copy_path), self.data)

    def test_cli_exit_codes(self):
        script = os.path.join(ROOT, "scripts", "validate_chapters.py")
        self.assertEqual(subprocess.run([sys.executable, script], capture_output=True).returncode, 0)


class ProgressTests(unittest.TestCase):
    def test_flow(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = os.path.join(tmp, "progress.json")
            data = progress.init(path)
            self.assertEqual(len(data["chapters"]), 52)
            self.assertIn("Pre-production", progress.next_item(data))
            data["phase"] = "slice"
            self.assertIn("Chapter 1", progress.next_item(data))
            data["chapters"]["1"]["stage"] = "lock"
            self.assertIn("Chapter 5", progress.next_item(data))
            with self.assertRaises(SystemExit):
                progress.init(path)

    def test_cannot_lock_an_unverified_chapter(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = os.path.join(tmp, "progress.json")
            progress.init(path)
            script = os.path.join(ROOT, "scripts", "progress.py")
            result = subprocess.run([sys.executable, script, "--file", path, "set", "4", "lock"], capture_output=True, text=True)
            self.assertEqual(result.returncode, 1)
            self.assertIn("not Verified", result.stdout)
            result = subprocess.run([sys.executable, script, "--file", path, "set", "5", "lock"], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0)


if __name__ == "__main__":
    unittest.main()
