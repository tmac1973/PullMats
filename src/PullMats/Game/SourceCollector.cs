using System.Collections.Generic;
using System.Linq;
using AzuCraftyBoxes;
using AzuCraftyBoxes.IContainers;
using PullMats.Core;
using UnityEngine;

namespace PullMats.Game
{
    internal static class SourceCollector
    {
        // CraftyBoxes also returns storage the player carries; PullMats only pulls from placed storage.
        private static readonly HashSet<string> CarriedContainerTypes = new()
        {
            "BackpackContainer",
            "BowsBeforeHoesQuiver",
            "GemBagContainer",
        };

        /// <summary>
        /// Placed containers in CraftyBoxes range, nearest first, plus the stock rows the planner needs.
        /// CraftyBoxes has already dropped world/dungeon chests, containers the player can't access,
        /// carts in use and chests another player has open.
        /// </summary>
        internal static (List<IContainer> Containers, List<SourceStock> Stock) Collect(
            Player player, IReadOnlyList<MaterialNeed> needs, IReadOnlyDictionary<string, MaterialPrefab> prefabs)
        {
            Vector3 origin = player.transform.position;

            // Copy immediately: CraftyBoxes reuses its cached list between calls.
            List<IContainer> containers = API.GetNearbyContainers(player, AzuCraftyBoxesPlugin.mRange.Value)
                .Where(c => c != null && !CarriedContainerTypes.Contains(c.GetType().Name))
                .OrderBy(c => (c.GetPosition() - origin).sqrMagnitude)
                .ToList();

            var stock = new List<SourceStock>();
            for (int i = 0; i < containers.Count; i++)
            {
                IContainer container = containers[i];
                string containerPrefab = container.GetPrefabName();
                foreach (MaterialNeed need in needs)
                {
                    if (!API.CanItemBePulled(containerPrefab, prefabs[need.Key].PrefabName))
                        continue;
                    int count = container.ItemCount(need.Key);
                    if (count > 0)
                        stock.Add(new SourceStock(i, need.Key, count));
                }
            }

            return (containers, stock);
        }
    }
}
