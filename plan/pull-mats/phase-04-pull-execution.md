# Phase 04 — Game adapter: recipe, sources, execution & notifications

**Depends on:** 01 (project, `Plugin.cs`, references), 02 (planner), 03 (config + `PullRequested` trigger) ·
**Enables:** 05, 06, 07 (the working feature)

## Goal
Hook the trigger to the real game. Read the selected piece's recipe, collect
placed containers in range through the AzuCraftyBoxes API, take a snapshot of
the player's inventory, run `PullPlanner`, and move the items if the plan
succeeds. Show the configured success or failure message. After this phase the
mod is feature-complete in single player.

## Files touched
- `src/PullMats/Game/RecipeReader.cs`: `Piece` → `List<MaterialNeed>` (+ map key → item prefab).
- `src/PullMats/Game/SourceCollector.cs`: CraftyBoxes containers → filtered, sorted `IContainer` list + `SourceStock` rows.
- `src/PullMats/Game/PlayerSnapshot.cs`: `Player` → `PlayerState`.
- `src/PullMats/Game/PullExecutor.cs`: carries out a `Success` plan (claims container ownership, then removes and adds items).
- `src/PullMats/Game/Notifier.cs`: formats every `PullResult` and shows it through `MessageHud`.
- `src/PullMats/Game/PullController.cs`: subscribes to `PullInput.PullRequested` and runs the pipeline.
- `src/PullMats/Plugin.cs`: replace the phase 03 log subscriber with `PullController.Init()`.

## Steps
1. **RecipeReader.Read(Piece piece)**: for each `Piece.Requirement r` in
   `piece.m_resources`, skip it if `r.m_resItem == null` or
   `r.m_resItem.m_itemData?.m_shared == null`, or if `r.GetAmount(1) <= 0`.
   Otherwise emit
   `MaterialNeed(Key: shared.m_name, DisplayName: Localization.instance.Localize(shared.m_name), PerSet: r.GetAmount(1), UnitWeight: shared.m_weight, MaxStack: shared.m_maxStackSize)`.
   It also returns a `Dictionary<string, (GameObject prefab, string prefabName)>`
   mapping each key to `r.m_resItem.gameObject` and
   `r.m_resItem.gameObject.name`. The prefab name is what CraftyBoxes' YAML
   exclusions use. If the same item appears in two requirements, their
   amounts are merged into one `MaterialNeed`.
2. **SourceCollector.Collect(Player p, needs, prefabMap)**:
   - Call `AzuCraftyBoxes.API.GetNearbyContainers(p, AzuCraftyBoxesPlugin.mRange.Value)`.
     CraftyBoxes has already dropped world/dungeon chests (creator == 0),
     containers the player can't access (`CheckAccess` + ward), carts in use, and
     chests another player has open.
   - Copy that list right away (`new List<IContainer>(...)`). CraftyBoxes reuses
     its cached list between calls.
   - Drop carried storage, where the runtime type name is `BackpackContainer`,
     `BowsBeforeHoesQuiver` or `GemBagContainer`. Match by
     `GetType().Name`, because those types are only there when the matching mod
     is loaded.
   - Sort by distance from the player (`IContainer.GetPosition()`).
   - For each container index `i` and each need, skip it if
     `!AzuCraftyBoxes.API.CanItemBePulled(container.GetPrefabName(), prefabName)`.
     Otherwise read `count = container.ItemCount(need.Key)` and add
     `SourceStock(i, need.Key, count)` when `count > 0`.
3. **PlayerSnapshot.Take(Player p, needs)**:
   - `Carried[key] = inv.CountItems(key, -1, true)`
   - `CurrentWeight = inv.GetTotalWeight()`
   - `MaxWeight = p.GetMaxCarryWeight()`
   - `EmptySlots = inv.GetEmptySlots()`
   - `FreeStackSpace[key] = inv.FindFreeStackSpace(key, Game.m_worldLevel)`
4. **PullController.OnPull(Player, Piece)**:
   1. Build the `PullRequest` using `PullMatsConfig.PullMode` and plan it.
   2. On `Success`, call `PullExecutor.Execute`. It returns a
      `Dictionary<string,int> moved` (key → amount actually added to the player)
      and a `bool shortfall`, which is true if any container gave less than planned.
   3. Call `Notifier.ShowSuccess(pieceName, needs, moved, shortfall)` on
      success, or `Notifier.ShowFailure(pieceName, result)` otherwise.
   4. Catch and log every exception, then show `PullMats error — see log` as a
      failure notification.
   5. Ignore presses that arrive while a pull is running (a simple `bool busy`
      guard), and presses within 0.2 s of the last one.
5. **PullExecutor.Execute(plan, containers, prefabMap, player)** runs
   synchronously in the same frame, in three stages:
   0. **Claim ownership:** for each distinct source in the plan that is an
      `AzuCraftyBoxes.IContainers.VanillaContainer`, get its wrapped `Container`
      from the compiler-generated primary-constructor field `<_container>P`.
      The `FieldInfo` comes from
      `HarmonyLib.AccessTools.Field(typeof(VanillaContainer), "<_container>P")`,
      cached once. Then call `container.m_nview.ClaimOwnership()` if
      `m_nview.IsValid() && !m_nview.IsOwner()`. Vanilla does the same when a
      player opens a chest. Ownership takes effect locally right away, so the
      save that follows goes out from the owner and nothing lets the previous
      owner overwrite it. If the field is missing (a CraftyBoxes refactor), log
      one warning and skip this stage; removal still works the way CraftyBoxes
      itself does it. Non-vanilla sources (drawers) handle ownership inside
      CraftyBoxes' own implementations.
   1. **Remove:** for each `Take`, call
      `moved = containers[t.SourceIndex].ProcessContainerInventory(t.Key, 0, t.Amount)`.
      This is CraftyBoxes' own removal. It decrements stacks, then calls
      `Save()` and `Changed()`, which is how the change reaches other peers.
      Add `moved` to `removed[key]`. If `moved < t.Amount` (the container
      changed since the snapshot), set `shortfall = true`, log a warning and
      keep going. Whatever was removed is still delivered.
   2. **Add:** for each key, add `removed[key]` to the player inventory in
      chunks of `min(remaining, MaxStack)` using
      `inv.AddItem(prefab, chunk)`. If `AddItem` returns false (it shouldn't,
      after the space check), build the item as
      `var data = prefab.GetComponent<ItemDrop>().m_itemData.Clone(); data.m_dropPrefab = prefab; data.m_stack = chunk; data.m_worldLevel = (byte)Game.m_worldLevel;`
      and drop it at the player's feet with
      `ItemDrop.DropItem(data, chunk, p.transform.position + p.transform.forward, Quaternion.identity)`,
      logging a warning. Items are never destroyed. Dropped chunks still count
      toward `moved`.
   3. Return `(moved, shortfall)`.
6. **Notifier**: localize the piece name with `Localization.instance.Localize`.
   `ShowSuccess` uses the configured `Success position`, and `ShowFailure`
   uses `Failure position`. The messages:
   - Success: `Pulled {piece}: {Name} ×{moved}, {Name} ×{moved}, …`, in the
     `needs` order and from the `moved` dictionary. When `shortfall` is true,
     append ` (a chest changed — pulled less than a full set)`.
   - Missing: `Can't pull {piece} — missing {Name} {avail}/{req}, …`
   - TooHeavy: `Can't pull {piece} — too heavy ({resulting:0}/{max:0})`
   - NoSpace: `Can't pull {piece} — inventory full (needs {n} more slot(s))`
   - NothingToPull: `{piece} needs no materials`

   Center messages go through `MessageHud.instance.ShowMessage(MessageType.Center, text)`.
   Top-left messages use the same call with `TopLeft`.

## Build gate
- `dotnet build -c Release` has 0 errors.
- `dotnet test tests/PullMats.Tests` passes.

## Test plan
Single player in `pullmats-dev`. Use `devcommands` (`spawn` items into two
chests about 5 m apart) to set up the stock.
- **Portal, stocked:** press N → exactly 20 fine wood, 2 surtling cores and 10
  greydwarf eyes arrive. The top-left message lists them, and the chest counts
  drop by the same amounts.
- Press N twice more → 60/6/30 carried. Chests drain nearest first, then from
  the second chest.
- **Missing:** remove all surtling cores → center message `missing Surtling core 0/2`,
  and neither the inventory nor the chests change.
- **Too heavy:** fill the inventory with stone until close to max weight, then
  pull a Karve → center `too heavy (x/y)` and nothing moves. Equip Megingjord
  and retry → the pull now succeeds.
- **No space:** fill every slot with unstackables → center `inventory full`.
- **Exclusions:** add `Wood` to a chest's `exclude` list in
  `Azumatt.AzuCraftyBoxes.yml` → that chest's wood is never taken.
- **Range:** set CraftyBoxes `Container Range` to 3 and stand 10 m away → Missing.
- **TopUp mode:** carrying 4 wood with a workbench selected, press N → pulls 6.
- **Item drawers:** skipped unless kg/mkz ItemDrawers is installed. It's not in
  the dev profile, so it's covered by CraftyBoxes' own support.
- After each test, save, quit, reload, and reopen the chests to confirm the
  counts persisted with no duplicates.

## Commit
`feat: pull a set of build materials from nearby CraftyBoxes containers`

## Rollback
Delete `src/PullMats/Game/` and restore the phase 03 subscriber in `Plugin.cs`.
The planner and config from earlier phases stay valid on their own.
