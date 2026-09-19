# Phase 05 — "Pull mats" row in the build key hints

**Depends on:** 01 (Harmony/TextMeshPro references, `Plugin.cs`), 02 (test project in the build gate), 03 (button name + configured bindings) · **Enables:** 07
(README screenshots show the hint)

## Goal
Add one row, `[N] Pull mats`, to the vanilla bottom-right build hint panel
while an allowed tool is equipped. It should show the current keyboard binding,
and the gamepad binding when a controller is active. No other vanilla or modded
hint rows change.

## Files touched
- `src/PullMats/UI/KeyHintRow.cs`: Harmony patches plus row creation and updates.
- `src/PullMats/Plugin.cs`: `new Harmony(Guid).PatchAll(typeof(KeyHintRow))` in `Awake`.

## Steps
1. Postfix `KeyHints.Start` (instance `__instance`). Inside
   `__instance.m_buildHints`, get the `UIInputHint` component and its
   `m_mouseKeyboardHint` and `m_gamepadHint` child containers.
2. Keyboard row: clone the parent row of `__instance.m_buildMenuKey` (the
   "Build menu" row, a `TextMeshProUGUI` key label plus a sibling description
   label) with `Object.Instantiate(row, row.parent)`. Name it `PullMats_Hint`
   and put it last with `SetAsLastSibling()`. In the clone, set the description
   `TextMeshProUGUI` (the child that isn't the key label) to `Pull mats`.
   Remove any `Localize` component on the clone first, or it would overwrite
   the text with the original token.
3. Gamepad row: do the same with the first child row of `m_gamepadHint`,
   named `PullMats_HintGP`.
4. Postfix `KeyHints.UpdateHints`:
   1. Set each row's `SetActive` to
      `PullMatsConfig.Enabled && player.InPlaceMode() && rightItem prefab ∈ allowed tools`.
   2. Set the keyboard key label to `PullMatsConfig.PullKey.Value.ToString()`,
      but only when it has changed, compared against a cached string.
   3. Hide the gamepad row when `PullMatsConfig.PullGamepad.Value == GamepadButton.None`.
      Otherwise label it with `ZInput.instance.GetBoundKeyString("PullMats_Pull", true)`.
5. If any lookup in step 1 returns null (a Valheim UI change), log one warning
   and skip creating the hint. The pull feature itself keeps working.

## Build gate
- `dotnet build -c Release` has 0 errors.
- `dotnet test tests/PullMats.Tests` passes.

## Test plan
- Hammer equipped → the panel shows every vanilla row plus `N  Pull mats` at the
  bottom, in matching font and alignment.
- Switch to a sword → the vanilla combat hints show and the row is gone.
- Rebind to `B` with the `LeftControl` modifier → without a restart, the row
  reads `B + LeftControl`. That's BepInEx's `KeyboardShortcut.ToString()`
  format, used as-is.
- `Enabled = false` → the row is hidden.
- Controller connected with `Pull gamepad button = None` → the gamepad panel
  shows no PullMats row. Set a button → the row appears with that glyph/text.
- Compatibility smoke test: temporarily install
  `Azumatt-Build_Camera_Custom_Hammers_Edition` into `pullmats-dev` → no
  exceptions in the log, and our row is present whenever the vanilla build hint
  panel is the one showing. Uninstall it afterwards.

## Commit
`feat(ui): show Pull mats key hint in hammer build mode`

## Rollback
Delete `UI/KeyHintRow.cs` and the `PatchAll` line. The hint is cosmetic and
nothing depends on it.
