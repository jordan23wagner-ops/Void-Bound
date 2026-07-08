using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Combat
{
    // Shows a tool in the player's hand and plays a gather motion while skilling
    // (§5/§6). Tools have no meshes of their own (they're a PlayerTools tier), so
    // this builds simple low-poly rod/pickaxe/axe/sickle props from primitives and
    // parents them to the Hand_R bone (same attach + rig-scale compensation as
    // EquipmentVisuals' held weapons). The gather motion reuses the existing Attack
    // swing (mine/chop/gather) or Cast (fishing) clips — no new animator state.
    // ResourceNode calls PlayGather() on each harvest.
    public class GatherAnimator : MonoBehaviour
    {
        // Held-tool grip pose on the hand bone (matches EquipmentVisuals' weapon pose).
        private static readonly Vector3 GripPos = new(0f, 0.02f, 0f);
        private static readonly Vector3 GripEuler = new(60f, 180f, 180f);
        private const float BodyScale = 1f;   // player is the Hero body
        private const float ShowSeconds = 5f;  // keep the tool out while actively skilling

        private CharacterAnimation anim;
        private Transform hand;
        private readonly Dictionary<SkillType, GameObject> tools = new();
        private GameObject active;
        private Coroutine hideCo;

        private void Start()
        {
            anim = GetComponent<CharacterAnimation>();
            hand = FindBone("Hand_R");
            if (hand == null) return;

            foreach (SkillType s in new[] { SkillType.Woodcutting, SkillType.Mining, SkillType.Fishing, SkillType.Gathering })
            {
                var tool = BuildTool(s);
                Attach(tool.transform, hand);
                tool.SetActive(false);
                tools[s] = tool;
            }
        }

        // Play a gather beat for `skill`: show its tool, swing/cast, keep it out
        // briefly so continuous skilling doesn't flicker the prop in and out.
        public void PlayGather(SkillType skill)
        {
            if (anim != null)
            {
                if (skill == SkillType.Fishing) anim.TriggerCast();
                else anim.TriggerAttack();
            }

            if (hand == null || !tools.TryGetValue(skill, out var tool)) return;
            if (active != null && active != tool) active.SetActive(false);
            active = tool;
            tool.SetActive(true);

            if (hideCo != null) StopCoroutine(hideCo);
            hideCo = StartCoroutine(HideAfter(ShowSeconds));
        }

        private IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (active != null) active.SetActive(false);
            active = null;
            hideCo = null;
        }

        // ── attach + build ─────────────────────────────────────────────

        private Transform FindBone(string boneName)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == boneName) return t;
            return null;
        }

        // Parent to the hand with the imported rig's ~100x bone scale compensated,
        // so the prop lands at BodyScale with the grip pose (cf. EquipmentVisuals).
        private static void Attach(Transform prop, Transform bone)
        {
            prop.SetParent(bone, false);
            float bs = bone.lossyScale.x <= 0.0001f ? 1f : bone.lossyScale.x;
            prop.localPosition = GripPos / bs;
            prop.localRotation = Quaternion.Euler(GripEuler);
            prop.localScale = Vector3.one * (BodyScale / bs);
        }

        // Prefer the real low-poly FBX (Resources/GatherTools, built by
        // Tools/build_gather_tools.py); fall back to primitives if it's missing.
        private static GameObject BuildTool(SkillType skill)
        {
            string fbx = FbxName(skill);
            var prefab = fbx != null ? Resources.Load<GameObject>("GatherTools/" + fbx) : null;
            if (prefab != null)
            {
                var inst = Instantiate(prefab);
                inst.name = "GatherTool_" + skill;
                RestyleTool(inst);
                return inst;
            }
            return BuildToolPrimitive(skill);
        }

        private static string FbxName(SkillType skill) => skill switch
        {
            SkillType.Woodcutting => "axe",
            SkillType.Mining => "pickaxe",
            SkillType.Fishing => "rod",
            SkillType.Gathering => "sickle",
            _ => null
        };

        // Recolour the FBX's "Wood"/"Metal" slots to URP materials at runtime.
        private static void RestyleTool(GameObject go)
        {
            var wood = Mat(new Color(0.40f, 0.28f, 0.16f));
            var metal = Mat(new Color(0.62f, 0.64f, 0.68f));
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
            {
                var slots = mr.sharedMaterials;
                var outMats = new Material[slots.Length];
                for (int i = 0; i < slots.Length; i++)
                {
                    string n = slots[i] != null ? slots[i].name : "";
                    outMats[i] = n.Contains("Metal") ? metal : wood;
                }
                mr.sharedMaterials = outMats;
            }
        }

        private static GameObject BuildToolPrimitive(SkillType skill)
        {
            var root = new GameObject("GatherTool_" + skill);
            var wood = Mat(new Color(0.40f, 0.28f, 0.16f));
            var metal = Mat(new Color(0.62f, 0.64f, 0.68f));

            switch (skill)
            {
                case SkillType.Woodcutting: // axe: handle + a side blade
                    Part(root, wood, new Vector3(0f, 0.25f, 0f), new Vector3(0.04f, 0.5f, 0.04f));
                    Part(root, metal, new Vector3(0.09f, 0.47f, 0f), new Vector3(0.18f, 0.14f, 0.03f));
                    break;
                case SkillType.Mining: // pickaxe: handle + a horizontal pick bar
                    Part(root, wood, new Vector3(0f, 0.25f, 0f), new Vector3(0.04f, 0.5f, 0.04f));
                    Part(root, metal, new Vector3(0f, 0.5f, 0f), new Vector3(0.4f, 0.05f, 0.05f));
                    break;
                case SkillType.Fishing: // rod: one long tapering pole
                    Part(root, wood, new Vector3(0f, 0.38f, 0f), new Vector3(0.025f, 0.8f, 0.025f));
                    break;
                default: // Gathering — sickle: handle + a blade
                    Part(root, wood, new Vector3(0f, 0.2f, 0f), new Vector3(0.04f, 0.4f, 0.04f));
                    Part(root, metal, new Vector3(0.1f, 0.42f, 0f), new Vector3(0.2f, 0.04f, 0.03f));
                    break;
            }
            return root;
        }

        private static void Part(GameObject root, Material mat, Vector3 pos, Vector3 scale)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = c.GetComponent<Collider>();
            if (col != null) Destroy(col);
            c.transform.SetParent(root.transform, false);
            c.transform.localPosition = pos;
            c.transform.localScale = scale;
            c.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Material Mat(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            m.color = c;
            return m;
        }
    }
}
