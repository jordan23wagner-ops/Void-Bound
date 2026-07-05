using System.Collections.Generic;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.Combat
{
    // Renders equipped gear on a character's body, attached to the animated
    // skeleton so it moves with the animation (weapon swings with the Attack
    // clip, helm bobs with the head, etc.). Observer only — never touches
    // equip/stat logic. Player mode follows PlayerInventory.Equipped live; enemy
    // mode shows the EnemyDefinitionSO's weapon + armor once.
    //
    // Two attach modes (models from Tools/build_equipment_models.py):
    //  - weapons/shield are grip-space → parented to the hand bone with a tuned
    //    local offset.
    //  - armor is hero-body space → placed at the character root (correct rest
    //    pose) then reparented to its bone keeping world position, so it sits
    //    right AND follows that bone. Goblin armor is downscaled to fit.
    public class EquipmentVisuals : MonoBehaviour
    {
        public enum BodyType { Hero, Goblin }

        [SerializeField] private BodyType bodyType = BodyType.Hero;
        [SerializeField] private EnemyDefinitionSO enemyDefinition; // null => player mode

        private float BodyScale => bodyType == BodyType.Goblin ? 0.68f : 1f;

        private readonly Dictionary<EquipmentSlot, GameObject> shown = new();
        private readonly Dictionary<string, Transform> bones = new();
        private PlayerInventory inventory;

        public void Configure(BodyType body, EnemyDefinitionSO enemyDef)
        {
            bodyType = body;
            enemyDefinition = enemyDef;
        }

        private void Start()
        {
            ResolveBones();

            if (enemyDefinition != null) { ShowEnemyGear(); return; }

            inventory = GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.OnInventoryChanged += SyncPlayer;
                SyncPlayer();
            }
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.OnInventoryChanged -= SyncPlayer;
        }

        private void ResolveBones()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (!bones.ContainsKey(t.name)) bones[t.name] = t;
        }

        private Transform Bone(string n) => bones.TryGetValue(n, out var t) ? t : transform;

        // Per-body local offset for grip-space weapons/shield on the hand bone.
        // Tuned so the blade reads as held; the hand bone points down the forearm.
        private (Vector3 pos, Vector3 euler) HandOffset(bool shield)
        {
            // Local-to-hand-bone rotations (tuned for the +Z-facing model — see
            // CharacterModelSwap's 180° flip): weapon blade points up and slightly
            // forward, shield face points forward and sits upright. Follows the
            // hand through the animation.
            return shield ? (new Vector3(0f, 0.02f, 0.06f), new Vector3(0f, 0f, 180f))
                          : (new Vector3(0f, 0.02f, 0f), new Vector3(20f, 180f, 180f));
        }

        private (Transform bone, bool grip) Target(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Weapon => (Bone("Hand_R"), true),
            EquipmentSlot.Shield => (Bone("Hand_L"), true),
            EquipmentSlot.Helm   => (Bone("Head"), false),
            EquipmentSlot.Body   => (Bone("Chest"), false),
            EquipmentSlot.Legs   => (Bone("Hips"), false),
            EquipmentSlot.Boots  => (Bone("Hips"), false),
            EquipmentSlot.Gloves => (Bone("Chest"), false),
            EquipmentSlot.Cape   => (Bone("Chest"), false),
            EquipmentSlot.Amulet => (Bone("Neck"), false),
            _ => (transform, false),
        };

        private void SyncPlayer()
        {
            var equipped = inventory.Equipped;
            var toRemove = new List<EquipmentSlot>();
            foreach (var kv in shown)
                if (!equipped.ContainsKey(kv.Key) || equipped[kv.Key] == null)
                    toRemove.Add(kv.Key);
            foreach (var slot in toRemove) RemoveVisual(slot);
            foreach (var kv in equipped) EnsureVisual(kv.Key, kv.Value);
        }

        private void ShowEnemyGear()
        {
            if (enemyDefinition.weapon != null)
                EnsureVisual(EquipmentSlot.Weapon, enemyDefinition.weapon);
            if (enemyDefinition.armor != null)
                foreach (var piece in enemyDefinition.armor)
                    if (piece != null) EnsureVisual(piece.slot, piece);
        }

        private void EnsureVisual(EquipmentSlot slot, GearItemSO item)
        {
            if (item == null || item.visualPrefab == null) { RemoveVisual(slot); return; }

            if (shown.TryGetValue(slot, out var existing))
            {
                if (existing != null && existing.name == VisualName(item)) return;
                RemoveVisual(slot);
            }

            var (bone, grip) = Target(slot);
            var go = Instantiate(item.visualPrefab);
            go.name = VisualName(item);

            if (grip)
            {
                go.transform.SetParent(bone, false);
                var (pos, euler) = HandOffset(slot == EquipmentSlot.Shield);
                // The imported armature root ("Rig") carries a ~100x unit-scale,
                // so bones have a large lossyScale. Compensate so the gear ends
                // up at BodyScale in world space with a sensible world offset.
                float bs = bone.lossyScale.x <= 0.0001f ? 1f : bone.lossyScale.x;
                go.transform.localPosition = pos / bs;
                go.transform.localRotation = Quaternion.Euler(euler);
                go.transform.localScale = Vector3.one * (BodyScale / bs);
            }
            else
            {
                // Body-space: correct rest placement at the root, then follow the bone.
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * BodyScale;
                go.transform.SetParent(bone, worldPositionStays: true);
            }

            TintMain(go, RarityVisualEffects.GetRarityColor(item.rarity));
            shown[slot] = go;
        }

        private void RemoveVisual(EquipmentSlot slot)
        {
            if (shown.TryGetValue(slot, out var go))
            {
                if (go != null) Destroy(go);
                shown.Remove(slot);
            }
        }

        private static string VisualName(GearItemSO item) => "Gear_" + item.itemId;

        private static void TintMain(GameObject go, Color color)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials)
                    if (m != null && m.name.StartsWith("Main"))
                        m.color = color;
        }
    }
}
