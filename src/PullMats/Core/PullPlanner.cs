using System;
using System.Collections.Generic;
using System.Linq;

namespace PullMats.Core
{
    /// <summary>
    /// Decides what one key press pulls. All-or-nothing: returns Success only if every material is
    /// available in full, the result stays within max carry weight, and everything fits in the inventory.
    /// </summary>
    public static class PullPlanner
    {
        public static PullResult Plan(PullRequest request)
        {
            List<MaterialNeed> needs = MergeNeeds(request.Needs);
            if (needs.Count == 0)
                return new PullResult.NothingToPull();

            PlayerState player = request.Player;
            List<MaterialAmount> totals = needs
                .Select(n => new MaterialAmount(n, AmountFor(request.Mode, n, Get(player.Carried, n.Key))))
                .ToList();

            // Availability
            var shortfalls = new List<Shortfall>();
            foreach (MaterialAmount t in totals)
            {
                int available = request.Stock.Where(s => s.Key == t.Need.Key).Sum(s => Math.Max(0, s.Available));
                if (available < t.Amount)
                    shortfalls.Add(new Shortfall(t.Need, available, t.Amount));
            }
            if (shortfalls.Count > 0)
                return new PullResult.Missing(shortfalls);

            // Weight
            float resulting = player.CurrentWeight + totals.Sum(t => t.Amount * t.Need.UnitWeight);
            if (resulting > player.MaxWeight)
                return new PullResult.TooHeavy(resulting, player.MaxWeight);

            // Space
            int slotsNeeded = 0;
            foreach (MaterialAmount t in totals)
            {
                int overflow = Math.Max(0, t.Amount - Get(player.FreeStackSpace, t.Need.Key));
                int maxStack = Math.Max(1, t.Need.MaxStack);
                slotsNeeded += (overflow + maxStack - 1) / maxStack;
            }
            if (slotsNeeded > player.EmptySlots)
                return new PullResult.NoSpace(slotsNeeded, player.EmptySlots);

            // Allocation, in source order (the adapter sorts sources nearest first)
            var takes = new List<Take>();
            foreach (MaterialAmount t in totals)
            {
                int remaining = t.Amount;
                foreach (SourceStock s in request.Stock.Where(s => s.Key == t.Need.Key).OrderBy(s => s.SourceIndex))
                {
                    if (remaining <= 0)
                        break;
                    int take = Math.Min(remaining, s.Available);
                    if (take <= 0)
                        continue;
                    takes.Add(new Take(s.SourceIndex, t.Need.Key, take));
                    remaining -= take;
                }
            }

            return new PullResult.Success(takes, totals);
        }

        /// <summary>
        /// FullSet: always one recipe's worth. TopUp: round up to the next whole-set multiple, pulling a
        /// full set when the carried amount is already an exact multiple (including zero).
        /// </summary>
        internal static int AmountFor(PullMode mode, MaterialNeed need, int carried) => mode switch
        {
            PullMode.TopUp => need.PerSet - Math.Max(0, carried) % need.PerSet,
            _ => need.PerSet,
        };

        /// <summary>Drops non-positive requirements and merges repeated items into one need.</summary>
        private static List<MaterialNeed> MergeNeeds(IReadOnlyList<MaterialNeed> needs)
        {
            var merged = new List<MaterialNeed>();
            foreach (MaterialNeed n in needs)
            {
                if (n.PerSet <= 0)
                    continue;
                int i = merged.FindIndex(m => m.Key == n.Key);
                if (i >= 0)
                    merged[i] = merged[i] with { PerSet = merged[i].PerSet + n.PerSet };
                else
                    merged.Add(n);
            }
            return merged;
        }

        private static int Get(IReadOnlyDictionary<string, int> map, string key) =>
            map.TryGetValue(key, out int value) ? value : 0;
    }
}
