# House Party mods

*[Versão em português](README.md)*

Mods for [House Party](https://store.steampowered.com/app/611790/House_Party/)
(Eek! Games), built with [MelonLoader](https://github.com/LavaGang/MelonLoader).

Each mod is a standalone DLL: install only the ones you want.

## Mods

| Mod | What it does | Download |
|---|---|---|
| [RelationshipHints](mods/RelationshipHints/README.en.md) | Shows each reply's effect on friendship and romance in the reply menu (`+1A -1R \| Text`) | [Releases](../../releases?q=RelationshipHints&expanded=true) |
| [NewTalkMarker](mods/NewTalkMarker/README.en.md) | Shows a "!" above a character when a new line becomes available for them | [Releases](../../releases?q=NewTalkMarker&expanded=true) |
| [ItemHighlight](mods/ItemHighlight/README.en.md) | Shows the names of items in view and makes the ones you can pick up glow, each with its own key | [Releases](../../releases?q=ItemHighlight&expanded=true) |
| [SkipIntro](mods/SkipIntro/README.en.md) | Opens the game straight to the main menu, without the Eek! Games video and the disclaimer | [Releases](../../releases?q=SkipIntro&expanded=true) |
| [AutoContinue](mods/AutoContinue/README.en.md) | Moves on from the first loading screen by itself, without waiting for a click | [Releases](../../releases?q=AutoContinue&expanded=true) |
| [Jump](mods/Jump/README.en.md) | Makes the character jump with `Space`; combat mode moves to `Alt` | [Releases](../../releases?q=Jump&expanded=true) |
| [ModSettings](mods/ModSettings/README.en.md) | In-game window (`F6`) to adjust mod settings, saved right away | [Releases](../../releases?q=ModSettings&expanded=true) |

## Compatibility

The mods are developed and tested on:

| Component | Version |
|---|---|
| House Party (Steam, Windows) | 1.5.2.14289 |
| Unity | 2022.3.62f2 (IL2CPP) |
| MelonLoader | 0.7.3 Open-Beta (net6 runtime) |

Other MelonLoader 0.7.x versions should work. Game updates may break the mods.
If that happens, the mod logs the error and the game keeps running normally,
just without the mod's feature. Your game version is shown near the top of
`MelonLoader\Latest.log`, on the `Game Version` line. Each mod's README states
the version it was last tested on.

## Installation

1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader/releases)
   **0.7.x** into the game folder
   (`...\steamapps\common\House Party`). If you use the zip, extract
   `MelonLoader.x64.zip` directly into the game folder.
2. Start the game once and wait for the main menu. On the first run,
   MelonLoader generates assemblies, which takes a few minutes.
   - If you get **"Failed to Download DeobfuscationMap!"**, open
     `UserData\Loader.cfg` in the game folder, change
     `force_offline_generation = false` to `true` and start the game again.
     The deobfuscation map MelonLoader tries to download is for an older game
     version and isn't needed.
3. Download the DLL of each mod you want (table above) and put it in the
   game's `Mods` folder, the one at the root of the install, not
   `HouseParty_Data\StreamingAssets\Mods`.

Mod settings live in `UserData\MelonPreferences.cfg`, created after the first
run. With [ModSettings](mods/ModSettings/README.en.md) you can adjust them in
the game, without editing the file. The mods' texts follow the game language:
Portuguese or, for every other language, English.

### Uninstall

To remove a mod, delete its DLL from the `Mods` folder. To also remove
MelonLoader, delete `version.dll` and the `MelonLoader`, `Mods`, `Plugins`,
`UserData` and `UserLibs` folders from the game root.

## Building from source

Requirements: [.NET SDK](https://dotnet.microsoft.com/download) 6 or newer,
and the game with MelonLoader installed and run once, because the build uses
the assemblies it generates.

```sh
dotnet build HousePartyMods.slnx -c Release
# if the game is in another folder:
dotnet build HousePartyMods.slnx -c Release -p:GameDir="D:\Games\House Party"
```

Each DLL ends up in `mods\<Mod>\bin\Release\net6.0\`.

Layout:

- `mods/`: one project per mod, each with its own README;
- `Directory.Build.props`: game folder and references shared by all mods;
- `tools/`: Python scripts used to analyze the `.character` files;
- [IDEIAS.md](IDEIAS.md): new mod ideas under evaluation (in Portuguese).

## Disclaimer

Fan project, not affiliated with Eek! Games. This repository contains no game
binaries or game code.

## License

[GPL-3.0](LICENSE)
