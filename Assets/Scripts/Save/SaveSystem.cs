using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Homestead;
using VoidBound.Inventory;
using VoidBound.Skilling;

namespace VoidBound.Save
{
    // Single-slot JSON save of the player's core progression (§ save system).
    // Gathers state from the persisted player + the Pool station, writes to
    // persistentDataPath, and restores it on load. Items are keyed by itemId and
    // resolved through ItemRegistry (bake it via VoidBound > Bake Item Registry).
    public static class SaveSystem
    {
        private static string FilePath => Path.Combine(Application.persistentDataPath, "voidbound_save.json");
        public static bool HasSave => File.Exists(FilePath);

        // Automatic save/load (boot-load, quit-save, zone-travel save) is OFF in
        // the editor by default so dev play sessions don't clobber each other's
        // state; standalone builds always autosave. Manual Save/Load and the
        // New Game dev button ignore this flag.
        public static bool AutoEnabled = !Application.isEditor;

        public static void Save(GameObject player)
        {
            if (player == null) return;
            var d = new SaveData();

            var cur = player.GetComponent<PlayerCurrency>();
            if (cur != null) { d.gold = cur.Gold; d.voidShards = cur.VoidShards; }

            var mat = player.GetComponent<MaterialInventory>();
            if (mat != null)
                foreach (var kv in mat.GetAllMaterials())
                    d.materials.Add(new MatSave { id = kv.Key, count = kv.Value });

            var upg = player.GetComponent<PlayerUpgrades>();
            var inv = player.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                foreach (var it in inv.Backpack) if (it != null) d.backpack.Add(ToGear(it, upg));
                foreach (var kv in inv.Equipped) if (kv.Value != null) d.equipped.Add(ToGear(kv.Value, upg));
            }

            var store = player.GetComponent<PlayerStorage>();
            if (store != null)
                foreach (var it in store.Stored) if (it != null) d.bank.Add(ToGear(it, upg));

            var skills = player.GetComponent<PlayerSkills>();
            if (skills != null)
                foreach (var kv in skills.Skills)
                    d.skills.Add(new SkillSave { type = (int)kv.Key, level = kv.Value.level, xp = kv.Value.currentXP });

            var tools = player.GetComponent<PlayerTools>();
            if (tools != null)
                foreach (var kv in tools.Owned) // persist every owned tool, incl. tier-0
                    d.tools.Add(new ToolSave { skill = (int)kv.Key, tier = (int)kv.Value });

            var pool = Object.FindAnyObjectByType<PoolStation>();
            if (pool != null) { d.hasPool = true; d.poolTier = pool.CurrentTier; }

            File.WriteAllText(FilePath, JsonUtility.ToJson(d, true));
            Debug.Log($"[Save] Wrote {FilePath}");
        }

        public static void Load(GameObject player)
        {
            if (player == null || !HasSave) return;
            var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            if (d == null) return;

            var cur = player.GetComponent<PlayerCurrency>();
            if (cur != null) { cur.TakeAll(); cur.AddGold(d.gold); cur.AddVoidShards(d.voidShards); }

            var mat = player.GetComponent<MaterialInventory>();
            if (mat != null)
            {
                mat.TakeAll(); // clear
                foreach (var m in d.materials)
                {
                    var so = ItemRegistry.Material(m.id);
                    if (so != null) mat.AddMaterial(so, m.count);
                }
            }

            // Upgrade tiers must be applied BEFORE equipping so worn stats scale right.
            var upg = player.GetComponent<PlayerUpgrades>();
            var inv = player.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                var backpack = Resolve(d.backpack, upg);
                var equipped = Resolve(d.equipped, upg);
                inv.LoadState(backpack, equipped);
            }

            var store = player.GetComponent<PlayerStorage>();
            if (store != null) store.LoadState(Resolve(d.bank, upg));

            var skills = player.GetComponent<PlayerSkills>();
            if (skills != null)
                foreach (var s in d.skills) skills.LoadProgress((SkillType)s.type, s.level, s.xp);

            var tools = player.GetComponent<PlayerTools>();
            if (tools != null)
                foreach (var t in d.tools) tools.LoadTier((SkillType)t.skill, (RarityTier)t.tier);

            if (d.hasPool)
            {
                var pool = Object.FindAnyObjectByType<PoolStation>();
                if (pool != null) pool.SetTier(d.poolTier);
            }

            Debug.Log($"[Save] Loaded {FilePath}");
        }

        public static void Delete() { if (HasSave) File.Delete(FilePath); }

        // Reset the live session's core progression to a blank slate (dev "New
        // Game"). Pair with Delete() so the next boot is fresh too.
        public static void Wipe(GameObject player)
        {
            if (player == null) return;

            var cur = player.GetComponent<PlayerCurrency>();
            if (cur != null) cur.TakeAll();

            var mat = player.GetComponent<MaterialInventory>();
            if (mat != null) mat.TakeAll();

            var empty = new List<GearItemSO>();
            var inv = player.GetComponent<PlayerInventory>();
            if (inv != null) inv.LoadState(empty, empty);

            var store = player.GetComponent<PlayerStorage>();
            if (store != null) store.LoadState(empty);

            var skills = player.GetComponent<PlayerSkills>();
            if (skills != null)
                foreach (SkillType s in System.Enum.GetValues(typeof(SkillType)))
                    skills.LoadProgress(s, 1, 0);

            var tools = player.GetComponent<PlayerTools>();
            if (tools != null)
                foreach (SkillType s in System.Enum.GetValues(typeof(SkillType)))
                    tools.LoadTier(s, RarityTier.Common);

            var pool = Object.FindAnyObjectByType<PoolStation>();
            if (pool != null) pool.SetTier(0);
        }

        // Resolve saved gear entries to their SO assets, applying their upgrade tier.
        private static List<GearItemSO> Resolve(List<GearSave> entries, PlayerUpgrades upg)
        {
            var list = new List<GearItemSO>();
            if (entries == null) return list;
            foreach (var g in entries)
            {
                var so = ItemRegistry.Gear(g.id);
                if (so == null) continue;
                if (upg != null && g.tier != (int)so.rarity) upg.SetTier(so.itemId, (RarityTier)g.tier);
                list.Add(so);
            }
            return list;
        }

        private static GearSave ToGear(GearItemSO it, PlayerUpgrades upg) =>
            new GearSave { id = it.itemId, tier = (int)(upg != null ? upg.GetTier(it) : it.rarity) };
    }
}
