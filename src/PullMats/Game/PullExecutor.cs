using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AzuCraftyBoxes.IContainers;
using HarmonyLib;
using PullMats.Core;
using UnityEngine;

namespace PullMats.Game
{
    internal static class PullExecutor
    {
        // VanillaContainer keeps its Container in a compiler-generated primary-constructor field.
        private static readonly FieldInfo? VanillaContainerField =
            AccessTools.Field(typeof(VanillaContainer), "<_container>P");

        private static bool _warnedMissingField;

        /// <summary>
        /// Carries out a successful plan in the current frame. Returns the amount of each material actually
        /// delivered, and whether any container gave less than planned (it changed since the snapshot).
        /// </summary>
        internal static (Dictionary<string, int> Moved, bool Shortfall) Execute(
            PullResult.Success plan,
            IReadOnlyList<IContainer> containers,
            IReadOnlyDictionary<string, MaterialPrefab> prefabs,
            Player player)
        {
            foreach (int index in plan.Takes.Select(t => t.SourceIndex).Distinct())
                ClaimOwnership(containers[index]);

            var removed = new Dictionary<string, int>();
            bool shortfall = false;
            foreach (Take take in plan.Takes)
            {
                // CraftyBoxes' own removal: decrements stacks, then saves so the change reaches other peers.
                int moved = containers[take.SourceIndex].ProcessContainerInventory(take.Key, 0, take.Amount);
                if (moved < take.Amount)
                {
                    shortfall = true;
                    Plugin.Log.LogWarning($"Container {take.SourceIndex} gave {moved}/{take.Amount} {take.Key}");
                }
                removed[take.Key] = (removed.TryGetValue(take.Key, out int sum) ? sum : 0) + moved;
            }

            Inventory inv = player.GetInventory();
            foreach (KeyValuePair<string, int> entry in removed)
            {
                GameObject prefab = prefabs[entry.Key].Prefab;
                int maxStack = Mathf.Max(1, prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize);
                int remaining = entry.Value;
                while (remaining > 0)
                {
                    int chunk = Mathf.Min(remaining, maxStack);
                    if (!inv.AddItem(prefab, chunk))
                        DropAtFeet(player, prefab, chunk);
                    remaining -= chunk;
                }
            }

            return (removed, shortfall);
        }

        /// <summary>
        /// Take network ownership of a vanilla chest before modifying it (as vanilla does when a chest is
        /// opened), so the previous owner can't overwrite the change.
        /// </summary>
        private static void ClaimOwnership(IContainer source)
        {
            if (source is not VanillaContainer)
                return;
            if (VanillaContainerField == null)
            {
                if (!_warnedMissingField)
                {
                    _warnedMissingField = true;
                    Plugin.Log.LogWarning("Could not find VanillaContainer's container field; skipping ownership claim.");
                }
                return;
            }

            if (VanillaContainerField.GetValue(source) is Container container &&
                container.m_nview != null && container.m_nview.IsValid() && !container.m_nview.IsOwner())
            {
                container.m_nview.ClaimOwnership();
            }
        }

        // Only reached if AddItem fails despite the planner's space check; never destroy items.
        private static void DropAtFeet(Player player, GameObject prefab, int amount)
        {
            ItemDrop.ItemData data = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
            data.m_dropPrefab = prefab;
            data.m_stack = amount;
            data.m_worldLevel = (byte)global::Game.m_worldLevel;
            Transform t = player.transform;
            ItemDrop.DropItem(data, amount, t.position + t.forward, Quaternion.identity);
            Plugin.Log.LogWarning($"Inventory refused {amount} {prefab.name}; dropped at player's feet.");
        }
    }
}
