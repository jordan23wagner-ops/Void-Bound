#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Core;
using VoidBound.Data;
using VoidBound.Homestead;
using VoidBound.Skilling;

namespace VoidBound.Editor
{
    // Phase 8 (home features): places the three new stations from the GDD
    // skilling rework — Crafting Bench, Enchanted Chest, Reclaimer — spread
    // across the Homestead. Reuses existing Blender meshes + the Buildings
    // material palette; the Reclaimer reuses the Hero model like a villager.
    // Idempotent: rebuilds them all under a "NewStations" root each run, then
    // promotes the Enchanted Chest + Reclaimer to their real stations via their
    // own setup passes — so a re-run never leaves either reverted to a placeholder.
    public static class Phase8HomesteadStations
    {
        private const string ModelDir = "Assets/Art/Models/Buildings";
        private const string MatDir = "Assets/Art/Materials/Buildings";
        private const string VillagerMatDir = "Assets/Art/Materials/Villagers";
        private const string RootName = "NewStations";

        [MenuItem("VoidBound/Setup Phase 8 - New Homestead Stations")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;

            // ── Crafting Bench — industry corner by the Forge ───────────
            // Reuse the Merchant stall mesh as a work-table; cloth → wood so it
            // reads as a bench, not a market. Real, functional CraftingStation.
            var bench = BuildStation(root, "Crafting Bench", "Merchant", new Vector2(-11f, -10f),
                slot => slot.StartsWith("Cloth") ? "WoodLight" : slot);
            var cs = bench.AddComponent<CraftingStation>();
            ConfigureInteract(cs, "Craft", "Craft tools, ammo, refined materials and untradables.");
            SetString(cs, "stationId", "crafting_bench");
            SetEnum(cs, "skillType", (int)SkillType.Crafting);
            EnsureTrigger(bench);

            // ── Enchanted Chest — mystic quarter near the Shrine/Pool ───
            // Reuse the StorageChest mesh, recoloured to a glowing Void palette
            // (gold trim kept) to distinguish it from the plain bank chest. Given
            // a PlaceholderStation here only as the base state; EnchantedChestSetup
            // (run below) swaps it for the real EnchantedChestStation + UI.
            var chest = BuildStation(root, "Enchanted Chest", "StorageChest", new Vector2(23f, 9f),
                slot => slot.Contains("Gold") ? "Gold" : "Void");
            var ec = chest.AddComponent<PlaceholderStation>();
            ConfigureInteract(ec, "Upgrade", "Upgrade untradables with refined materials. (coming soon)");
            SetString(ec, "comingSoonNote", "Untradable upgrades coming soon.");
            EnsureTrigger(chest);

            // ── Reclaimer — a static NPC by the Fast Travel Portal / respawn ─
            BuildReclaimer(root, new Vector2(3f, -19f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Phase8] Placed Crafting Bench, Enchanted Chest, and Reclaimer under 'NewStations'.");

            // Promote the two placeholder stations to their real components + UI
            // wiring so this pass always ends fully wired — no manual re-runs of
            // the sub-setups needed. Both open/save the scene and are idempotent.
            EnchantedChestSetup.Run();
            ReclaimerSetup.Run();
            // Rebuilding the bench above gives it a bare CraftingStation; re-wire
            // its recipe list (CraftingContentSetup) so it's never left empty.
            CraftingContentSetup.Run();
            Debug.Log("[Phase8] Enchanted Chest + Reclaimer promoted; Crafting Bench recipes re-wired.");
        }

        public static void RunFromBatch() => Run();

        // ── helpers ────────────────────────────────────────────────────

        private static GameObject BuildStation(Transform root, string name, string fbxName,
            Vector2 pos, System.Func<string, string> slotToMat)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{fbxName}.fbx");
            GameObject go;
            if (prefab != null) { go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = name; }
            else { go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; }

            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(pos.x, 0f, pos.y);
            go.transform.rotation = Quaternion.Euler(0f, HomesteadLayout.FaceCentreYaw(pos), 0f);
            go.isStatic = true;

            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
            {
                var slots = mr.sharedMaterials;
                var newMats = new Material[slots.Length];
                for (int i = 0; i < slots.Length; i++)
                {
                    string slotName = slots[i] != null ? slots[i].name : "";
                    string matName = slotToMat != null ? slotToMat(slotName) : slotName;
                    newMats[i] = LoadMat(MatDir, matName) ?? LoadMat(MatDir, "WoodLight") ?? slots[i];
                }
                mr.sharedMaterials = newMats;
            }
            return go;
        }

        private static void BuildReclaimer(Transform root, Vector2 pos)
        {
            var heroFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Hero.fbx");
            var heroCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Animation/HeroAnimator.controller");

            var go = new GameObject("Reclaimer");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(pos.x, 0.1f, pos.y);
            var dir = new Vector3(-pos.x, 0f, -pos.y); // face the green (0,0)
            if (dir.sqrMagnitude > 0.001f)
                go.transform.rotation = Quaternion.LookRotation(dir.normalized);

            if (heroFbx != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(heroFbx);
                model.name = "Model";
                model.transform.SetParent(go.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // hero-model flip
                var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
                if (heroCtrl != null) anim.runtimeAnimatorController = heroCtrl;
                anim.applyRootMotion = false;

                var skin = LoadMat(VillagerMatDir, "Skin");
                var hair = LoadMat(VillagerMatDir, "Hair");
                var tunic = LoadMat(VillagerMatDir, "TunicBlue");
                var smr = model.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null)
                    smr.sharedMaterials = smr.sharedMaterials.Select(m =>
                    {
                        string n = m != null ? m.name : "";
                        return n.Contains("Armor") ? tunic : n.Contains("Hair") ? hair : skin;
                    }).ToArray();
            }

            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.9f, 0f);
            bc.size = new Vector3(1f, 1.8f, 1f);
            bc.isTrigger = true;

            var ps = go.AddComponent<PlaceholderStation>();
            ConfigureInteract(ps, "Reclaim", "Buy back untradable gear from a grave you abandoned, for a gold fee. (coming soon)");
            SetString(ps, "comingSoonNote", "Death reclaim coming soon.");
        }

        private static void EnsureTrigger(GameObject go)
        {
            if (go.GetComponent<Collider>() != null) return;
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.75f, 0f);
            bc.size = new Vector3(2f, 1.5f, 2f);
            bc.isTrigger = true;
        }

        private static void ConfigureInteract(Interactable comp, string prompt, string description)
        {
            var so = new SerializedObject(comp);
            so.FindProperty("interactPrompt").stringValue = prompt;
            so.FindProperty("interactRange").floatValue = 3f;
            so.FindProperty("tooltipDescription").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Component c, string prop, string value)
        {
            var so = new SerializedObject(c);
            so.FindProperty(prop).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Component c, string prop, int value)
        {
            var so = new SerializedObject(c);
            so.FindProperty(prop).enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material LoadMat(string dir, string name) =>
            string.IsNullOrEmpty(name) ? null : AssetDatabase.LoadAssetAtPath<Material>($"{dir}/{name}.mat");
    }
}
#endif
