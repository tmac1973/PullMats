# Phase 02 — Pure pull-planning logic + unit tests

**Depends on:** 01 · **Enables:** 04 (the game adapter feeds this planner and
executes its plan)

## Goal
Put every PullMats *decision* into plain C# with no Unity or Valheim types:
how much to pull (FullSet/TopUp), where it comes from, and whether the pull is
refused for missing materials, weight or inventory space. Unit tests cover it on
net10. This is the logic that must never be wrong (no partial pulls, no
encumbrance), so it's tested outside the game before anything touches real
inventories.

## Files touched
- `src/PullMats/Core/PullModels.cs`: input/output record types (below).
- `src/PullMats/Core/PullPlanner.cs`: static `PullPlanner.Plan(PullRequest) → PullResult`.
- `src/PullMats/Core/IsExternalInit.cs`: `#if !NET5_0_OR_GREATER` polyfill `namespace System.Runtime.CompilerServices { internal static class IsExternalInit {} }` so positional `record` types compile on net472 (net10 tests already have it).
- `tests/PullMats.Tests/PullMats.Tests.csproj`: net10.0 xUnit project that
  links `../../src/PullMats/Core/*.cs` (`<Compile Include=... Link=...>`), with no
  reference to the plugin project.
- `tests/PullMats.Tests/PullPlannerTests.cs`: test cases.
- `PullMats.sln`: add the test project.

## Steps
1. `PullModels.cs` (namespace `PullMats.Core`; no `UnityEngine` usings):
   - `enum PullMode { FullSet, TopUp }`
   - `record MaterialNeed(string Key, string DisplayName, int PerSet, float UnitWeight, int MaxStack)`.
     `Key` is the item's shared name token (e.g. `$item_finewood`), the key
     `Inventory.CountItems` matches on.
   - `record SourceStock(int SourceIndex, string Key, int Available)`: one row per
     container × material. Excluded or inaccessible containers never show up
     here; the adapter filters them first.
   - `record PlayerState(IReadOnlyDictionary<string,int> Carried, float CurrentWeight, float MaxWeight, int EmptySlots, IReadOnlyDictionary<string,int> FreeStackSpace)`.
     `FreeStackSpace[key]` is the room left in partial stacks of that item
     already in the inventory.
   - `record PullRequest(string PieceName, PullMode Mode, IReadOnlyList<MaterialNeed> Needs, IReadOnlyList<SourceStock> Stock, PlayerState Player)`
   - `record Take(int SourceIndex, string Key, int Amount)`
   - `abstract record PullResult` with these cases:
     - `Success(IReadOnlyList<Take> Takes, IReadOnlyList<(MaterialNeed Need,int Amount)> Totals)`
     - `NothingToPull`: the piece has no requirements
     - `Missing(IReadOnlyList<(MaterialNeed Need,int Available,int Required)> Shortfalls)`
     - `TooHeavy(float ResultingWeight, float MaxWeight)`
     - `NoSpace(int SlotsNeeded, int SlotsFree)`
2. `PullPlanner.Plan`, in this order:
   1. Drop needs with `PerSet <= 0`. If nothing is left, return
      `NothingToPull`. All later steps only see needs with `PerSet >= 1`.
   2. **Amounts, per material:**
      - `FullSet`: `amount_i = PerSet_i`.
      - `TopUp`: `amount_i = PerSet_i − (Carried_i mod PerSet_i)`. This rounds
        each material up to the next whole-set multiple. When a material is
        already an exact multiple (including 0), the press pulls a full set of
        it, so every press moves something.
   3. **Availability:** `avail_i = Σ Stock[key].Available`. Every material with
      `avail_i < amount_i` goes into the shortfall list. If the list isn't
      empty, return `Missing` with all shortfalls, not just the first.
   4. **Weight:** `resulting = CurrentWeight + Σ amount_i*UnitWeight_i`. If
      `resulting > MaxWeight`, return `TooHeavy(resulting, MaxWeight)`. Equal is allowed.
   5. **Space:** for each material,
      `overflow_i = max(0, amount_i − FreeStackSpace[key])` and
      `slots_i = ceil(overflow_i / MaxStack_i)`. If `Σ slots_i > EmptySlots`,
      return `NoSpace`.
   6. **Allocation:** for each material, walk `Stock` rows in `SourceIndex` order
      (the adapter sorts by distance, nearest first) and take
      `min(remaining, Available)` until the amount is met. Return `Success`.
      `Totals` lists every material with its `amount_i`, all of which are ≥ 1.
3. Test project: `xunit` 2.9.x, `xunit.runner.visualstudio`,
   `Microsoft.NET.Test.Sdk`, `<Nullable>enable</Nullable>`,
   `<LangVersion>latest</LangVersion>`.
4. Tests (one `[Fact]` or `[Theory]` each):
   - FullSet pulls exactly `PerSet` even when the player already carries some.
   - Three FullSet plans in a row (feeding the new carried counts back in) give 3× the recipe.
   - TopUp examples:
     - carried 4/10 → pull 6
     - carried 10/10 → pull 10
     - carried 0/10 → pull 10
     - two materials (4/10 wood, 0/2 core) → pull 6 wood, 2 cores
     - carried 25/10 wood + 1/2 core → pull 5 wood and 1 core
   - Missing reports every short material with its available/required counts.
   - Weight exactly at max passes. Weight +0.1 over max gives `TooHeavy`.
   - `NoSpace`, both when stacks overflow with no empty slots and when stacks absorb everything (so it passes).
   - Allocation spans several sources in index order and never takes more than a source has.
   - A piece with no requirements, or where every `PerSet` is 0, gives `NothingToPull`. A mix of 0 and positive `PerSet` values ignores the zero ones and doesn't throw.

## Build gate
- `dotnet build -c Release` (whole solution) has 0 errors.
- `dotnet test tests/PullMats.Tests` has all tests passing.

## Test plan
The automated tests above are the verification. Also check by hand that
`src/PullMats/Core/` has no `using UnityEngine` or Valheim types
(`grep -r "UnityEngine" src/PullMats/Core` returns nothing).

## Commit
`feat(core): add pure pull planner with FullSet/TopUp, weight and space checks`

## Rollback
Delete `src/PullMats/Core/` and `tests/`, and remove the test project from the
solution. Nothing in the plugin calls the planner yet, so this is safe to revert
on its own.
