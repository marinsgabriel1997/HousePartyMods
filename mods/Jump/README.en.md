# Jump

*[Versão em português](README.md) · [All mods](../../README.en.md)*

Makes the character jump with `Space`. Combat mode, which the game puts on
`Space`, moves to `Alt`.

- The jump is about 0.8 m high and lasts under a second. You can walk and
  run in the air and land on furniture.
- Standing or walking, the character plays the game's jump animation.
  Running, it jumps without it, because the animation jolts the camera.
- It only jumps from the ground, one jump at a time, and doesn't jump in
  dialogues, menus, pause and cutscenes.
- In the game's control mapping (`F8`), combat shows on `Alt`, and `Space`
  is reserved for jumping: the game won't accept another action on it.
- The combat change isn't saved in the game's controls. Without the mod,
  combat goes back to `Space`.
- If you've put combat on another key through `F8`, the mod leaves it
  alone.
- If something fails, the mod logs it once and the game goes on without
  jumping.

The mod doesn't change any game file or save.

## Compatibility

Last tested on House Party 1.5.2.14289 with MelonLoader 0.7.3. See the
details in the [main README](../../README.en.md#compatibility).

## Installation

1. Install MelonLoader as described in the [main README](../../README.en.md#installation).
2. Download `Jump.dll` from the
   [Releases](../../../../releases?q=Jump&expanded=true) page and put it in
   the `Mods` folder at the game root.
3. Start the game, start or load a game and press `Space`.

To uninstall, delete `Mods\Jump.dll`. Combat goes back to `Space`.

## Settings

Through the [ModSettings](../ModSettings/README.en.md) window (`F6`), with
setting names in the game language, or in `UserData\MelonPreferences.cfg`:

```toml
[Jump]
Key = "Space"
Height = 0.8
Animation = true
CombatOnAlt = true
```

- `Key`: jump key, by Unity `KeyCode` name (for example `Space`, `V`, `G`).
  The game already uses several keys; G, K, L, N, V, X and Z are free with
  the default controls.
- `Height`: jump height in meters, from 0.1 to 3.
- `Animation`: with `false`, the character jumps without the animation when
  standing and walking too.
- `CombatOnAlt`: with `false`, combat stays on `Space`, and `Space` jumps
  and toggles combat at the same time. The change only happens with jump on
  `Space`.

## Known limitations

- It also jumps in combat mode.
- Jumping lets you reach places the game didn't plan for, such as the top
  of furniture. Story triggers tied to an area may fire when you jump into
  it, as if you had walked there.
- If a new game version changes how controls are saved, `Alt` may end up
  saved. In that case, reset combat to default in `F8`.
