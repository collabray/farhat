## 9. Quality assurance and testing

### 9.1 Test layers

| Layer | What it checks | Tooling | When it runs |
| --- | --- | --- | --- |
| Unit tests (EditMode) | Pure logic: save migration, cooldowns, damage maths, objective rules | Unity Test Framework | Every commit |
| System tests (PlayMode) | A system in a small test scene: fail-forward, checkpoints, Shadow states | Unity Test Framework | Every commit |
| Chapter smoke test | A bot plays each chapter's critical path and reports soft-locks | Custom bot plus CI | Nightly and on every chapter change |
| Fail simulator | Fires every failure type at every checkpoint | Editor tool | Nightly |
| Content validation | Missing references, unlocalised strings, tags, unreachable checkpoints | Editor scripts | Every commit |
| Playtests | Fun, clarity, difficulty | Humans, recorded | Per milestone |
| Compatibility and performance | Hardware matrix, frame time, memory | Devices plus profiler | Per milestone and before release |

### 9.2 The chapter smoke-test bot

The bot is the cheapest guard against soft-locks across 52 chapters. Each chapter declares an ordered list of **critical-path waypoints** (objective positions, interact points, boss arenas). The bot loads the chapter, walks to each waypoint with the NavMesh, triggers the interaction, and fails the run when no objective progress happens for a set time or the player becomes unable to move. It logs the last objective, position, frame time and active states so the developer can reproduce the stall. It does not need to play well; it only needs to prove that the path exists.

### 9.3 Playtest protocol

1. **Audience:** include players aged 8 to 14, fans of the series, and adults who have never seen it.

2. **Format:** observed sessions of 30 to 45 minutes with think-aloud, plus unobserved takes recorded on video.

3. **Questions:** where did you get stuck, what did you think you were meant to do, when were you bored, when did you smile.

4. **Measure:** time per objective, deaths per section, hint use, quit points, completion.

5. **Safeguards:** a parent or guardian present for minors, no personal data collected, recordings stored under the consent given.

6. **Output:** a ranked fix list per chapter, reviewed at each stage gate.

### 9.4 Accessibility and localisation QA

- Test every accessibility option from bible section 8 on every chapter's critical path.

- Subtitles: check line breaks, speaker names and long strings in every language.

- Right-to-left: mirrored UI, correct subtitle alignment and number formatting.

- Colour-blind palettes checked for light and dark states.

### 9.5 Compatibility matrix

Test at least: minimum, recommended and high-end PC configurations (integrated, mid-range and high-end graphics), keyboard and several gamepads, 1080p, 1440p and ultrawide, and each target console when it is added.
