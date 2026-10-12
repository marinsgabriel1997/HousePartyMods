# ItemHighlight

*[Versão em português](README.md) · [All mods](../../README.en.md)*

Highlights the items you can interact with, so you don't have to hover the
mouse over everything. There are two independent highlights, each with its
own key:

- **Names (`H`)**: shows the name of each item in view, right above it, in
  the color of its action type. `H` again hides them.
- **Glow (`J`)**: makes items you can pick up pulse in pink. Each press goes
  to the next strength level (0.3, 0.6, 1.2, 2.5 and 5); after the last one,
  it turns off.

| Name color | Item type | Shown by default |
|---|---|---|
| gold | can be picked up | yes |
| cyan | another action, such as drink, turn on or rummage | yes |
| light green | opens or closes (doors, drawers, cabinets) | no |
| light gray | sit, lie down or climb | no |
| white | inspect only | no |

- Only items in view are shown: anything behind walls or other objects is
  left out. Characters and items in the inventory aren't highlighted.
- By default, only items you can pick up glow. The toilet, stove and trash
  can show their names but don't pulse.
- Highlights are hidden while paused, in menus and in conversations, and come
  back afterwards. The keys don't work at those moments, so typing a save
  name doesn't toggle them.
- Turning the glow off restores the items' original look.
- Names follow the language chosen in the game, using the game's own
  translations; items without a translation show their English name.

The mod doesn't change any game file or save.

## Compatibility

Last tested on House Party 1.5.2.14289 with MelonLoader 0.7.3. See the
details in the [main README](../../README.en.md#compatibility).

## Installation

1. Install MelonLoader as described in the [main README](../../README.en.md#installation).
2. Download [`ItemHighlight.dll`](../../dist/1.0.0/ItemHighlight.dll) (download button on the
   file page) and put it in the `Mods` folder at the game root.
3. Start the game, start or load a game and press `H` or `J`.

To uninstall, delete `Mods\ItemHighlight.dll`.

## Settings

Through the [ModSettings](../ModSettings/README.en.md) window (`F6`), with
setting names in the game language, or in `UserData\MelonPreferences.cfg`:

```toml
[ItemHighlight]
LabelsKey = "H"
GlowKey = "J"
StartLabels = false
StartGlow = false
GlowUse = false
ShowCollectible = true
ShowUse = true
ShowOpenClose = false
ShowPosition = false
ShowInspect = false
```

- `LabelsKey` and `GlowKey`: keys for names and glow, by Unity `KeyCode`
  name (for example `H`, `G`, `F6`). The game already uses several keys; G,
  H, J, K, L, N, V, X and Z are free with the default controls.
- `StartLabels` and `StartGlow`: whether names and glow start on when the
  game opens. The glow starts at the first level.
- `GlowUse`: with `true`, items with other actions (cyan name) glow too.
- `ShowCollectible`, `ShowUse`, `ShowOpenClose`, `ShowPosition` and
  `ShowInspect`: which item types are highlighted, in names and glow.

## Known limitations

- How strong the glow looks depends on the material: dark, matte items show
  up at the first level, but light or shiny items, such as the beer bottle
  and sheets of paper, only show up well from 2.5. Some materials don't
  glow.
- Small items barely show up with the glow; names help more for them.
- There's no distance limit. Items seen from afar through windows or open
  doors also show up, and nearby names can overlap.
- An item touching another object may not be highlighted, and items behind
  glass may show up.
- Items hidden on purpose, such as puzzle items, are highlighted too. Use the
  keys when you want.
