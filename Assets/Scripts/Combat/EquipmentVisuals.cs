using System.Collections.Generic;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.Combat
{
    // Renders equipped gear on a character's body. Purely an observer — never
    // touches equip/stat logic. On the Player it follows PlayerInventory.Equipped
    // live; on an enemy it shows the EnemyDefinitionSO's weapon + armor once.
    //
    // Gear models (GearItemSO.visualPrefab) are authored in two frames (see
    // Tools/build_equipment_models.py): weapons/shield in grip-space (attach at
    // hand sockets) and armor in hero-body space (attach at the root socket).
    // The Goblin body reuses the same hero-space armor, downscaled by its root
    // socket. All socket transforms are tuned constants below.
    public class EquipmentVisuals : MonoBehaviour
    {
        public enum BodyType { Hero, Goblin }

        [SerializeField] private BodyType bodyType = BodyType.Hero;
        [SerializeField] private EnemyDefinitionSO enemyDefinition; // null => player mode

        private struct Socket { public Vector3 pos, euler; public float scale; }

        private Transform rootSocket, handRSocket, handLSocket;
        private readonly Dictionary<EquipmentSlot, GameObject> shown = new();
        private PlayerInventory inventory;

        public void Configure(BodyType body, EnemyDefinitionSO enemyDef)
        {
            bodyType = body;
            enemyDefinition = enemyDef;
        }

        private void Awake()
        {
            BuildSockets();
        }

        private void Start()
        {
            if (enemyDefinition != null)
            {
                ShowEnemyGear();
                return;
            }

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

        // ── Socket setup ─────────────────────────────────────────
        private void BuildSockets()
        {
            Socket root, handR, handL;
            if (bodyType == BodyType.Hero)
            {
                root  = new Socket { pos = Vector3.zero, euler = Vector3.zero, scale = 1f };
                handR = new Socket { pos = new Vector3(0.30f, 0.76f, 0.02f), euler = Vector3.zero, scale = 1f };
                handL = new Socket { pos = new Vector3(-0.30f, 0.76f, 0.02f), euler = Vector3.zero, scale = 1f };
            }
            else // Goblin — hero-space armor downscaled; hands wider/lower
            {
                root  = new Socket { pos = Vector3.zero, euler = Vector3.zero, scale = 0.68f };
                handR = new Socket { pos = new Vector3(0.36f, 0.44f, -0.08f), euler = Vector3.zero, scale = 0.7f };
                handL = new Socket { pos = new Vector3(-0.36f, 0.44f, -0.08f), euler = Vector3.zero, scale = 0.7f };
            }

            rootSocket  = MakeSocket("Socket_Root", root);
            handRSocket = MakeSocket("Socket_HandR", handR);
            handLSocket = MakeSocket("Socket_HandL", handL);
        }

        private Transform MakeSocket(string name, Socket s)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = s.pos;
            go.transform.localEulerAngles = s.euler;
            go.transform.localScale = Vector3.one * s.scale;
            return go.transform;
        }

        private Transform SocketFor(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Weapon => handRSocket,
            EquipmentSlot.Shield => handLSocket,
            _ => rootSocket, // all armor authored in body-space
        };

        // ── Player (live) ────────────────────────────────────────
        private void SyncPlayer()
        {
            var equipped = inventory.Equipped;

            // Remove visuals whose slot is no longer equipped
            var toRemove = new List<EquipmentSlot>();
            foreach (var kv in shown)
                if (!equipped.ContainsKey(kv.Key) || equipped[kv.Key] == null)
                    toRemove.Add(kv.Key);
            foreach (var slot in toRemove) RemoveVisual(slot);

            // Add/replace visuals for equipped items
            foreach (var kv in equipped)
                EnsureVisual(kv.Key, kv.Value);
        }

        // ── Enemy (static) ───────────────────────────────────────
        private void ShowEnemyGear()
        {
            if (enemyDefinition.weapon != null)
                EnsureVisual(EquipmentSlot.Weapon, enemyDefinition.weapon);
            if (enemyDefinition.armor != null)
                foreach (var piece in enemyDefinition.armor)
                    if (piece != null) EnsureVisual(piece.slot, piece);
        }

        // ── Instance management ──────────────────────────────────
        private void EnsureVisual(EquipmentSlot slot, GearItemSO item)
        {
            if (item == null || item.visualPrefab == null) { RemoveVisual(slot); return; }

            // Rebuild only if the shown item for this slot changed
            if (shown.TryGetValue(slot, out var existing))
            {
                if (existing != null && existing.name == VisualName(item)) return;
                RemoveVisual(slot);
            }

            var socket = SocketFor(slot);
            var go = Instantiate(item.visualPrefab, socket);
            go.name = VisualName(item);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

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
            {
                var mats = r.materials; // per-instance copies
                foreach (var m in mats)
                    if (m != null && m.name.StartsWith("Main"))
                        m.color = color;
            }
        }
    }
}
