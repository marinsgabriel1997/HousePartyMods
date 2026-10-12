# RelationshipHints

*[Versão em português](README.md) · [All mods](../../README.en.md)*

Shows, before each option in the reply menu, the effect it will have on your
relationship with the character:

```
+1F -1R | I like your glasses. They make you look smart!
```

- **F** = friendship, **R** = romance. With the game in Portuguese,
  friendship shows as **A** (*amizade*).
- Positive values are shown in **green**, negative ones in **red**.
- Replies with no effect get no prefix.

The effect is calculated **at runtime**, from the current game state, so
conditional replies show the right result for the moment. Real example with
Katherine: "So how about letting me get you that drink now?" shows `-1F -1R`
before you have seen a certain dialogue of hers, and `+1F +1R` after.

The mod does not change any game file, neither `.character` nor language
files. It only changes the text shown in the menu, so it works with any
language, including community translations.

## Markers

| Marker | Meaning |
|---|---|
| `+1F`, `-2R` | adds or subtracts friendship or romance points |
| `?F`, `?R` | there is an effect, but it can't be predicted safely (e.g. it depends on a random roll) |
| `=5F` | sets the relationship to a fixed value instead of adding |
| `Madison: -3F` | effect on the relationship with another character, not the one you're talking to |

## Compatibility

Last tested on House Party 1.5.2.14289 with MelonLoader 0.7.3. See the
[main README](../../README.en.md#compatibility) for details.

## Installation

1. Install MelonLoader as described in the [main README](../../README.en.md#installation).
2. Download `RelationshipHints.dll` from
   [Releases](../../../../releases?q=RelationshipHints&expanded=true) and put
   it in the `Mods` folder at the game root.
3. Start the game and talk to someone.

To uninstall, delete `Mods\RelationshipHints.dll`.

## Configuration

Through the [ModSettings](../ModSettings/README.en.md) window (`F6`), with
setting names in the game language, or in `UserData\MelonPreferences.cfg`:

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
