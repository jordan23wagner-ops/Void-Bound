using System;
using System.Collections.Generic;

namespace VoidBound.Save
{
    // Flat, JsonUtility-friendly snapshot of the player's progression. Items are
    // stored by itemId (+ upgrade tier) and resolved through ItemRegistry on load.
    [Serializable] public class MatSave { public string id; public int count; }
    [Serializable] public class GearSave { public string id; public int tier; } // tier = RarityTier as int
    [Serializable] public class SkillSave { public int type; public int level; public int xp; }
    [Serializable] public class ToolSave { public int skill; public int tier; }

    // The single unrecovered death-drop (GraveManager.pending), if any.
    [Serializable]
    public class GraveSave
    {
        public bool hasGrave;
        public string scene;
        public float x, y, z;
        public List<string> itemIds = new();
        public List<MatSave> materials = new();
        public int gold;
        public int shards;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int gold;
        public int voidShards;
        public List<MatSave> materials = new();
        public List<GearSave> backpack = new();
        public List<GearSave> equipped = new();
        public List<GearSave> bank = new();
        public List<SkillSave> skills = new();
        public List<ToolSave> tools = new();
        public bool hasPool;
        public int poolTier;

        // The unrecovered grave (if any) and untradable gear owed back by the
        // Reclaimer from a previously-abandoned grave. Both are pure runtime
        // state on GraveManager otherwise — without this they're lost on quit.
        public GraveSave grave = new();
        public List<string> reclaimable = new();
    }
}
