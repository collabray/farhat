## 8. Content pipeline and tooling

### 8.1 Repository layout

```
/Assets
  /Game
    /Core            event bus, services, state machines
    /Systems         combat, shadow, alchemy, decode, vehicles, companion, cover-up
    /Story           chapter manager, objectives, checkpoints, unlocks, dialogue
    /Content
      /Chapters      ChapterDefinition assets (Ch01 to Ch52)
      /Scenes        one folder per hub or set piece
      /Enemies       enemy and boss data
      /Dialogue      Yarn or Ink scripts and string tables
    /Art  /Audio  /VFX  /UI
  /Editor            tools, importers, StoryGate
  /Tests             EditMode and PlayMode tests
/Docs                bible, this document, decision log
/Build               CI scripts
```

### 8.2 Bible-to-data importer

The design bible stays the source of truth for story and chapter design. A small editor tool reads a CSV export of the bible's master overview and chapter tables and creates or updates a ChapterDefinition stub for each of the 52 chapters (number, title, tag, tier, objectives). It never overwrites authored fields such as scenes and cinematics. This keeps the document and the build in sync after every episode check.

### 8.3 Level kits and greybox first

- Greybox every chapter with ProBuilder and primitive characters before any final art; it must be completable end to end.

- Build level kits per region (Emirates, Oxford and England, Mexico, desert, canyon, mine, labyrinth). A kit is a set of modular pieces, materials, props and lighting presets.

- Reuse hubs: the same hub controller runs the palace, Oxford, the Mayan village and the camps.

- Write encounter data in ScriptableObjects so designers can re-tune a boss without a programmer.

### 8.4 Art, animation and audio guidelines

| Area | Starting guideline (tune in the slice) |
| --- | --- |
| Style | Stylised 3D that keeps the series' silhouettes; flat-shaded or lightly toon-shaded materials in URP |
| Characters | One shared humanoid skeleton for Farhat, Tilda, Ben and the cast, with a separate rig for Wings (scorpion) and the Shadow |
| Animation | Retarget a base library; hand-key signature moves; no motion capture unless budget allows |
| Textures | Hand-painted, shared atlases, texture streaming on |
| VFX | VFX Graph for light and darkness, with a quality tier for low-end hardware |
| Audio | FMOD or Wwise with a music state per chapter and an intensity layer for combat |
| Cinematics | Timeline plus Cinemachine, in-engine, no pre-rendered video except an optional intro |

### 8.5 Dialogue and localisation workflow

1. Writers author scenes in Yarn or Ink with tone tags (confident, kind, careful).

2. A build step exports all lines to string tables keyed by chapter prefix.

3. Translators work from the tables; a text-fit test runs in the engine for every language.

4. Right-to-left languages are tested with mirrored UI and subtitle layouts.

5. Voice lines use the same keys, so a recording can be swapped without code changes.

### 8.6 Editor tools worth building early

| Tool | What it does | Payoff |
| --- | --- | --- |
| Chapter Board window | Lists all 52 chapters with tag, tier, stage-gate status and a Play button | One-click playtesting from any chapter |
| Checkpoint jumper | Starts the game at any checkpoint with a saved loadout | Saves hours of re-running |
| Fail simulator | Fires every FailureType on demand | Verifies fail-forward everywhere |
| Story report | Exports chapters grouped by source tag | Feeds the episode check list |
