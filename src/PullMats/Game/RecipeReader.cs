using System.Collections.Generic;
using PullMats.Core;
using UnityEngine;

namespace PullMats.Game
{
    /// <summary>Item prefab behind a material, plus its prefab name (what CraftyBoxes' YAML exclusions use).</summary>
    internal sealed record MaterialPrefab(GameObject Prefab, string PrefabName);

    internal static class RecipeReader
    {
        internal static (List<MaterialNeed> Needs, Dictionary<string, MaterialPrefab> Prefabs) Read(Piece piece)
        {
            var needs = new List<MaterialNeed>();
            var prefabs = new Dictionary<string, MaterialPrefab>();

            foreach (Piece.Requirement r in piece.m_resources)
            {
                ItemDrop.ItemData.SharedData? shared = r.m_resItem?.m_itemData?.m_shared;
                if (shared == null)
                    continue;
                int amount = r.GetAmount(1);
                if (amount <= 0)
                    continue;

                int existing = needs.FindIndex(n => n.Key == shared.m_name);
                if (existing >= 0)
                {
                    needs[existing] = needs[existing] with { PerSet = needs[existing].PerSet + amount };
                    continue;
                }

                needs.Add(new MaterialNeed(
                    shared.m_name,
                    Localization.instance.Localize(shared.m_name),
                    amount,
                    shared.m_weight,
                    shared.m_maxStackSize));
                GameObject prefab = r.m_resItem!.gameObject;
                prefabs[shared.m_name] = new MaterialPrefab(prefab, prefab.name);
            }

            return (needs, prefabs);
        }
    }
}
