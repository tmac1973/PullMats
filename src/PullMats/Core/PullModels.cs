using System.Collections.Generic;

// Pure planning types. Must not reference UnityEngine or Valheim so they can be unit tested.
namespace PullMats.Core
{
    public enum PullMode
    {
        FullSet,
        TopUp,
    }

    /// <summary>One material of a piece recipe. Key is the item's shared name token (e.g. "$item_finewood").</summary>
    public sealed record MaterialNeed(string Key, string DisplayName, int PerSet, float UnitWeight, int MaxStack);

    /// <summary>How much of one material a single source container holds.</summary>
    public sealed record SourceStock(int SourceIndex, string Key, int Available);

    /// <summary>
    /// Snapshot of the player's inventory. FreeStackSpace[key] is the room left in partial stacks of
    /// that item already in the inventory. Missing keys count as 0.
    /// </summary>
    public sealed record PlayerState(
        IReadOnlyDictionary<string, int> Carried,
        float CurrentWeight,
        float MaxWeight,
        int EmptySlots,
        IReadOnlyDictionary<string, int> FreeStackSpace);

    public sealed record PullRequest(
        string PieceName,
        PullMode Mode,
        IReadOnlyList<MaterialNeed> Needs,
        IReadOnlyList<SourceStock> Stock,
        PlayerState Player);

    public sealed record Take(int SourceIndex, string Key, int Amount);

    public sealed record MaterialAmount(MaterialNeed Need, int Amount);

    public sealed record Shortfall(MaterialNeed Need, int Available, int Required);

    public abstract record PullResult
    {
        private PullResult() { }

        public sealed record Success(IReadOnlyList<Take> Takes, IReadOnlyList<MaterialAmount> Totals) : PullResult;

        /// <summary>The piece has no material requirements.</summary>
        public sealed record NothingToPull : PullResult;

        public sealed record Missing(IReadOnlyList<Shortfall> Shortfalls) : PullResult;

        public sealed record TooHeavy(float ResultingWeight, float MaxWeight) : PullResult;

        public sealed record NoSpace(int SlotsNeeded, int SlotsFree) : PullResult;
    }
}
