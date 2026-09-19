using System.Collections.Generic;
using PullMats.Core;

namespace PullMats.Game
{
    internal static class PlayerSnapshot
    {
        internal static PlayerState Take(Player player, IReadOnlyList<MaterialNeed> needs)
        {
            Inventory inv = player.GetInventory();
            var carried = new Dictionary<string, int>();
            var freeStack = new Dictionary<string, int>();
            foreach (MaterialNeed need in needs)
            {
                carried[need.Key] = inv.CountItems(need.Key);
                freeStack[need.Key] = inv.FindFreeStackSpace(need.Key, global::Game.m_worldLevel);
            }

            return new PlayerState(carried, inv.GetTotalWeight(), player.GetMaxCarryWeight(), inv.GetEmptySlots(), freeStack);
        }
    }
}
