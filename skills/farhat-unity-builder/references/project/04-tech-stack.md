## 4. Tech stack

Versions and licence terms change; confirm each at project start and record the result in the decision log (appendix).

| Layer | Choice | Why | Alternatives |
| --- | --- | --- | --- |
| Engine | Unity 6 (LTS), URP | Stylised look, broad hardware, strong tooling | Unreal Engine 5, Godot 4 (smaller ecosystem for console) |
| Language | C# | Fast iteration, safe, good tooling | C++ and Blueprints (Unreal) |
| Rendering | Universal Render Pipeline, Shader Graph, VFX Graph | Good performance on low-end hardware | HDRP (heavier, not needed) |
| Camera and cutscenes | Cinemachine, Timeline | Authored cameras and set pieces without custom code | Custom camera rigs |
| Input | Unity Input System | Rebinding and gamepad support built in | Rewired |
| Characters and animation | Animator, Animation Rigging, IK | Companion and creature rigs | Third-party animation sets |
| Navigation and AI | AI Navigation (NavMesh) plus a behaviour-tree or utility-AI layer | Companions and enemies | A* Pathfinding Project |
| Level greybox | ProBuilder | Fast playable blockouts | Blender blockouts |
| Content streaming | Addressables | Chapters load and unload as bundles | Scene additive loading only |
| Dialogue | Yarn Spinner or Ink | Writer-friendly scripts, localisation hooks | Custom dialogue graph |
| Audio | FMOD Studio or Wwise | Adaptive music and mixing | Unity audio only |
| UI | UI Toolkit or uGUI | HUD, menus, subtitles | Third-party UI kits |
| Localisation | Unity Localization package | String tables, right-to-left support | Custom CSV pipeline |
| Art tools | Blender, Krita, Substance (optional) | Free and widely known | Maya, ZBrush |
| Version control | Git with Git LFS, or Perforce for larger teams | Large binary assets need LFS or Perforce | Unity Version Control |
| CI/CD | GitHub Actions or GitLab CI with GameCI Unity actions | Automated builds and tests | Jenkins, TeamCity |
| Testing | Unity Test Framework (EditMode and PlayMode) plus a custom chapter smoke-test bot | Catch soft-locks early | Manual only |
| Crash and diagnostics | Sentry or Unity diagnostics, opt-in | Stability tracking without collecting child data | None |
| Project management | Jira, Linear or Notion | Chapter board with stage gates | Trello |
| Backend | None (single-player). Platform cloud saves if offered | No servers to run | Custom backend only if online features are added later |

**Verified version and licence facts, as of 3 November 2026.** Re-check these on the vendors' pages at project start.

| Item | What the vendor states | Consequence for this project |
| --- | --- | --- |
| Unity version | Unity 6.3 LTS was released in December 2025 with two years of LTS support, to December 2027. Unity 6.0 LTS support ends in October 2026 | Start on 6.3 LTS and stay on it; do not start on 6.0 |
| Unity cost | Unity Personal is free for organisations with under $200,000 in revenue and funding over the last 12 months; Unity 6 makes the splash screen optional and Unity has no runtime fee. Unity Pro is $2,310 per seat per year in 2026 | Personal covers development and the slice; plan for Pro once revenue or funding passes the limit |
| Unity consoles | Unity's plan page lists publishing to game consoles as a Pro benefit | Budget Pro seats before any console port, and confirm terms with Unity and each platform holder |
| Unreal cost | Unreal Engine is free below $1 million in lifetime gross revenue per product, with a 5% royalty above that | Same cost logic for the Unreal alternative |
| Unreal Epic Store option | A reduced 3.5% royalty applies to games that launch on the Epic Games Store first or at the same time, per Epic's programme from 1 January 2025 | Verify the current programme before relying on it |
| Steam | Valve's Steam Direct fee is $100 per app, recoupable in the payment after the product reaches $1,000 in adjusted gross revenue; the fee must be paid 30 days before release | Pay it early; a 30-day wait cannot be skipped |
