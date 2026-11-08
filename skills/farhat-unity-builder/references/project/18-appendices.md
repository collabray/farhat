## 18. Appendices

### A. Decision log (initial)

| ID | Decision | Date | Status |
| --- | --- | --- | --- |
| D1 | Engine: Unity 6.3 LTS with URP | 3 Nov 2026 | Proposed; confirm against team skills |
| D2 | Architecture: layered, data-driven, event-driven | 3 Nov 2026 | Proposed |
| D3 | Release model: slice, then episodic | 3 Nov 2026 | Proposed |
| D4 | Platforms: PC first | 3 Nov 2026 | Proposed |
| D5 | No online features at launch | 3 Nov 2026 | Proposed |
| D6 | Story gate blocks unverified chapters in release builds | 3 Nov 2026 | Proposed |

### B. First 30 days checklist

1. Confirm the licence in writing, and the voice and music rights.

2. Choose the engine and pin the LTS version; set up Git with LFS and CI.

3. Create the repository layout and the assembly definitions.

4. Build the Chapter Definition asset and the bible importer; create all 52 stubs.

5. Prototype movement, camera, sword combat and the Shadow.

| 6. Start the episode check for the [I] chapters, beginning with Season 2. 7. Greybox chapter 1, then chapter 5. 8. Set up the Chapter Board, smoke-test bot skeleton and content validation. 9. Pay the Steam Direct fee early so the 30-day wait runs in the background. 10. Run the first internal playtest and record the findings. C. Glossary Term Meaning Addressables Unity's system for loading assets and scenes as streamed bundles Chapter Definition The data asset that describes one chapter Fail-forward Failure that returns the player to a checkpoint, never to a game-over screen LTS Long-term support release of an engine Smoke-test bot An automated player that proves a chapter's critical path exists Story gate A build check that blocks chapters not tagged Verified URP Universal Render Pipeline, Unity's scalable renderer Vertical slice A small, near-final playable part of the game that proves the whole D. Related documents and sources The Game Script and Design Bible: story, characters, systems, all 52 chapters and the source tags. Vendor pages used for the facts in section 4: Unity 6 release information, Unity pricing, Unreal Engine licence and Steam Direct fee. |
| --- |

| Term | Meaning |
| --- | --- |
| Addressables | Unity's system for loading assets and scenes as streamed bundles |
| Chapter Definition | The data asset that describes one chapter |
| Fail-forward | Failure that returns the player to a checkpoint, never to a game-over screen |
| LTS | Long-term support release of an engine |
| Smoke-test bot | An automated player that proves a chapter's critical path exists |
| Story gate | A build check that blocks chapters not tagged Verified |
| URP | Universal Render Pipeline, Unity's scalable renderer |
| Vertical slice | A small, near-final playable part of the game that proves the whole |

### D. Related documents and sources

- The Game Script and Design Bible: story, characters, systems, all 52 chapters and the source tags. In this skill: `references/bible/`.
- Vendor pages used for the facts in section 4: [Unity 6 release information](https://unity.com/cn/unity-6-release), [Unity pricing](https://unity.com/pricing-updates), [Unreal Engine licence](https://www.unrealengine.com/en-US/license) and [Steam Direct fee](https://partner.steamgames.com/doc/gettingstarted/appfee).
