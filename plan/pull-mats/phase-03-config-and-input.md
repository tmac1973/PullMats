# Phase 03 — Configuration, keybind & trigger gating

**Depends on:** 01, 02 (uses `PullMats.Core.PullMode` for the Pull mode setting) · **Enables:** 04 (the adapter reads the config and runs when
the trigger fires), 05 (the key hint shows the configured binding), 06 (tests
config sync)

## Goal
Add every PullMats setting to the BepInEx config file, with admin-only gameplay
settings synced by Jotunn and per-player UI/keybind settings. Register the pull
button with Jotunn's InputManager (default keyboard **N**, gamepad unbound).
Decide each frame whether a press counts. At the end of this phase, pressing N
with the hammer and a piece selected logs `Pull requested: <piece>`. Pressing it
in any other state does nothing.

## Files touched
- `src/PullMats/PullMatsConfig.cs`: binds all config entries and exposes typed accessors.
- `src/PullMats/PullInput.cs`: Jotunn `ButtonConfig` registration and the per-frame trigger check.
- `src/PullMats/Plugin.cs`: calls `PullMatsConfig.Bind(Config)` and `PullInput.Register()` in `Awake`, and `PullInput.Tick()` in `Update`.

## Steps
1. `PullMatsConfig.Bind(ConfigFile cfg)` creates these entries. Admin-only ones
   pass `new ConfigurationManagerAttributes { IsAdminOnly = true }`, which
   Jotunn syncs from the server when the server has PullMats and makes
   read-only for non-admin clients.

   | Section | Key | Type | Default | Admin-only |
   |---|---|---|---|---|
   | `1 - General` | `Enabled` | bool | `true` | yes |
   | `1 - General` | `Pull mode` | `PullMats.Core.PullMode` | `FullSet` | yes |
   | `1 - General` | `Allowed tools` | string | `Hammer` | yes |
   | `2 - Notifications` | `Success position` | `MessageHud.MessageType` | `TopLeft` | no |
   | `2 - Notifications` | `Failure position` | `MessageHud.MessageType` | `Center` | no |
   | `3 - Input` | `Pull key` | `KeyboardShortcut` | `new KeyboardShortcut(KeyCode.N)` | no |
   | `3 - Input` | `Pull gamepad button` | `InputManager.GamepadButton` | `GamepadButton.None` | no |

   - `Allowed tools` is parsed into a `HashSet<string>` (trimmed, case-insensitive)
     and re-parsed on the entry's `SettingChanged` and when Jotunn applies synced
     values (`SynchronizationManager.OnConfigurationSynchronized`).
   - The description for `Allowed tools` says: "Comma-separated item prefab names
     that enable the pull key (e.g. Hammer). Add modded hammer prefab names to
     allow them."
2. `PullInput.Register()` creates
   `ButtonConfig { Name = "PullMats_Pull", ShortcutConfig = PullMatsConfig.PullKey, GamepadConfig = PullMatsConfig.PullGamepad, Hint = "Pull mats", ActiveInGUI = false, ActiveInCustomGUI = false, BlockOtherInputs = false }`
   and passes it to `InputManager.Instance.AddButton(Plugin.Guid, button)`.
3. `PullInput.Tick()` runs from `Plugin.Update()`. It returns early unless all
   of these hold:
   - `PullMatsConfig.Enabled`
   - `Player.m_localPlayer != null` (this is what keeps it inert on a dedicated
     server, which has no local player)
   - `ZInput.GetButtonDown(PullInput.ButtonName)`, which is the `ButtonConfig.Name` read back after `AddButton`, because Jotunn appends `!<mod guid>` to it (Jotunn registers both the
     shortcut and the gamepad button under this name)
   - no text input or menu has focus: `!Console.IsVisible()`, `!Chat.instance ||
     !Chat.instance.HasFocus()`, `!TextInput.IsVisible()`, `!Menu.IsVisible()`,
     `!InventoryGui.IsVisible()`, `!Minimap.IsOpen()`, `!StoreGui.IsVisible()`
   - `player.InPlaceMode()`
   - the right-hand item's `m_dropPrefab?.name` is in the allowed-tools set
   - `player.GetSelectedPiece() != null`

   If all hold, it raises the static event `PullInput.PullRequested(Player, Piece)`.
   For now the only subscriber logs `Pull requested: {piece.m_name}`.
4. The build menu itself is open when `Hud.IsPieceSelectionVisible()`. Presses
   count there too, so the player can pick a piece and pull without closing the
   menu. `ActiveInGUI = false` only affects Jotunn's inventory-GUI blocking,
   which is already covered by the `InventoryGui.IsVisible()` check.

## Build gate
- `dotnet build -c Release` has 0 errors.
- `dotnet test tests/PullMats.Tests` still passes.

## Test plan
In `pullmats-dev`, single player:
- Hammer equipped, Workbench selected, press N → log shows `Pull requested: $piece_workbench`.
- Hoe equipped (not in the allowed list), press N → nothing logged.
- Add `Hoe` to `Allowed tools` in ConfigurationManager (F1), press N with the hoe
  → logged. Then remove `Hoe` again.
- Hammer out with chat or console open, typing `n` → nothing logged.
- Rebind `Pull key` to `Ctrl+B` in ConfigurationManager → N no longer triggers,
  Ctrl+B does.
- `Enabled = false` → nothing triggers.
- Check `BepInEx/config/Spronglehump.PullMats.cfg` has every entry above with the
  right defaults and sections.

## Commit
`feat: add config (admin-synced gameplay settings) and Jotunn pull keybind`

## Rollback
Revert the three files. `Plugin.cs` goes back to the phase 01 version. Delete
`Spronglehump.PullMats.cfg` from the dev profile so stale entries don't linger.
