# Troubleshooting for the build agent

The game-side problems (camera clipping, stuck companions, soft-locks, save corruption, shader and Addressables issues, IL2CPP stripping,
and so on) are in `references/project/11-troubleshooting-guide.md` with causes, fixes and prevention. This file covers problems you hit
while running the pipeline.

| Symptom | Likely cause | What to do |
| --- | --- | --- |
| `unity_cli.py` says "Unity editor not found" | Unity not installed, or installed in a non-standard folder | Ask the human to install Unity 6.3 LTS through Unity Hub, or set `UNITY_EDITOR_PATH` to the editor executable |
| Unity exits at once with "Another Unity instance is running with this project open" | The editor has the project open | Close it, or work through MCP, never run batch mode against an open project |
| Batch mode asks for a licence or exits with a licence error | Unity is not activated on this machine | Ask the human to open Unity Hub and sign in; do not try to activate it yourself |
| `compile` prints no errors but exit is non-zero | A package or asmdef reference failed to resolve | Open `Logs/unity-compile.log`, search for "error" and "Unable to", run `install-packages`, retry |
| `error CS0246 ... Unity.AI.Navigation`, or an asmdef reference that cannot be found | A package is not installed (the scaffolder was run without an editor, or `--skip-packages` was used) | `unity_cli.py install-packages`, then compile again. If the project already fails to compile, temporarily move `Assets/Game` out, run `install-packages` (only `Bootstrap` is needed), then move it back |
| `error CS0246 ... Unity.InputSystem` or missing `InputAction` | Input System package not installed, or Active Input Handling set to the old system | Install the package; set Project Settings > Player > Active Input Handling to the new Input System (or Both) and restart the editor |
| `generate-scenes` fails while creating the NavMesh surface | AI Navigation not installed yet | Install packages first; the generator needs the package to add `NavMeshSurface` |
| Greybox enemies do nothing in play | NavMesh not baked (baker runs at scene start) or enemies not enabled | Check the console for `RuntimeNavMeshBaker`; confirm the ground has a `NavMeshSurface`; bake in the editor if the scene was edited by hand |
| Player cannot move | Input System not set to accept new input, or the Bootstrap scene was not the one entered | Play from `Bootstrap`; check Active Input Handling |
| Chapter will not load: "Scene ... is not in the build settings" | Scenes were not generated or the build list was reset | `generate-scenes` again; it rewrites the build settings |
| Smoke test cannot complete a chapter | A tier-1 objective is not reachable by the bot (hand-edited scene) | The assertion message names the missing goal; fix the scene or extend the bot's path for that chapter |
| `ChapterDataTests` fails after editing data | `data/chapters.json` and the Unity copy differ, or a bible rule is broken | Run `python3 scripts/validate_chapters.py`, copy the file to `Assets/Game/Content/Data/`, re-import chapters |
| `asset_fetch.py` says "Licence ... is not allowed" | The asset's licence is NonCommercial, NoDerivatives, ShareAlike or unknown | Choose another asset. Do not override |
| `asset_fetch.py` says it looks like fan art | The asset name matches a licensed character | Do not download it. Characters are modelled from model sheets |
| Sketchfab download fails | Missing or wrong `SKETCHFAB_API_TOKEN`, or the model is not downloadable | Ask the human for a token; pick another model |
| `license_ledger.py verify` reports "checksum changed" | A recorded file was edited after download | Move your edit into a derived copy and record it as a new entry, or restore the original |
| Git push is rejected for large files | LFS not installed or not tracking the type | Install Git LFS, `git lfs install`, check `.gitattributes`, re-commit |
| Release build fails with "Story gate failed" | Included chapters are not Verified | This is correct. Run the episode check or exclude those chapters from the build |
| C# type-check says `mcs` not found | Mono compiler not installed | Optional. Install `mono-mcs`, or skip and rely on the Unity compile |

## When you are stuck

1. Read the whole error, not the first line.
2. Read `<project>/Logs/unity-<command>.log`.
3. Reproduce with the smallest command (`compile`, then one test).
4. Search the Unity 6 scripting reference for the exact API signature before changing code from memory.
5. If two attempts fail, stop, write down what you tried and the exact output, and ask the human.
