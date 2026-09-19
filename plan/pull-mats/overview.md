# PullMats — Project Overview

## Problem
In Valheim, AzuCraftyBoxes lets you build from nearby chests, but only at base.
Before leaving base to build somewhere else (a portal, a Karve, a workbench),
you have to look up each recipe, open chests one by one, and pull the right
amount of each material by hand. That's slow and easy to get wrong. The
material list is right there in the hammer menu, but you can't use it to fill
your inventory.

This affects anyone who packs build kits before an expedition, in single
player, on a listen server, or on a dedicated server.

## Goals
- With the **hammer** equipped and a piece selected in the build menu, pressing
  a configurable key (default **N**) moves exactly one full set of that piece's
  materials from nearby storage into the player's inventory.
- Each press pulls another full set, so pressing N three times on a portal
  gives enough for three portals.
- Pulls are **all-or-nothing**:
  - if any material is short, nothing moves and the message lists what's missing;
  - if the set would push the player over max carry weight, nothing moves and
    the message shows the weights;
  - if the inventory has no room for the set, nothing moves and the message says so.
- Show an on-screen message on success (what was pulled) and on every failure.
  Success and failure messages each have a configurable position.
- Works in single player, as a client on a listen server, and as a client on a
  dedicated server.
- Reuse AzuCraftyBoxes for finding containers, the pull range, per-container
  item exclusions, and third-party storage (item drawers etc.).
- Use Jotunn for input: a configurable key, an optional gamepad button, and a
  key hint in the build HUD.
- Ship as a Thunderstore-format package published to Hexium.

## Non-goals
- No container scanning of our own and no standalone mode. AzuCraftyBoxes is
  required.
- No pulling from storage the player carries (Backpacks-mod bags, quivers, gem
  bags). Only placed storage counts.
- No partial pulls.
- No other build tools (hoe, cultivator) unless their prefab is added to the
  allowed-tools list.
- No crafting-recipe (workbench/forge item) pulling. This is for build pieces
  only.
- No required server-side component. Nothing is authoritative on the server;
  the server only syncs config.
- No automated publishing. Uploading to Hexium is done by hand.
- No default gamepad binding.

## Users & primary flow
**User:** a Valheim player (any server type) with AzuCraftyBoxes and Jotunn
installed, who packs build materials before leaving base.

1. The player stands near their storage (within CraftyBoxes' *Container Range*,
   20 m by default).
2. They equip the hammer and select a piece in the build menu, e.g. *Portal*.
3. The build HUD key hint shows `N  Pull mats`.
4. They press **N**.
5. PullMats works out one set (or a top-up amount in `TopUp` mode) from the
   piece's recipe.
6. It finds placed containers in range through the CraftyBoxes API. It skips
   containers the player can't access, containers another player has open, and
   items excluded by CraftyBoxes' YAML.
7. Before moving anything, it checks three things: every material is available
   in full, current weight + set weight ≤ max carry weight, and the inventory
   has room.
8. **If all three pass:** it removes the materials from the containers, adds
   them to the inventory, and shows e.g. `Pulled Portal: Fine wood ×20,
   Surtling core ×2, Greydwarf eye ×10`.
9. **If any fail:** nothing moves, and a message explains why, e.g.
   `Can't pull Portal — missing Surtling core 1/2`, or
   `Can't pull Portal — too heavy (312/300)`, or `Can't pull Portal — inventory full`.
10. Pressing N again repeats the pull for another set.

## Constraints
- **Stack:** C# targeting .NET Framework 4.7.2 (net472), BepInEx 5
  (BepInExPack_Valheim), Harmony, built with the local .NET 10 SDK.
- **Hard dependencies:** `Azumatt-AzuCraftyBoxes` (developed against 1.8.22)
  and `ValheimModding-Jotunn` (developed against 2.30.1). Both are on Hexium.
- **CraftyBoxes API used:** `AzuCraftyBoxes.API.GetNearbyContainers`,
  `API.CanItemBePulled`, the `IContainer` interface
  (`ItemCount`, `ProcessContainerInventory`, `GetPrefabName`, `GetInventory`,
  `GetPosition`), and the synced `AzuCraftyBoxesPlugin.mRange` setting.
  Carried-bag implementations (BackpackContainer, BowsBeforeHoesQuiver,
  GemBagContainer) are filtered out. For vanilla chests, PullMats also reads
  `VanillaContainer`'s wrapped `Container` (a private field, through Harmony
  `AccessTools`) so it can claim network ownership before modifying it.
- **Multiplayer model:** follows CraftyBoxes, so all logic runs on the client.
  Container changes go out through the container's normal save and network
  sync. PullMats skips containers another player has open, the same rule
  CraftyBoxes uses. Before modifying a vanilla chest, the client claims its
  network ownership, as vanilla does when a chest is opened, so the change
  can't be overwritten by the chest's previous owner.
- **Server:** installing PullMats on the server is optional. When it's
  installed, Jotunn syncs the admin-only gameplay settings (enabled, pull mode,
  allowed tools) to clients. Keybinds and notification positions always stay
  per-player. A version mismatch must never block anyone from joining.
- **Access control:** same rules as CraftyBoxes. A player can only pull from a
  container they could open themselves (container access + ward/PrivateArea
  access) that a player built (dungeon/world chests are skipped).
- **Encumbrance:** compare against `Player.GetMaxCarryWeight()`, which includes
  Megingjord, buffs and modded bonuses.
- **Identity:** plugin name `PullMats`, author/team `Spronglehump`, GUID
  `Spronglehump.PullMats`.
- **Distribution:** Thunderstore-format zip (`manifest.json`, 256×256 `icon.png`,
  `README.md`, `CHANGELOG.md`, DLL) uploaded to Hexium. The manifest
  `website_url` is `https://github.com/tmac1973/PullMats` (public repo).
- **Dev environment:** Linux with the Gale mod manager. Game DLLs come from
  `/games/SteamLibrary/steamapps/common/Valheim`. Test builds deploy to a new
  minimal Gale profile, `pullmats-dev`, with only Jotunn, AzuCraftyBoxes and
  Official BepInEx ConfigurationManager. Multiplayer is tested on a local
  Valheim Dedicated Server.

## Success criteria
- On a fresh `pullmats-dev` profile with a stocked chest in range, hammer
  equipped and *Portal* selected, pressing N moves exactly one Portal's worth
  of materials into the inventory and shows a success message.
- Pressing N three times gives exactly three sets (FullSet mode), even when the
  player already carried some of the materials.
- With `TopUp` mode selected, one press rounds each material up to the next
  whole-set multiple. A material that's already an exact multiple (including
  zero) gets a full set.
- If one material is short, nothing moves and the message names the missing
  item and its available/needed counts.
- If the set would exceed max carry weight, nothing moves and the message shows
  the weight numbers.
- If the inventory has no room, nothing moves and the message says so.
- Materials in CraftyBoxes-excluded items, locked or warded chests the player
  can't access, and chests another player has open are never taken.
- The key does nothing unless an allowed tool (default `Hammer`) is equipped
  with a piece selected.
- The same scenarios pass in single player, as host, and as a client on a local
  dedicated server. No items are duplicated or lost, as checked by reopening
  chests and relogging.
- Players can join a dedicated server whether or not PullMats is installed on
  it. If it is installed, the admin-only settings sync to clients.
- The key and gamepad button can be changed in the config / ConfigurationManager,
  and the build HUD key hint shows the current binding.
- The built zip imports into Gale and installs with its dependencies resolved.

## Decisions
- **Relationship to AzuCraftyBoxes** → Hard dependency. Use its API for
  container discovery, range, YAML exclusions and third-party storage.
- **Input handling** → Jotunn input: configurable key (default N), gamepad
  button, and a build-mode key hint.
- **Pull range** → Always CraftyBoxes' `Container Range` setting. PullMats has
  no range setting of its own.
- **Pull sources** → Placed storage only. Carried bags are ignored.
- **Set math when already carrying mats** → Configurable. Default: always pull
  a full set (`FullSet`). Alternative: top up to the next whole-set multiple
  (`TopUp`). Rationale: kits for several builds that share a material (a boat,
  then a portal) must not undercount.
- **Partial availability** → All-or-nothing; the message lists what's missing.
- **Notification placement** → Configurable per type. Default: success
  top-left, failures center-screen.
- **Encumbrance rule** → Refuse if current weight + set weight > max carry
  weight. No safety margin. Inventory space is also checked.
- **Which tools trigger the pull** → Hammer only.
- **How the hammer is identified** → Configurable comma-separated prefab list,
  default `Hammer`.
- **Default gamepad binding** → None. Configurable.
- **Server control** → Optional server install. When present, Jotunn syncs
  admin-only gameplay settings. Keybinds and notification positions stay
  per-player.
- **Mod name / author** → `PullMats` by `Spronglehump`, GUID
  `Spronglehump.PullMats`.
- **Distribution** → Thunderstore-compatible zip targeted at Hexium (where
  Azumatt publishes first). Uploaded by hand.
- **Dev deploy target** → New minimal Gale profile `pullmats-dev`.
- **Multiplayer testing** → Local Valheim Dedicated Server + own client, with a
  written manual test checklist.
- **Source control** → Public GitHub repo `tmac1973/PullMats`, used as the
  manifest `website_url`.
- **Build-mode key hint** → Add one "Pull mats" row to the vanilla hint panel
  with a Harmony postfix. No Jotunn KeyHintConfig, because it would replace the
  whole panel.
- **Project setup** → Hand-rolled net472 SDK csproj. JotunnLib and the
  publicizer come from NuGet. Valheim DLLs come from the Steam install;
  BepInEx, Harmony and CraftyBoxes come from the Gale dev profile. Nothing is
  vendored into the repo.
- **Automated tests** → xUnit (net10) unit tests for the pure planning logic.
  In-game behaviour is covered by manual checklists.
- **Message language** → English message templates. Item and piece names use
  Valheim's own localization.
- **Second-player tests (chest in use, foreign ward)** → A friend with their
  own Steam account joins the local dedicated server. Release is blocked until
  those rows pass.
