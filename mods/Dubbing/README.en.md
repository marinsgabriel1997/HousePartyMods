# Dubbing

*[Versão em português](README.md) · [All mods](../../README.en.md)*

Plays dubbings in other languages instead of the original voices, from
`.ogg` files in a folder. The mod swaps the voice when it plays: the game's
files are not changed.

- It covers dialogue lines and voiced background chatter.
- The character's mouth follows the dubbed voice, and the conversation waits
  for the dubbed line to finish, as with the original.
- A line without a file in the chosen dubbing plays the original voice. You
  can dub just part of the game.
- Replacing a file in the folder takes effect the next time the line plays,
  without restarting the game.
- If a file is broken or something fails, the mod logs it once and the line
  plays the original voice.

The mod doesn't include dubbings: they're installed separately, in one
folder per language. It doesn't change any game file or save.

## Compatibility

Last tested on House Party 1.5.2.14289 with MelonLoader 0.7.3. See the
details in the [main README](../../README.en.md#compatibility).

## Installation

1. Install MelonLoader, as described in the [main README](../../README.en.md#installation).
2. Download `Dubbing.dll` from the
   [Releases](../../../../releases?q=Dubbing&expanded=true) page and put it
   in the `Mods` folder at the root of the game.
3. Put the dubbing in `UserData\Dubbing\<language>` (see below).
4. Open the game and choose the dubbing in the "Dubbing" option of
   ModSettings (`F6`), or in `Language` in the settings file (see below).

To uninstall, delete `Mods\Dubbing.dll`. The dubbings stay in
`UserData\Dubbing` and can be deleted too.

## Dubbing files

One `.ogg` (Ogg Vorbis) per line, named after the original audio clip, in a
folder per language, story and character:

```text
UserData\Dubbing\<language>\<story>\<character>\<clip>.ogg
```

For example:

```text
UserData\Dubbing\pt-BR\Original Story\Amy\base_dialogue_originalstory_amy_12.ogg
UserData\Dubbing\pt-BR\Original Story\Amy\base_backgroundchatter_amy_33.ogg
```

- `<language>`: any name, the same one chosen in the dubbing option.
- `<story>`: the name of the story's folder in
  `HouseParty_Data\StreamingAssets\Mods\Stories`, for example
  `Original Story`, `Date Night With Brittney` or
  `A Vickie Vixen Valentine`.
- `<character>`: the name of the character's `.character` file in that
  folder, without the extension, for example `Amy`.
- `<clip>`: the name of the original clip in the game's audio bundles, such
  as `base_dialogue_originalstory_amy_12` (Amy's line 12),
  `base_dialogue_originalstory_amy_0.1` (variant 1 of line 0) or
  `base_backgroundchatter_amy_33` (Amy's background chatter 33).

Background chatter goes in the folder of the character who speaks. A file in
another character's or another story's folder isn't used.

## Configuration

Through the [ModSettings](../ModSettings/README.en.md) window (`F6`), with
the option names in the game's language, or in
`UserData\MelonPreferences.cfg`:

```toml
[Dubbing]
Language = "pt-BR"
```

- `Language`: name of the dubbing folder in `UserData\Dubbing`. Empty turns
  the mod off. The option's description lists the folders found when the
  game started. If the folder doesn't exist, the mod logs it once and lines
  play the original voice.

## Known limitations

- Lines that have no original audio in the game stay silent, even with a
  file in the folder.
- It doesn't dub the loose lines the game plays from generic bundles (the
  talking fish, Britt and the driver) or cutscenes.
- Tested with the full Portuguese dubbing only in the Original Story. In the
  other stories and for `Phone Call`, the character folder name wasn't
  checked in the game.
- Each line is read from disk when it starts, which took a few milliseconds
  in testing. On a very slow disk, the line may take a moment to start.
