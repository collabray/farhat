## 10. Performance and platform targets

These are planning budgets for the vertical slice; the slice exists to confirm or change them.

| Area | Planning target | Notes |
| --- | --- | --- |
| Frame rate | 60 fps on minimum PC spec; 30 fps floor on the lowest console | Frame time budget: 16.6 ms at 60 fps, 33.3 ms at 30 |
| Minimum PC spec | Quad-core CPU, 8 GB RAM, a GTX 1050-class GPU, 1080p low to medium | Set from the slice profile |
| Load times | Under 15 seconds to load a chapter, with a visible cutscene or hub covering streaming | Use Addressables and async loading |
| Memory | Keep a fixed per-chapter budget; unload the previous chapter's bundles before the next fully loads | Track in CI |
| Draw batches | Keep within the minimum-spec limit measured in the slice | Use SRP Batcher, GPU instancing, static batching for hubs |
| Characters on screen | Budgeted per chapter by cost tier | Boss chapters carry fewer background actors |
| Streaming | Hubs streamed in cells; set pieces loaded as a single scene | Prevents hitching |
| Quality tiers | Low, Medium, High, with automatic detection and a manual setting | VFX and shadows scale first |
| Build size | Track per chapter; compress textures and audio | Plan episodic downloads |

**Profiling routine.** Profile on the lowest-spec target, never only on the development machine. Run the Unity Profiler and Frame Debugger on every TierA chapter at the greybox stage and again at alpha. Fail a chapter in review if it misses the frame budget with no plan to reach it.
