# Tidebound — Unity 3D fishing prototype

An original, single-player fishing adventure inspired by the broad appeal of exploration, collection and skill-based fishing. All names, world layouts, mechanics implementation and primitive art in this project are original. No Fisch or Roblox assets are included.

## Open and play

1. Install Unity Hub and Unity **6.0 LTS (6000.0)**. This project targets the built-in render pipeline. Use the editor version in ProjectSettings/ProjectVersion.txt, or a newer Unity 6.0 patch.
2. Extract this archive. In Unity Hub select **Add → Add project from disk** and choose the **Tidebound** folder containing Assets, Packages and ProjectSettings.
3. Allow Unity to import the project. The editor script creates and opens `Assets/Scenes/Tidebound.unity` automatically. If needed, select **Tidebound → Create or open game scene**.
4. Press **Play**, click the Game view, and walk forward to the dock's end. Hold and release Space to cast.
5. If Unity reports that legacy input is disabled, set **Edit → Project Settings → Player → Other Settings → Active Input Handling** to **Input Manager (Old)** or **Both**, then restart the editor.
6. Use a 16:9 Game view for the intended HUD layout. A desktop keyboard and mouse are required.

No external art downloads, paid assets, third-party runtime packages, or manual scene assembly are needed. Unity generates missing standard project settings on import.

## Coastal style update

The updated presentation uses a chunky original angler with boots, jacket, backpack and fishing hat; layered faceted evergreens; coastal boulders; painted cottages with pitched roofs; a striped trader awning and NPC; dock lanterns; a beacon tower; a decorative moored research skiff; shoreline foam and angular clouds. Turquoise water has animated surface highlights using an included built-in-pipeline shader. The HUD uses warm gold accents, dark translucent panels and a floating bite indicator. The skiff is scenery, not a controllable boat.

This is an original low-poly coastal art direction inspired by the genre, with no copied Fisch assets, characters or interface. Unity import, shader compilation and the resulting appearance still need verification in Unity 6.0; no in-engine screenshot has been produced in this environment.

## Controls

| Input | Action |
|---|---|
| WASD | Move |
| Right mouse + drag | Orbit camera |
| Space or left mouse | Hold to charge; release to cast |
| Space or left mouse on BITE | Hook fish |
| Hold / release Space or left mouse while reeling | Raise / lower the white marker |
| Q | Sonar pulse; cast within 10 seconds for better rare-fish odds |
| E near the wooden stall | Trade, upgrade, travel |
| J | Collection journal |
| X | Cancel fishing |
| Escape | Pause menu |

Keep the white marker inside the moving teal fish band. Landing progress rises while aligned; tension rises when you lose the fish. Fill landing progress before tension fills. Higher rod levels widen the band.

## First expedition

Walk along the dock, cast, wait for BITE, hook, then reel. Catch a few Copperfins or Glass Minnows. Return to the wooden stall on the island, press E, and sell your catch. The first rod upgrade costs 120 shells. Ember Atoll costs 250 shells to unlock; Moonwake Reach costs 500. Travel through the trader menu is instant in this prototype. Sonar has an 18-second cooldown and boosts the selection weight of non-common fish on the next cast.

## Included

- Three explorable 3D islands, docks, trees, stalls, ocean and third-person angler.
- Nine species, weighted catches, cast charge, timed hooks, moving-target reeling and line tension.
- Sonar scouting, fish journal, selling, three rod upgrades and two destination unlocks.
- Local progression saved through Unity PlayerPrefs. Select **Tidebound → Reset prototype save** outside Play mode to clear it.
- Generated audio cue, responsive immediate-mode HUD, automatic scene setup and build-scene registration.

## Build a desktop version

Open **File → Build Profiles**, select your desktop platform (install its module through Unity Hub if needed), confirm the generated Tidebound scene is listed, then Build. Save the build outside Assets. The bootstrap also starts the game when entering Play from an otherwise empty scene.

## Project layout

- `Assets/Scripts/Tidebound.cs`: world generation, player, fishing, economy, persistence and UI.
- `Assets/Scripts/TideboundArt.cs`: faceted scenery, village props and original character models.
- `Assets/Resources/CoastalWater.shader`: animated coastal water (kept in Resources for build inclusion).
- `Assets/Editor/TideboundSetup.cs`: scene creation and save-reset menu.
- `Packages/manifest.json`: Unity built-in modules only.

## Validation and limits

The source and package structure were checked in the creation environment. **Unity Editor was unavailable, so editor import, C# compilation, Play mode and standalone builds have not been verified.** Treat this as a starter prototype, not a tested release. The art uses authored procedural meshes and blocks, the ocean is a flat plane with animated shader ripples, and the trader uses menus. No swimming, boats, online multiplayer, controller/mobile input, quests, polished animations, accessibility settings, or production audio are implemented. Balancing needs hands-on playtesting.

## Original direction for expansion

Tidebound centers on reading the sea: expand sonar into distinguishable calls, moving currents, migration windows and tide-dependent habitats. Replace instant travel with a customizable research skiff, add catch-and-release tagging expeditions, and let players restore shoreline beacons. Keep the warm coastal palette and natural proportions rather than reproducing Roblox characters, Fisch UI, locations or fish designs.

The authored code and documentation are provided under the MIT license in LICENSE. Unity itself remains subject to its own license.
