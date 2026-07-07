using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.Skilling;

namespace VoidBound.Combat
{
    // Player death: keep your 3 most valuable items (by goldValue) across
    // everything carried — equipped and backpack; everything else (the rest of
    // your gear, the whole backpack, and all currency) drops into a gravestone
    // at the death spot. Respawn back at Homestead at full HP after the death
    // animation. Auto-attached to the persisted player by GameBootstrap.
    public class PlayerDeath : MonoBehaviour
    {
        [SerializeField] private float respawnDelay = 2.2f;
        private const string HomeScene = "Homestead";
        public const int KeepCount = 3;

        private Health health;
        private PlayerInventory inventory;
        private MaterialInventory materialInv;
        private PlayerCurrency currency;
        private CharacterAnimation anim;
        private Core.PlayerController controller;
        private PlayerCombat combat;
        private CharacterController charController;
        private bool dying;

        private void Awake()
        {
            health = GetComponent<Health>();
            inventory = GetComponent<PlayerInventory>();
            materialInv = GetComponent<MaterialInventory>();
            currency = GetComponent<PlayerCurrency>();
            anim = GetComponent<CharacterAnimation>();
            controller = GetComponent<Core.PlayerController>();
            combat = GetComponent<PlayerCombat>();
            charController = GetComponent<CharacterController>();
        }

        private void OnEnable() { if (health != null) health.OnDeath += Die; }
        private void OnDisable() { if (health != null) health.OnDeath -= Die; }

        private void Die()
        {
            if (dying) return;
            dying = true;

            var dropped = DropLoot();
            var droppedMaterials = materialInv != null
                ? materialInv.TakeAll()
                : new List<MaterialInventory.Stack>();
            int gold = 0, shards = 0;
            if (currency != null)
            {
                var taken = currency.TakeAll();
                gold = taken.gold;
                shards = taken.shards;
            }
            GraveManager.SetGrave(SceneManager.GetActiveScene().name, transform.position, dropped, droppedMaterials, gold, shards);

            if (controller != null) controller.enabled = false;
            if (combat != null) combat.enabled = false;
            StartCoroutine(Respawn());
        }

        // Everything carried (equipped + backpack) ranked most-valuable-first by
        // goldValue. The first KeepCount survive death; the rest drop (§4A). Single
        // source of truth shared by DropLoot and the death preview so the two can
        // never disagree. Ties are arbitrary (equal value, so equivalent).
        private static List<(GearItemSO item, EquipmentSlot slot, bool equipped)> RankCarried(PlayerInventory inventory)
        {
            var entries = new List<(GearItemSO item, EquipmentSlot slot, bool equipped)>();
            if (inventory == null) return entries;

            foreach (var kv in inventory.Equipped)
                if (kv.Value != null) entries.Add((kv.Value, kv.Key, true));
            foreach (var item in inventory.Backpack)
                if (item != null) entries.Add((item, default, false));

            entries.Sort((a, b) => b.item.goldValue.CompareTo(a.item.goldValue));
            return entries;
        }

        // The items you'd keep if you died right now — the KeepCount most valuable
        // across equipped + backpack (§4A). Drives the "kept on death" preview.
        public static List<GearItemSO> PreviewKept(PlayerInventory inventory)
        {
            var ranked = RankCarried(inventory);
            int keep = Mathf.Min(KeepCount, ranked.Count);
            var kept = new List<GearItemSO>(keep);
            for (int i = 0; i < keep; i++) kept.Add(ranked[i].item);
            return kept;
        }

        // Keep the KeepCount most valuable; drop the rest. Equipped items that
        // aren't kept are unequipped first so their stat bonuses come off before
        // they drop. Returns the dropped gear.
        private List<GearItemSO> DropLoot()
        {
            var dropped = new List<GearItemSO>();
            if (inventory == null) return dropped;

            var ranked = RankCarried(inventory);
            int keep = Mathf.Min(KeepCount, ranked.Count);
            for (int i = keep; i < ranked.Count; i++)
            {
                var e = ranked[i];
                if (e.equipped) inventory.UnequipItem(e.slot); // moves it to the backpack
                if (inventory.RemoveItem(e.item)) dropped.Add(e.item);
            }

            return dropped;
        }

        private IEnumerator Respawn()
        {
            yield return new WaitForSeconds(respawnDelay);

            if (SceneManager.GetActiveScene().name != HomeScene)
            {
                // GameBootstrap repositions the persisted player to Homestead's
                // PlayerSpawnPoint on load.
                SceneManager.LoadScene(HomeScene);
                yield return null; // let the load + reposition happen
                yield return null;
            }
            else
            {
                var spawn = GameObject.Find("PlayerSpawnPoint");
                if (spawn != null) TeleportTo(spawn.transform.position);
            }

            health.Revive();
            anim?.Revive();
            if (controller != null) controller.enabled = true;
            if (combat != null) combat.enabled = true;
            // Now that the player has respawned away from the death spot, reveal
            // the gravestone (same-scene deaths; cross-scene ones appear when the
            // origin zone is next loaded).
            GraveManager.RevealGrave();
            dying = false;
        }

        private void TeleportTo(Vector3 pos)
        {
            if (charController != null) charController.enabled = false;
            transform.position = pos;
            if (charController != null) charController.enabled = true;
        }
    }
}
