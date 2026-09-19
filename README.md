# PullMats

Pack build kits before you leave base. With the **hammer** equipped and a piece selected in the build menu, press **N** to pull one full set of that piece's materials from nearby chests into your inventory. Press again for another set: three presses on a Portal gives you enough for three portals.

Built on [AzuCraftyBoxes](https://valheim.hexium.gg/mods/Azumatt/AzuCraftyBoxes), so it uses the same range, the same per-container exclusions and the same storage (vanilla chests, carts, ships, item drawers).

## Requirements

- [Jotunn](https://valheim.hexium.gg/mods/ValheimModding/Jotunn)
- [AzuCraftyBoxes](https://valheim.hexium.gg/mods/Azumatt/AzuCraftyBoxes)

## Usage

1. Stand within CraftyBoxes' *Container Range* of your storage (20 m by default).
2. Equip the hammer, open the build menu and pick a piece (the placement ghost is showing).
3. Press **N**. The build hints in the bottom right show a `N  Pull mats` row as a reminder.

A message shows what was pulled, e.g. `Pulled Portal: Fine wood ×20, Surtling core ×2, Greydwarf eye ×10`.

## All-or-nothing

A pull either moves the whole set or nothing at all:

- **Missing materials:** nothing moves; the message lists every short item, e.g. `missing Surtling core 1/2`.
- **Too heavy:** if the set would take you over your max carry weight (Megingjord and buffs count), nothing moves.
- **Inventory full:** if the set doesn't fit, nothing moves.

## Configuration

`BepInEx/config/Spronglehump.PullMats.cfg`, or in-game with a configuration manager (F1).

| Setting | Default | Server-synced | Description |
|---|---|---|---|
| Enabled | `true` | yes | Master switch for the pull key. |
| Pull mode | `FullSet` | yes | `FullSet`: each press pulls one full recipe, regardless of what you carry. `TopUp`: each press rounds every material up to the next whole set. |
| Allowed tools | `Hammer` | yes | Comma-separated item prefab names that enable the pull key. Add modded hammers here. |
| Success position | `TopLeft` | no | Where the "Pulled ..." message appears. |
| Failure position | `Center` | no | Where "Can't pull ..." messages appear. |
| Pull key | `N` | no | Keyboard shortcut. |
| Pull gamepad button | `None` | no | Gamepad button (unbound by default). |

## Multiplayer

PullMats runs on the client of whoever presses the key. It works in single player, as a host, and as a client of a dedicated server.

- **Server install is optional.** If the server has PullMats, the server-synced settings above are enforced for everyone (admins can edit them). If it doesn't, each player's own settings apply.
- **It never blocks joining.** Players without it, or with a different version, can still join.
- Chests another player has open, and chests you can't access (locked or warded), are skipped.

## Notes and limitations

- Only placed storage counts. Bags you carry (Backpacks, quivers, gem bags) are not pulled from.
- Build pieces only; workbench/forge crafting recipes are not covered.
- CraftyBoxes' YAML exclusions apply: an item excluded for a container is never pulled from it.

## Source

<https://github.com/tmac1973/PullMats>
