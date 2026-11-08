import json
import os
import subprocess
import sys
import tempfile
import unittest

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, ROOT)

import install  # noqa: E402

SCRIPT = os.path.join(ROOT, "install.py")


def run(*args):
    return subprocess.run([sys.executable, SCRIPT] + list(args), capture_output=True, text=True)


class InstallerTests(unittest.TestCase):
    def test_project_install_copies_the_skill_for_both_clients_and_merges_mcp(self):
        with tempfile.TemporaryDirectory() as tmp:
            home = os.path.join(tmp, "home")
            os.makedirs(home)
            result = run("--project", tmp, "--home", home)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            for relative in (".claude/skills/farhat-unity-builder/SKILL.md", ".agents/skills/farhat-unity-builder/SKILL.md",
                             ".claude/skills/farhat-unity-builder/data/chapters.json", ".agents/skills/farhat-unity-builder/scripts/unity_cli.py",
                             ".mcp.json", ".agents/mcp_config.json"):
                self.assertTrue(os.path.exists(os.path.join(tmp, relative)), relative)
            with open(os.path.join(tmp, ".mcp.json")) as handle:
                servers = json.load(handle)["mcpServers"]
            self.assertEqual(servers["unityMCP"]["type"], "http")
            self.assertEqual(servers["blender"]["command"], "uvx")
            with open(os.path.join(tmp, ".agents", "mcp_config.json")) as handle:
                self.assertIn("serverUrl", json.load(handle)["mcpServers"]["unityMCP"])
            self.assertFalse(os.path.exists(os.path.join(tmp, ".claude", "skills", "farhat-unity-builder", "tests", "__pycache__")))

    def test_existing_mcp_servers_are_kept_and_backed_up(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = os.path.join(tmp, ".mcp.json")
            with open(path, "w") as handle:
                json.dump({"mcpServers": {"mine": {"command": "x"}, "unityMCP": {"type": "http", "url": "http://localhost:9999/mcp"}}}, handle)
            result = run("--project", tmp, "--client", "claude", "--home", tmp)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            with open(path) as handle:
                servers = json.load(handle)["mcpServers"]
            self.assertEqual(servers["mine"]["command"], "x")
            self.assertEqual(servers["unityMCP"]["url"], "http://localhost:9999/mcp", "an existing server of the same name is kept")
            self.assertIn("blender", servers)
            self.assertTrue(os.path.exists(path + ".bak"))
            run("--project", tmp, "--client", "claude", "--force-mcp", "--home", tmp)
            with open(path) as handle:
                self.assertEqual(json.load(handle)["mcpServers"]["unityMCP"]["url"], "http://localhost:8080/mcp")

    def test_dry_run_changes_nothing(self):
        with tempfile.TemporaryDirectory() as tmp:
            result = run("--project", tmp, "--dry-run", "--home", tmp)
            self.assertEqual(result.returncode, 0)
            self.assertIn("[dry-run]", result.stdout)
            self.assertEqual(os.listdir(tmp), [])

    def test_user_install_and_uninstall(self):
        with tempfile.TemporaryDirectory() as tmp:
            home = os.path.join(tmp, "home")
            os.makedirs(os.path.join(home, ".gemini", "antigravity"))
            result = run("--project", tmp, "--target", "user", "--home", home)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertTrue(os.path.exists(os.path.join(home, ".claude", "skills", "farhat-unity-builder", "SKILL.md")))
            self.assertTrue(os.path.exists(os.path.join(home, ".gemini", "antigravity", "skills", "farhat-unity-builder", "SKILL.md")))
            result = run("--project", tmp, "--target", "user", "--home", home, "--uninstall")
            self.assertEqual(result.returncode, 0)
            self.assertFalse(os.path.exists(os.path.join(home, ".claude", "skills", "farhat-unity-builder")))

    def test_reinstall_replaces_the_old_copy(self):
        with tempfile.TemporaryDirectory() as tmp:
            run("--project", tmp, "--client", "claude", "--no-mcp", "--home", tmp)
            stale = os.path.join(tmp, ".claude", "skills", "farhat-unity-builder", "stale.txt")
            with open(stale, "w") as handle:
                handle.write("old")
            run("--project", tmp, "--client", "claude", "--no-mcp", "--home", tmp)
            self.assertFalse(os.path.exists(stale))

    def test_antigravity_user_folder_selection(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertTrue(install.antigravity_user_skills_dir(tmp).endswith(os.path.join(".gemini", "config", "skills")))
            os.makedirs(os.path.join(tmp, ".gemini", "antigravity"))
            self.assertTrue(install.antigravity_user_skills_dir(tmp).endswith(os.path.join(".gemini", "antigravity", "skills")))

    def test_skill_frontmatter_is_valid(self):
        with open(os.path.join(ROOT, "skills", "farhat-unity-builder", "SKILL.md"), encoding="utf-8") as handle:
            text = handle.read()
        self.assertTrue(text.startswith("---\n"))
        front = text.split("---\n")[1]
        self.assertIn("name: farhat-unity-builder", front)
        description = [l for l in front.splitlines() if l.startswith("description:")][0]
        self.assertGreater(len(description), 200)
        self.assertLess(len(description), 1024 + 20)


if __name__ == "__main__":
    unittest.main()
