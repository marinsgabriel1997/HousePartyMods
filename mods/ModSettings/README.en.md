# ModSettings

*[Versão em português](README.md) · [All mods](../../README.en.md)*

In-game window to adjust mod settings without editing
`MelonPreferences.cfg`. `F6` opens and closes it.

- On the left, the installed mods that have settings; on the right, the
  settings of the chosen mod, with name, description and a `Default` button.
- On/Off is a single click. Numbers and text are edited by clicking the
  field, typing and pressing Enter (or clicking elsewhere); `Esc` while
  editing undoes it.
- There is no save button: every change applies right away and is saved. An
  invalid value turns red and is not saved.
- In a match, the window opens the game's pause menu underneath and closes
  it along with the window. If you were already paused, the pause stays.
- Texts follow the language chosen in the game: Portuguese or, for every
  other language, English.
- The settings list scrolls with the mouse wheel.

The menu finds by itself the settings each mod registers with MelonLoader;
no mod depends on it, and mods by other authors show up too. Mods without
settings, such as SkipIntro, are not listed.

The mod does not change any game file or save.

## Compatibility

Last tested on House Party 1.5.2.14289 with MelonLoader 0.7.3. See the
details in the [main README](../../README.en.md#compatibility).

## Installation

1. Install MelonLoader as described in the [main README](../../README.en.md#installation).
2. Download [`ModSettings.dll`](../../dist/1.0.0/ModSettings.dll) (download button on the
   file page) and put it in the `Mods` folder at the game root.
3. Start the game and press `F6`.

To uninstall, delete `Mods\ModSettings.dll`. The other mods keep working,
configurable through `MelonPreferences.cfg`.

## Settings

The key is shown in the window itself, under ModSettings. In
`UserData\MelonPreferences.cfg`:

```toml
[ModSettings]
OpenKey = "F6"
```

- `OpenKey`: key that opens and closes the window, by Unity `KeyCode` name
  (for example `F6`, `F4`, `K`). The game already uses `F10` to save and
  several other keys; see the free ones in
  [game keys](../../docs/teclas-do-jogo.md) (in Portuguese).

## Known limitations

- Some settings only apply when the game starts, such as ItemHighlight's
  "Names on at startup"; each description says so.
- Settings of types the window can't edit are shown read-only.
- The text field is basic: it types and deletes at the end, with no cursor
  in the middle and no paste.
