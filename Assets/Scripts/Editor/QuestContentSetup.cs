#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidBound.Data;
using VoidBound.Homestead;
using VoidBound.Quests;
using VoidBound.UI;

namespace VoidBound.Editor
{
    // Phase 9: the first quest — "The Warden's Charge" — which threads the whole
    // core loop into one objective chain (gather logs → fletch arrows → slay
    // goblins → turn in for a reward). Authors the QuestSO from real asset ids,
    // wires the player/HUD quest components, plants the Warden NPC on the plaza,
    // and bakes the item registry so the quest resolves on load. Idempotent.
    public static class QuestContentSetup
    {
        private const string QuestDir  = "Assets/ScriptableObjects/Quests";
        private const string QuestPath = QuestDir + "/warden_charge.asset";
        private const string WardenName = "The Warden";

        [MenuItem("VoidBound/Setup Phase 9 - First Quest")]
        public static void Setup()
        {
            var quest = CreateOrUpdateQuest();
            if (quest == null) return;

            if (SceneManager.GetActiveScene().name != "Homestead")
            {
                Debug.LogError("[Phase9] Open Homestead.unity first (quest asset was still authored).");
                return;
            }
            WireScene(quest);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Phase9] First quest wired. Save the scene (Ctrl+S). Registry baked.");
        }

        // Batch entry point: author the quest, open Homestead, wire it, save, bake.
        public static void RunFromBatch()
        {
            CreateOrUpdateQuest();
            EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            var quest = AssetDatabase.LoadAssetAtPath<QuestSO>(QuestPath);
            WireScene(quest);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        // ═══════════════════════════════════════════════════════════

        private static QuestSO CreateOrUpdateQuest()
        {
            var log   = FindMaterial("log_0");   // Rough Logs (T1)
            var arrows = FindMaterial("arrows");
            if (log == null || arrows == null)
            {
                Debug.LogError("[Phase9] Missing base materials (log_0 / arrows). Bake content first.");
                return null;
            }

            if (!System.IO.Directory.Exists(QuestDir)) System.IO.Directory.CreateDirectory(QuestDir);

            var quest = AssetDatabase.LoadAssetAtPath<QuestSO>(QuestPath);
            if (quest == null)
            {
                quest = ScriptableObject.CreateInstance<QuestSO>();
                AssetDatabase.CreateAsset(quest, QuestPath);
            }

            quest.questId = "warden_charge";
            quest.title = "The Warden's Charge";
            quest.giverName = "The Warden";
            quest.offerText =
                "You there — new blood. The Bleak creeps closer each night and the " +
                "watch grows thin. Prove your worth: bring me timber for the palisade, " +
                "fletch your own arrows, and thin the goblin scouts massing in the " +
                "Ashfields. Do that, and the hold will call you one of its own.";
            quest.activeText =
                "The palisade won't raise itself, and those goblins still draw breath. " +
                "Come back when the work is done.";
            quest.turnInText =
                "Timber, arrows, and goblin blood on the ash. You'll do. Take this — " +
                "you've earned a place at the fire.";

            quest.objectives = new[]
            {
                new QuestObjective { type = QuestObjectiveType.Gather, targetId = "log_0",  required = 3, label = "Chop Rough Logs" },
                new QuestObjective { type = QuestObjectiveType.Craft,  targetId = "arrows", required = 1, label = "Fletch Arrows at the Bench" },
                new QuestObjective { type = QuestObjectiveType.Kill,   targetId = "",       required = 4, label = "Slay Goblins in the Ashfields" },
            };

            quest.reward = new QuestReward
            {
                gold = 60,
                voidShards = 2,
                xpSkill = SkillType.CombatSTR,
                xpAmount = 150,
                rewardMaterial = arrows,
                rewardMaterialQty = 15,
                rewardGear = null,
            };

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Phase9] Authored quest '{quest.title}' ({QuestPath}).");
            return quest;
        }

        private static void WireScene(QuestSO quest)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) { Debug.LogError("[Phase9] No Player in scene."); return; }
            if (player.GetComponent<PlayerQuests>() == null) player.AddComponent<PlayerQuests>();

            var hud = Object.FindAnyObjectByType<HUDManager>()?.gameObject;
            if (hud == null) { Debug.LogError("[Phase9] No HUDCanvas/HUDManager in scene."); return; }
            if (hud.GetComponent<QuestGiverUI>() == null) hud.AddComponent<QuestGiverUI>();
            if (hud.GetComponent<QuestTrackerUI>() == null) hud.AddComponent<QuestTrackerUI>();

            BuildWarden(quest);

            // Save-load resolves activeQuestId → QuestSO through the registry.
            ItemRegistryBaker.Bake();
        }

        // Plants (or updates) the Warden NPC on the plaza, right in the player's
        // spawn sightline (spawn is (0,0,-5) facing +Z toward the green).
        private static void BuildWarden(QuestSO quest)
        {
            // Rebuild fresh each run so visual tweaks take effect (idempotent).
            var old = GameObject.Find(WardenName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(WardenName);
            BuildWardenVisual(root);
            root.transform.position = new Vector3(4f, 0f, -2f);
            root.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // face the spawning player

            var col = root.GetComponent<BoxCollider>();
            if (col == null) col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.size = new Vector3(2.2f, 2.2f, 2.2f);
            col.isTrigger = true;

            var station = root.GetComponent<QuestGiverStation>();
            if (station == null) station = root.AddComponent<QuestGiverStation>();
            station.SetQuest(quest);

            var so = new SerializedObject(station);
            so.FindProperty("interactPrompt").stringValue = "Speak";
            so.FindProperty("interactRange").floatValue = 3f;
            var tip = so.FindProperty("tooltipDescription");
            if (tip != null) tip.stringValue = "The Warden — has need of a capable hand.";
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Phase9] Warden placed at (4,0,-2) with quest assigned.");
        }

        private static void BuildWardenVisual(GameObject root)
        {
            var robe = new Color(0.13f, 0.12f, 0.17f);  // dark charcoal-violet robe
            var hood = new Color(0.06f, 0.055f, 0.09f);
            var gold = new Color(0.98f, 0.78f, 0.30f);

            // Rigged Hero body, reskinned dark — matches the model + animation
            // standard used for the player/goblins/boss (idles via HeroAnimator).
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Hero.fbx");
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/HeroAnimator.controller");
            if (fbx != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                model.name = "Model";
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                model.transform.localScale = Vector3.one;

                var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
                if (ctrl != null) anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;

                // Per-slot reskin (Hero FBX order: Skin, Hair, Armor): a gaunt pale
                // face + dark hair over a dark robe — a wan, hooded-looking figure.
                var faceMat = SolidMat(new Color(0.58f, 0.55f, 0.52f));
                var hairMat = SolidMat(hood);
                var robeMat = SolidMat(robe);
                var smr = model.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    int n = smr.sharedMaterials.Length;
                    smr.sharedMaterials = n >= 3
                        ? new[] { faceMat, hairMat, robeMat }
                        : System.Linq.Enumerable.Repeat(robeMat, n).ToArray();
                }
            }
            else
            {
                MakePart(PrimitiveType.Capsule, root.transform, "Body",
                    new Vector3(0f, 1.0f, 0f), new Vector3(0.85f, 0.95f, 0.85f), robe, false);
            }

            // A dark hood peak above/behind the head so it reads as cowled.
            MakePart(PrimitiveType.Sphere, root.transform, "Hood",
                new Vector3(0f, 1.78f, -0.06f), new Vector3(0.42f, 0.5f, 0.42f), hood, false);

            // Floating gold quest marker (a rotated cube = diamond) above the head.
            var marker = MakePart(PrimitiveType.Cube, root.transform, "QuestMarker",
                new Vector3(0f, 2.4f, 0f), new Vector3(0.26f, 0.26f, 0.26f), gold, true);
            marker.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            marker.AddComponent<VoidBound.Homestead.QuestMarkerBob>();
        }

        private static Material SolidMat(Color color) =>
            new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = color };

        private static GameObject MakePart(PrimitiveType type, Transform parent, string name,
            Vector3 lpos, Vector3 lscale, Color color, bool emissive)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lpos;
            go.transform.localScale = lscale;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2.2f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static MaterialItemSO FindMaterial(string itemId)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:MaterialItemSO"))
            {
                var so = AssetDatabase.LoadAssetAtPath<MaterialItemSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (so != null && so.itemId == itemId) return so;
            }
            return null;
        }
    }
}
#endif
