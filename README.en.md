# RelationshipHints: a House Party mod

*[Versão em português](README.md)*

Shows, before each option in the reply menu, the effect it will have on your
relationship with the character:

```
+1A -1R | I like your glasses. They make you look smart!
```

- **A** = friendship (*amizade*), **R** = romance.
- Positive values are shown in **green**, negative ones in **red**.
- Replies with no effect get no prefix.

The effect is calculated **at runtime**, from the current game state, so
conditional replies show the right result for the moment. Real example with
Katherine: "So how about letting me get you that drink now?" shows `-1A -1R`
before you have seen a certain dialogue of hers, and `+1A +1R` after.

The mod does not change any game file, neither `.character` nor language
files. It only changes the text shown in the menu, so it works with any
language, including community translations.

## Markers

| Marker | Meaning |
|---|---|
| `+1A`, `-2R` | adds or subtracts friendship or romance points |
| `?A`, `?R` | there is an effect, but it can't be predicted safely (e.g. it depends on a random roll) |
| `=5A` | sets the relationship to a fixed value instead of adding |
| `Madison: -3A` | effect on the relationship with another character, not the one you're talking to |

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
3. Download `RelationshipHints.dll` from [Releases](../../releases) and put it
   in the game's `Mods` folder, the one at the root of the install, not
   `HouseParty_Data\StreamingAssets\Mods`.
4. Start the game and talk to someone.

### Uninstall

Delete `Mods\RelationshipHints.dll`. To also remove MelonLoader, delete
`version.dll` and the `MelonLoader`, `Mods`, `Plugins`, `UserData` and
`UserLibs` folders from the game root.

## Configuration

After the first run, `UserData\MelonPreferences.cfg` gets this section:

```toml
[RelationshipHints]
DiagnosticLog = false
```

With `DiagnosticLog = true`, the mod writes to `MelonLoader\Latest.log`, for
each menu, the predicted effect of each reply and the path that led to it. On
each choice, it compares the prediction with the actual relationship change
the game applied and tags it `OK`, `DIVERGE` or `INCERTO` (uncertain). If you
find a wrong marker, turn this on, reproduce the case and attach the log when
you open an issue.

## How the effect is calculated

For each reply shown, the mod walks the same data the game uses:

1. the reply's own events (`ResponseEvents`);
2. the dialogue redirects (event 120) it triggers, with conditions evaluated
   by the game's own criteria evaluator. A redirect to the same character
   replaces `Next`, as it does in the game;
3. the next dialogue (`Next`): its start events (`StartEvents`) and close
   events (`CloseEvents`).

In dialogue 0, the character's "idle" conversation, only relationship events
count. Its redirects pick what the NPC says regardless of your choice.

The effect becomes `?` when there is a random criterion (`CoinFlip`) or when
the condition depends on something the reply itself changes before it is
evaluated.

### Known limitations

- The `CloseEvents` path and effects on other characters have had little
  in-game testing so far.
- Translations that already embed relationship markers in the text will show
  the marker twice.

## Building from source

Requirements: [.NET SDK](https://dotnet.microsoft.com/download) 6 or newer,
and the game with MelonLoader installed and run once, because the build uses
the assemblies it generates.

```sh
cd RelationshipHints
dotnet build -c Release
# if the game is in another folder:
dotnet build -c Release -p:GameDir="D:\Games\House Party"
```

The DLL ends up in `RelationshipHints\bin\Release\net6.0\`.

The `tools/` folder has Python scripts used to analyze the `.character` files
during development.

## Disclaimer

Fan project, not affiliated with Eek! Games. This repository contains no game
binaries or game code.

## License

[GPL-3.0](LICENSE)
