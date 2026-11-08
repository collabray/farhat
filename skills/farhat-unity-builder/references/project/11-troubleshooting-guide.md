## 11. Troubleshooting guide

### 11.1 Diagnostic playbook

1. **Reproduce:** use the Checkpoint jumper to land at the chapter and checkpoint; the Fail simulator to force the failure.

2. **Read the log:** every system logs under a tag (Chapter, Objective, Checkpoint, Shadow, Companion, Save). Filter by tag.

3. **Isolate:** load the system's test scene and reproduce with one system only.

4. **Bisect:** if it appeared recently, bisect commits using CI builds.

5. **Fix and guard:** add a test or a content-validation rule so the same fault cannot return.

### 11.2 Common problems

| Problem | Likely cause | Fix | Prevention |
| --- | --- | --- | --- |
| Player cannot progress, objective never completes | An event is published before its subscriber exists, or a trigger volume was removed | Check the Objective log; publish the completion from a checkpoint reload; confirm trigger references | Smoke-test bot; content validation for objectives with no source |
| Companion stuck or lost | NavMesh gaps, dynamic obstacles blocking, vehicle transitions | Re-bake the NavMesh with correct agent radius; add off-mesh links; teleport the companion after a stall timer | Companion stall timer and a teleport-to-player fallback |
| Camera clips through walls or jitters | Cinemachine collision settings, a conflicting camera priority, moving platform physics | Enable the collider extension and tune radius; set camera priorities explicitly; use LateUpdate or the Cinemachine update method matched to physics | Camera test scene with extreme angles |
| Player falls through the floor or tunnels (quicksand, fast chases) | Fast movement with discrete collision, thin colliders | Use continuous collision detection, thicker colliders, a kill-plane that restores to the last safe point | Safe-point recorder and kill-plane in every level |
| Checkpoint restore leaves a broken state | Restore does not reset every system (enemies, timers, Shadow state) | Make the restore routine data-driven: every system implements a Snapshot and Restore | Test that restore equals the saved snapshot |
| Save file corrupted or fails to load | Crash during write, version mismatch | Write to a temp file then rename; keep a backup; run SaveMigrator; offer to restore the backup | Version field and migration tests for every released version |
| Static event handlers fire twice or in the wrong scene | Domain reload disabled; handlers not unsubscribed | Unsubscribe in OnDisable; clear the bus on scene change | Call EventBus Clear in tests; lint for missing unsubscribes |
| Frame-rate spikes | Garbage collection, instantiating at runtime, shader compilation | Pool objects, avoid per-frame allocations, pre-warm shaders and variants | Allocation checks in profiler CI runs |
| Pink or missing materials in the build | Shader variants stripped or an editor-only shader | Add the shader to the always-included list or a shader variant collection | Build and run a smoke scene on every CI build |
| Scene or asset missing in the player build | Addressables group not built, or a scene not marked addressable | Rebuild Addressables before the player build; verify group settings | CI always builds content before the player |
| IL2CPP build errors or crashes on release builds only | Code stripping removes reflection-used types | Add a link.xml with the needed types or preserve attributes | Run release builds in CI, not only editor tests |
| Memory grows across chapters | Bundles or textures not released, event handlers holding references | Release Addressables handles; unload unused assets after each chapter | Memory snapshot comparison between chapter 1 and chapter 10 |
| Controls stop working after pause or a cutscene | Input action maps not restored | Centralise input map switching in one service | Test pause and unpause in every chapter |
| Subtitle or UI text overflows in some languages | Fixed-width layouts | Use auto-size and layout groups; test the longest language | Locale check at the stage gate |
| Cutscene audio out of sync | Timeline and audio clock drift, slow load | Use Timeline-driven audio events; preload audio for cutscenes | Cutscene test in the slowest target |
| Boss feels unfair | Telegraphs too short, hidden hit boxes | Lengthen telegraphs; show hit zones; add the Story difficulty assist | Playtest with children and with new players |
| Git repository becomes slow or merges break | Large binaries without LFS, scene and prefab merge conflicts | Use Git LFS or Perforce; enable Smart Merge; split big scenes into additive scenes | Scene ownership rules; lock files for binary assets |
| Steam upload or depot errors | Wrong depot config, missing launch option | Re-check app and depot ids and the launch options; use the SteamPipe tool log | Dry-run uploads to a private test branch |

### 11.3 Support triage for released builds

| Severity | Meaning | Response |
| --- | --- | --- |
| S1 | Crash on start, save loss, cannot finish a chapter | Hotfix within days; communicate on the store page |
| S2 | Soft-lock with a workaround, serious performance fault | Patch in the next scheduled build |
| S3 | Visual, audio or text faults | Batch into regular patches |
| S4 | Suggestions | Backlog |
