using System.Collections.Generic;
using UnityEngine;

namespace VoidBound.Data
{
    // Baked lookup of every item by itemId, so a save file's string ids resolve
    // back to their ScriptableObject assets on load. Rebuild via
    // "VoidBound > Bake Item Registry". Lives in Resources so it loads at runtime.
    public class ItemRegistrySO : ScriptableObject
    {
        public GearItemSO[] gear;
        public MaterialItemSO[] materials;
        public QuestSO[] quests;

        private Dictionary<string, GearItemSO> gearById;
        private Dictionary<string, MaterialItemSO> matById;
        private Dictionary<string, QuestSO> questById;

        public GearItemSO GetGear(string id)
        {
            if (gearById == null) Build();
            return !string.IsNullOrEmpty(id) && gearById.TryGetValue(id, out var g) ? g : null;
        }

        public MaterialItemSO GetMaterial(string id)
        {
            if (matById == null) Build();
            return !string.IsNullOrEmpty(id) && matById.TryGetValue(id, out var m) ? m : null;
        }

        public QuestSO GetQuest(string id)
        {
            if (questById == null) Build();
            return !string.IsNullOrEmpty(id) && questById.TryGetValue(id, out var q) ? q : null;
        }

        private void Build()
        {
            gearById = new Dictionary<string, GearItemSO>();
            if (gear != null)
                foreach (var g in gear)
                    if (g != null && !string.IsNullOrEmpty(g.itemId)) gearById[g.itemId] = g;

            matById = new Dictionary<string, MaterialItemSO>();
            if (materials != null)
                foreach (var m in materials)
                    if (m != null && !string.IsNullOrEmpty(m.itemId)) matById[m.itemId] = m;

            questById = new Dictionary<string, QuestSO>();
            if (quests != null)
                foreach (var q in quests)
                    if (q != null && !string.IsNullOrEmpty(q.questId)) questById[q.questId] = q;
        }
    }

    // Runtime accessor — loads the baked registry from Resources once.
    public static class ItemRegistry
    {
        private static ItemRegistrySO cached;
        public static ItemRegistrySO Instance =>
            cached != null ? cached : (cached = Resources.Load<ItemRegistrySO>("ItemRegistry"));

        public static GearItemSO Gear(string id) => Instance != null ? Instance.GetGear(id) : null;
        public static MaterialItemSO Material(string id) => Instance != null ? Instance.GetMaterial(id) : null;
        public static QuestSO Quest(string id) => Instance != null ? Instance.GetQuest(id) : null;
    }
}
