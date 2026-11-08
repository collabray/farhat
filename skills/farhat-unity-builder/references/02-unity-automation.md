# Unity automation

## 1. Two ways to drive Unity

| Way | Use for | Limits |
| --- | --- | --- |
| `scripts/unity_cli.py` (batch mode) | Anything repeatable and anything CI also runs: compile, packages, import, scene generation, validation, tests, builds | Unity cannot open a project that is already open in another editor. Close the editor first or use MCP |
| Unity MCP (a live editor) | Looking at and editing a scene, creating prefabs, reading the console, entering Play mode, quick experiments | Not repeatable by itself. Anything you decide in the editor must end up in committed assets, scripts or the generator |

Rule of thumb: explore with MCP, then make it permanent in code, data or the `SceneGenerator`, then prove it with `unity_cli.py`.

## 2. Commands

All commands accept `--dry-run` to print the exact Unity invocation. `--unity <path>` or `UNITY_EDITOR_PATH` selects the editor; the
highest 6000.3.x version in the Unity Hub folders is chosen automatically.

| Command | What it does | Exit codes |
| --- | --- | --- |
| `create-project` | Creates an empty project (the scaffolder does this for you) | 0 ok, 1 failed, 2 no editor |
| `install-packages` | Runs `PackageInstaller.InstallAll` (in the standalone `Bootstrap` assembly, so it works before the game code compiles), adding each needed package by name so Unity picks the version verified for the editor. The scaffolder runs it for you | 0 ok, 1 failed |
| `compile` | Opens the project, imports, compiles; prints each `error CSxxxx` with file and line | 0 clean, 1 errors |
| `import-chapters` | `ChapterCatalogImporter.ImportAll` creates the 52 `ChapterDefinition` assets and the catalog from `chapters.json` | |
| `generate-scenes` | `SceneGenerator.GenerateAllCli` builds the Player prefab, enemies, recipes, Bootstrap and 52 chapter scenes, and sets the build scene list | |
| `validate [--require-scenes]` | `ContentValidator.RunCli` checks ids, objectives, checkpoints, rules, fragments, scenes | 0 valid, 1 errors |
| `test --mode editmode\|playmode` | Runs the Unity Test Framework, writes `Logs/results-*.xml`, prints failures | 0 all passed, 1 failures |
| `build --target win64\|linux64\|macos [--release]` | Runs `BuildScript`; `--release` defines `RELEASE_BUILD` and activates the story gate | 0 ok, 1 failed |
| `run-method --method X` | Runs any static editor method | |

Logs are written to `<project>/Logs/unity-<command>.log`. Read the log, not only the summary, when something fails without a clear error.

## 3. The compile-fix loop

1. `unity_cli.py compile --project P`.
2. Read the listed errors. They are real Unity compiler messages with file and line.
3. Fix the cause. Common causes: an API that changed in the editor version (check the Unity 6 scripting reference for the real signature), a missing assembly reference in an `.asmdef`, a missing package.
4. Repeat until `OK`. Do not suppress warnings-as-errors or exclude files to get green.
5. When the fix was in a file that came from `templates/`, make the same fix in the skill's `templates/` copy and note it in your report.
6. Run `test --mode editmode`, then `playmode`.

Assembly references: `Farhat.Runtime` references `Unity.InputSystem` and `Unity.AI.Navigation`. If Unity reports that an assembly
reference cannot be found, install the matching package first (`install-packages`) and re-run `compile`.

## 4. Working through MCP

1. List the tools your client exposes from the Unity server. Names differ between versions; never assume.
2. Before editing: read the console and the active scene name. After editing: read the console again. Treat red errors as blocking.
3. Scene work: open the chapter scene by its generated name (`Chapter Board` in the Farhat menu lists them), change objects, **save the scene**, then confirm with `git status` that the change is a real file change.
4. Keep the contract with the data: trigger components reference objective ids from `chapters.json`. If you rename or add an objective, change `data/chapters.json`, copy it to the project, re-import chapters, then fix the scene.
5. To build something once for many chapters, add it to `SceneGenerator` or `PrefabBuilders` rather than repeating MCP clicks 52 times.
6. Enter Play mode from `Bootstrap`, not from a chapter scene. A chapter scene on its own has no player or services.

## 5. Where Unity batch mode bites

- First open of a project can take many minutes; do not assume a hang until the log stops moving.
- `-quit` must not be used with `-runTests`; the test runner exits by itself. `unity_cli.py` handles this.
- PlayMode tests need a graphics device, so `-nographics` is only used for EditMode.
- Package installs resolve asynchronously; `PackageInstaller` waits for each request with a timeout.
- Exit code 0 from Unity does not prove success. Always read the parsed summary.
- `ProjectSetup` switches Active Input Handling to the new Input System; that takes effect on the next editor start, so the first PlayMode run after `generate-scenes` must be a fresh batch command (it is, because each command starts the editor).
