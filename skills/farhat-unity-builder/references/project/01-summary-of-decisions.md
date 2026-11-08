## 1. Summary of decisions

| Decision | Choice | Why |
| --- | --- | --- |
| Game type | Single-player 3D third-person action-adventure | Matches the bible; no online infrastructure to build or secure |
| Engine | Unity 6 (LTS), C#, URP | Small-team productivity, broad hardware reach, large ecosystem. Unreal Engine 5 is a valid alternative if the team already has Unreal skills |
| Architecture | Layered, data-driven, event-driven | 52 chapters are authored as data on top of a small set of reusable systems |
| Chapter authoring | One Chapter Definition asset per episode | Corrections from the episode check change data, not code |
| Release model | Vertical slice, then episodic drops | Controls cost and tests the market before the full commitment |
| Platforms | PC first (Steam), consoles second | Lowest certification cost first; consoles after the slice proves out |
| Online features | None at launch | No child-data exposure, no server cost, simpler ratings |
| Story safety net | A build check that blocks release chapters still tagged [I] | Keeps unverified story out of shipped builds |

**Engine choice, in one paragraph.** Both engines can ship this game. Unity is the default here because the project is content-heavy rather than graphics-limited, C# is faster to iterate than C++, the target audience includes modest hardware, and hiring or training is easier. Choose Unreal instead when the team already works in it, wants its high-fidelity rendering out of the box, or plans a console-first release. Everything below transfers to either engine; only the code samples and tool names change.
