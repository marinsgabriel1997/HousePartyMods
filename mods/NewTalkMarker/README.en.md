# NewTalkMarker

*[Versão em português](README.md) · [All mods](../../README.en.md)*

Shows a **!** above a character's head when a new line becomes available for
them, unlocked by something you did in the game. For example, after talking
to Katherine, Stephanie gets the line "Katherine seems cool." and is marked.

| Mark | Meaning |
|---|---|
| yellow **!** | new line you haven't seen in the menu yet |
| green **!** | new line you saw in the menu but didn't pick |
| yellow and green **! !** | the character has both |

- Picking the line removes its mark.
- When you start a game, or load a save with no mod record, everything already
  available counts as known: nobody is marked.
- Lines that a conversation with a character unlocks for that same character,
  and that show up in the menu during that conversation, aren't marked. If the
  conversation ends before showing the new line, the character turns yellow.
- Marks are hidden while paused, in menus and in conversations, and are only
  shown up to a maximum distance.

The mod doesn't change any game file or save. Lines are identified by their
Id, not their text, so it works with any language.

## Marks per save

Marks are stored with each save (manual, quick and auto save), in a
`<save name>.newtalkmarker` file in the game's save folder
(`%USERPROFILE%\AppData\LocalLow\Eek\House Party\Saves`). When you load the
save, the marks go back to what they were when it was made. They reappear a
few seconds after the game finishes loading. Deleting the save in the game
also deletes the mod's file.

## Compatibility

Last tested on House Party 1.5.2.14289 with MelonLoader 0.7.3. See the
details in the [main README](../../README.en.md#compatibility).

## Installation

1. Install MelonLoader as described in the [main README](../../README.en.md#installation).
2. Download `NewTalkMarker.dll` from the
   [Releases](../../../../releases?q=NewTalkMarker&expanded=true) page and
   put it in the `Mods` folder at the game root.
3. Start the game and start or load a game.

To uninstall, delete `Mods\NewTalkMarker.dll`. The `.newtalkmarker` files in
the save folder can be deleted.

## Settings

In `UserData\MelonPreferences.cfg`:

```toml
[NewTalkMarker]
MaxDistance = 25.0
DiagnosticLog = false
```

- `MaxDistance`: maximum distance, in meters, to show the "!".
- `DiagnosticLog`: with `true`, the mod writes to `MelonLoader\Latest.log`
  every mark change, with the line and its criteria, and what it writes and
  reads with saves. If you find a wrong mark, turn this on, reproduce the case
  and attach the log when opening an issue.

## Known limitations

- Only the character's opening menu counts; lines that come up in the middle
  of a conversation aren't marked.
- Momentary conditions (where the character is, what they're doing, whether
  they're busy) are ignored so marks don't flicker. Because of that, a marked
  line may only show up in the menu at another moment.
- Getting an item may unlock a generic line for several characters at once,
  marking all of them.
- Restoring auto saves hasn't been tested in game, and it hasn't been checked
  whether Steam Cloud syncs the `.newtalkmarker` files. Without them, the save
  loads with no marks.
