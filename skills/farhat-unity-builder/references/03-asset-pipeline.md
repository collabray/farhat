# Asset pipeline

## 1. Principles

1. Everything third-party enters through `scripts/asset_fetch.py` and is recorded in `Assets/ThirdParty/ledger.json`. An asset with no ledger entry does not ship.
2. Only CC0, CC BY and (fonts) SIL OFL. Refuse NonCommercial, NoDerivatives, ShareAlike, Editorial, "Standard" store licences and unknown licences (`data/allowed-licenses.json`).
3. Never download models of the licensed characters. They are modelled from the rights holder's model sheets (bible section 14). The stand-in rigs in `data/asset-needs.json` keep the game playable until then.
4. Keep to the art direction: stylised 3D, flat or lightly toon-shaded URP materials, shared texture atlases, region palettes.
5. Respect budgets: `references/project/10-performance-and-platform-targets.md`. Reject an asset instead of importing something that blows the chapter budget.

## 2. Finding what to get

`data/asset-needs.json` lists 43 categories (HDRIs, textures, models, stand-in characters, animation, audio, UI, fonts) with the chapters that
use them, search words and the sources to try in order. Start with the chapters in the current phase (the slice uses chapters 1, 5 and 7).

```
python3 scripts/asset_fetch.py polyhaven search --type hdris --query desert
python3 scripts/asset_fetch.py polyhaven download --type hdris --id <id> --res 2k --format hdr --out <GAME>/Assets/ThirdParty/polyhaven --base <GAME> --ledger <GAME>/Assets/ThirdParty/ledger.json
python3 scripts/asset_fetch.py polyhaven download --type models --id <id> --res 1k --format gltf --out <GAME>/Assets/ThirdParty/polyhaven --base <GAME> --ledger <GAME>/Assets/ThirdParty/ledger.json
python3 scripts/asset_fetch.py sketchfab search --query "ruined wall" --license cc0,by
python3 scripts/asset_fetch.py url download --url <direct zip link> --name "<pack name>" --author "<author>" --license CC0 --source-url <page url> --out <GAME>/Assets/ThirdParty/packs --base <GAME> --ledger <GAME>/Assets/ThirdParty/ledger.json
```

Global options (`--ledger`, `--base`) go before the source name. For Kenney, Quaternius, OpenGameArt, Freesound and Google Fonts there is
no API call here: open the pack page, read the licence on the page, and use `url download` with the exact licence shown. If the page
shows a licence that is not allowed, pick another asset.

Poly Haven asks API users for a unique User-Agent and a credit to Poly Haven; the script sends a descriptive User-Agent and the
generated `CREDITS.md` contains the credit line. Re-read their API terms when you start, since the terms are theirs to change.

## 3. Importing into Unity

| Asset | How |
| --- | --- |
| FBX | Imports natively. Set Scale Factor so a human is about 1.8 units, enable Read/Write off, generate colliders only where needed |
| glTF / GLB | Unity does not import glTF on its own. Either install the glTFast package (check the exact package id in Package Manager before adding it) or convert with Blender: `blender --background --python scripts/blender_gltf_to_fbx.py -- in.glb out.fbx` |
| HDRI | Import as Texture (Default, 2D, high quality compression off for the skybox), build a Skybox/Panoramic material, assign to the chapter's lighting settings |
| PBR textures | Albedo sRGB on; normal maps set to Normal Map; roughness or metallic maps sRGB off; pack into the URP Lit mask map or use separate maps |
| Materials | Convert to URP Lit with the Render Pipeline Converter, then switch hero assets to the toon or flat shader used by the project |
| Audio | Music: Vorbis, streaming, stereo. Ambience: Vorbis, loop, decompress on load only if short. Effects: Vorbis or ADPCM, mono for 3D sounds |
| Fonts | TTF or OTF, include the OFL text in `Assets/ThirdParty/fonts/` |

Always import into `Assets/ThirdParty/<source>/<asset>/`, then make a prefab for the game in `Assets/Game/Art/` that references it. The
game's prefabs must not live inside `ThirdParty` so a re-download never overwrites your work.

## 4. Characters and animation

- Stand-ins: a CC0 rigged humanoid for human characters and a creature rig for Wings and the spider. Set the Animation Type to Humanoid, retarget a locomotion and combat set (idle, walk, run, jump, roll, sword attack, hit reaction, parry).
- Proportions follow the bible: Farhat 16, Tilda 15; silhouettes (hairband, koala laptop, scorpion body, butler suit) are added as separate meshes so the licensed model can replace the body later.
- Never ship a stand-in as the final character. `progress.py` will not let a chapter's `art` stage close while a stand-in is the only model for a named character; note it in the chapter doc.

## 5. Budgets and checks

- Per-chapter budgets come from the slice profile. Start from the planning targets in `references/project/10-*.md` and write the measured values into `Docs/budgets.md`.
- Use LODs for anything repeated, GPU instancing for props, texture atlases for kits, and static batching for hubs.
- Run `license_ledger.py verify` after every import and before every commit; a checksum change means a file was modified after it was recorded (record the modification as a new derived entry rather than editing the original).
- Generate `CREDITS.md` with `license_ledger.py credits` and keep it in the game; credits must be shown in the credits screen.
