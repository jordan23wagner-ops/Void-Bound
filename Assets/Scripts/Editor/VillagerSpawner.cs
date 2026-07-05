#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidBound.Combat;
using VoidBound.NPC;

namespace VoidBound.Editor
{
    // Populates the Homestead with a handful of ambient villagers (idempotent).
    // Each villager reuses the rigged Hero model (Walk/Idle already baked) under
    // a recoloured commoner tunic, driven by VillagerWander. One "attends" the
    // merchant stall; the rest stroll. All live under a "Villagers" root.
    public static class VillagerSpawner
    {
        private const string HeroFbx = "Assets/Art/Models/Hero.fbx";
        private const string HeroController = "Assets/Animation/HeroAnimator.controller";
        private const string MatDir = "Assets/Art/Materials/Villagers";
        private const string RootName = "Villagers";

        // x, z, wanderRadius, attend, faceX, faceZ, tunic
        private static readonly (float x, float z, float r, bool attend, float fx, float fz, string tunic)[] Specs =
        {
            (1.4f, 3.6f, 0f, true, 0.71f, -0.71f, "TunicBlue"), // customer beside the merchant (now by the square)
            (3.5f, -3.5f, 3.0f, false, 0f, 0f, "TunicBrown"),   // strolling the square
            (-4f, 3f, 3.0f, false, 0f, 0f, "TunicGreen"),
            (4f, 4.5f, 2.5f, false, 0f, 0f, "TunicRed"),        // near the homes
            (-3.5f, -3f, 3.0f, false, 0f, 0f, "TunicBlue"),
        };

        [MenuItem("VoidBound/Polish - Spawn Villagers")]
        public static void Run()
        {
            var heroFbx = AssetDatabase.LoadAssetAtPath<GameObject>(HeroFbx);
            var heroCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HeroController);
            if (heroFbx == null || heroCtrl == null)
            {
                Debug.LogError("[Villagers] Hero.fbx / HeroAnimator missing.");
                return;
            }

            var mats = CreateMaterials();
            string[] heroSlots = heroFbx.GetComponentInChildren<Renderer>()
                .sharedMaterials.Select(m => m != null ? m.name : "").ToArray();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;

            int i = 0;
            foreach (var s in Specs)
                BuildVillager(root, i++, s, heroFbx, heroCtrl, heroSlots, mats);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Villagers] Spawned {Specs.Length} villagers in Homestead.");
        }

        private static void BuildVillager(Transform root, int index,
            (float x, float z, float r, bool attend, float fx, float fz, string tunic) s,
            GameObject heroFbx, RuntimeAnimatorController heroCtrl, string[] heroSlots,
            System.Collections.Generic.Dictionary<string, Material> mats)
        {
            var go = new GameObject($"Villager_{index}");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(s.x, 0.1f, s.z);

            var cc = go.AddComponent<CharacterController>();
            cc.radius = 0.3f;
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            go.AddComponent<CharacterAnimation>();

            var w = go.AddComponent<VillagerWander>();
            var so = new SerializedObject(w);
            so.FindProperty("radius").floatValue = s.r;
            so.FindProperty("attend").boolValue = s.attend;
            so.FindProperty("faceDir").vector3Value = new Vector3(s.fx, 0f, s.fz);
            so.FindProperty("speed").floatValue = Random.Range(1.1f, 1.5f);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Rigged Hero model as the "Model" child (same 180° facing flip as the
            // player), recoloured to a commoner.
            var model = (GameObject)PrefabUtility.InstantiatePrefab(heroFbx);
            model.name = "Model";
            model.transform.SetParent(go.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = heroCtrl;
            anim.applyRootMotion = false;

            var smr = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null)
                smr.sharedMaterials = heroSlots.Select(slot =>
                    slot.Contains("Armor") ? mats[s.tunic]
                    : slot.Contains("Hair") ? mats["Hair"]
                    : mats["Skin"]).ToArray();
        }

        private static System.Collections.Generic.Dictionary<string, Material> CreateMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MatDir))
                AssetDatabase.CreateFolder("Assets/Art/Materials", "Villagers");

            var defs = new (string name, Color col)[]
            {
                ("Skin",       new Color(0.82f, 0.62f, 0.48f)),
                ("Hair",       new Color(0.22f, 0.15f, 0.10f)),
                ("TunicBrown", new Color(0.45f, 0.32f, 0.20f)),
                ("TunicBlue",  new Color(0.26f, 0.34f, 0.55f)),
                ("TunicGreen", new Color(0.30f, 0.42f, 0.26f)),
                ("TunicRed",   new Color(0.55f, 0.28f, 0.24f)),
            };

            var result = new System.Collections.Generic.Dictionary<string, Material>();
            foreach (var (name, col) in defs)
            {
                string path = $"{MatDir}/{name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(m, path);
                }
                m.color = col;
                EditorUtility.SetDirty(m);
                result[name] = m;
            }
            AssetDatabase.SaveAssets();
            return result;
        }
    }
}
#endif
