import os
import subprocess
import sys
import tempfile
import unittest

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, os.path.join(ROOT, "scripts"))

import unity_cli  # noqa: E402
import check_env  # noqa: E402

LOG = """Compilation started
Assets/Game/Runtime/Systems/PlayerController.cs(42,17): error CS0103: The name 'cam' does not exist in the current context
Assets/Game/Runtime/Systems/PlayerController.cs(42,17): error CS0103: The name 'cam' does not exist in the current context
Assets/Game/Runtime/Story/ChapterRunner.cs(10,5): error CS0246: The type or namespace name 'Foo' could not be found
Assets/Game/Runtime/Core/EventBus.cs(3,1): warning CS0105: The using directive appeared previously
Scripts have compiler errors.
"""

RESULTS = """<?xml version="1.0" encoding="utf-8"?>
<test-run id="2" testcasecount="3" result="Failed" total="3" passed="2" failed="1" inconclusive="0" skipped="0">
  <test-suite type="Assembly" name="Farhat.Tests.EditMode">
    <test-case id="1" name="A" fullname="Farhat.Tests.CoreTests.A" result="Passed"/>
    <test-case id="2" name="B" fullname="Farhat.Tests.CoreTests.B" result="Passed"/>
    <test-case id="3" name="C" fullname="Farhat.Tests.CoreTests.C" result="Failed">
      <failure><message><![CDATA[  Expected: 5 But was: 4  ]]></message></failure>
    </test-case>
  </test-suite>
</test-run>
"""


class UnityCliTests(unittest.TestCase):
    def test_compiler_errors_are_parsed_and_deduplicated(self):
        errors, _ = unity_cli.parse_log(LOG)
        self.assertEqual(len(errors), 2)
        self.assertEqual(errors[0]["code"], "CS0103")
        self.assertEqual(errors[0]["line"], 42)
        self.assertTrue(errors[0]["file"].endswith("PlayerController.cs"))

    def test_test_results_are_summarised(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = os.path.join(tmp, "r.xml")
            with open(path, "w") as handle:
                handle.write(RESULTS)
            summary = unity_cli.parse_test_results(path)
            self.assertEqual((summary["total"], summary["passed"], summary["failed"]), (3, 2, 1))
            self.assertEqual(summary["failures"][0]["name"], "Farhat.Tests.CoreTests.C")
            self.assertIn("Expected: 5", summary["failures"][0]["message"])

    def test_build_command_shape(self):
        command = unity_cli.build_command("/u/Unity", "/p", "Farhat.EditorTools.BuildScript.BuildWindows64", ["-releaseBuild"], "/p/log.txt")
        self.assertEqual(command[:4], ["/u/Unity", "-batchmode", "-nographics", "-quit"])
        self.assertIn("-executeMethod", command)
        self.assertEqual(command[-1], "-releaseBuild")

    def test_version_preference_picks_6000_3(self):
        paths = ["/h/Editor/6000.0.58f2/Editor/Unity", "/h/Editor/6000.3.2f1/Editor/Unity", "/h/Editor/6000.4.0f1/Editor/Unity"]
        self.assertEqual(sorted(paths, key=unity_cli.version_key)[-1], paths[1])

    def test_explicit_editor_path_wins(self):
        with tempfile.NamedTemporaryFile() as handle:
            self.assertEqual(unity_cli.find_unity(handle.name), handle.name)

    def test_dry_run_prints_the_command_without_unity(self):
        script = os.path.join(ROOT, "scripts", "unity_cli.py")
        for command, expect in (("compile", "-quit"), ("generate-scenes", "SceneGenerator.GenerateAllCli"), ("validate", "ContentValidator.RunCli"), ("install-packages", "PackageInstaller.InstallAll")):
            result = subprocess.run([sys.executable, script, command, "--project", "/tmp/x", "--dry-run"], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn(expect, result.stdout)
        result = subprocess.run([sys.executable, script, "build", "--project", "/tmp/x", "--target", "linux64", "--release", "--dry-run"], capture_output=True, text=True)
        self.assertIn("BuildLinux64", result.stdout)
        self.assertIn("-releaseBuild", result.stdout)


class EnvTests(unittest.TestCase):
    def test_collect_reports_required_tools(self):
        items = check_env.collect()
        names = [i["name"] for i in items]
        self.assertIn("Unity editor", names)
        python_item = [i for i in items if i["name"] == "Python 3.9+"][0]
        self.assertTrue(python_item["ok"])
        self.assertTrue(all(i["fix"] for i in items))


class ScaffoldTests(unittest.TestCase):
    def test_scaffold_into_an_existing_unity_folder(self):
        with tempfile.TemporaryDirectory() as tmp:
            os.makedirs(os.path.join(tmp, "ProjectSettings"))
            with open(os.path.join(tmp, "ProjectSettings", "ProjectVersion.txt"), "w") as handle:
                handle.write("m_EditorVersion: 6000.3.0f1\n")
            script = os.path.join(ROOT, "scripts", "scaffold_project.py")
            result = subprocess.run([sys.executable, script, "--project", tmp, "--with-docs"], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            for relative in ("Assets/Game/Runtime/Story/ChapterRunner.cs", "Assets/Game/Editor/SceneGenerator.cs", "Assets/Game/Content/Data/chapters.json",
                             ".gitignore", ".gitattributes", ".github/workflows/unity-ci.yml", ".farhat/progress.json", "Docs/chapters.json", "Docs/bible/00-index.md"):
                self.assertTrue(os.path.exists(os.path.join(tmp, relative)), relative)
            marker = os.path.join(tmp, "Assets", "Game", "Runtime", "Core", "EventBus.cs")
            with open(marker, "w") as handle:
                handle.write("// edited by a developer\n")
            subprocess.run([sys.executable, script, "--project", tmp], capture_output=True, text=True)
            with open(marker) as handle:
                self.assertIn("edited by a developer", handle.read(), "existing files must be kept without --force")

    @unittest.skipIf(os.name == "nt", "uses a shell script as a fake editor")
    def test_packages_are_installed_before_the_game_code_is_copied(self):
        with tempfile.TemporaryDirectory() as tmp:
            project = os.path.join(tmp, "game")
            os.makedirs(os.path.join(project, "ProjectSettings"))
            with open(os.path.join(project, "ProjectSettings", "ProjectVersion.txt"), "w") as handle:
                handle.write("m_EditorVersion: 6000.3.0f1\n")
            log = os.path.join(tmp, "fake.log")
            fake = os.path.join(tmp, "fake-unity")
            with open(fake, "w") as handle:
                handle.write("#!/bin/sh\n"
                             "echo \"$@\" >> \"%s\"\n"
                             "while [ $# -gt 0 ]; do if [ \"$1\" = \"-projectPath\" ]; then P=\"$2\"; fi; shift; done\n"
                             "if [ -d \"$P/Assets/Game/Runtime\" ]; then echo RUNTIME_PRESENT >> \"%s\"; fi\n"
                             "if [ -f \"$P/Assets/Game/Bootstrap/PackageInstaller.cs\" ]; then echo BOOTSTRAP_PRESENT >> \"%s\"; fi\n"
                             "exit 0\n" % (log, log, log))
            os.chmod(fake, 0o755)
            script = os.path.join(ROOT, "scripts", "scaffold_project.py")
            result = subprocess.run([sys.executable, script, "--project", project, "--unity", fake], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            with open(log) as handle:
                text = handle.read()
            self.assertIn("Farhat.EditorTools.PackageInstaller.InstallAll", text)
            self.assertIn("BOOTSTRAP_PRESENT", text)
            self.assertNotIn("RUNTIME_PRESENT", text, "game code must not exist yet when the packages are installed")
            self.assertTrue(os.path.exists(os.path.join(project, "Assets", "Game", "Runtime", "Story", "ChapterRunner.cs")))

    def test_scaffold_fails_clearly_without_unity(self):
        with tempfile.TemporaryDirectory() as tmp:
            env = dict(os.environ)
            env["UNITY_EDITOR_PATH"] = ""
            env["HOME"] = tmp
            script = os.path.join(ROOT, "scripts", "scaffold_project.py")
            result = subprocess.run([sys.executable, script, "--project", os.path.join(tmp, "g")], capture_output=True, text=True, env=env)
            if unity_cli.find_unity() is None:
                self.assertEqual(result.returncode, 2)
                self.assertIn("Unity editor not found", result.stdout)


if __name__ == "__main__":
    unittest.main()
