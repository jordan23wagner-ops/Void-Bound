using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.Combat
{
    // Player death: keep the equipped weapon + 2 random other equipped items;
    // everything else (all other worn armor, the whole backpack, and all
    // currency) drops into a gravestone at the death spot. Respawn back at
    // Homestead at full HP after the death animation. Auto-attached to the
    // persisted player by GameBootstrap.
    public class PlayerDeath : MonoBehaviour
    {
        [SerializeField] private float respawnDelay = 2.2f;
        private const string HomeScene = "Homestead";
        private const int KeepExtraEquipped = 2;

        private Health health;
        private PlayerInventory inventory;
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
            int gold = 0, shards = 0;
            if (currency != null)
            {
                var taken = currency.TakeAll();
                gold = taken.gold;
                shards = taken.shards;
            }
            GraveManager.SetGrave(SceneManager.GetActiveScene().name, transform.position, dropped, gold, shards);

            if (controller != null) controller.enabled = false;
            if (combat != null) combat.enabled = false;
            StartCoroutine(Respawn());
        }

        // Keep weapon + 2 random other equipped slots; drop the rest of the
        // equipment plus the entire backpack. Returns the dropped gear.
        private List<GearItemSO> DropLoot()
        {
            var dropped = new List<GearItemSO>();
            if (inventory == null) return dropped;

            var equippedSlots = new List<EquipmentSlot>(inventory.Equipped.Keys);
            var keep = new HashSet<EquipmentSlot>();
            if (inventory.Equipped.ContainsKey(EquipmentSlot.Weapon))
                keep.Add(EquipmentSlot.Weapon);

            var others = equippedSlots.FindAll(s => s != EquipmentSlot.Weapon);
            for (int i = 0; i < others.Count; i++) // Fisher–Yates
            {
                int j = Random.Range(i, others.Count);
                (others[i], others[j]) = (others[j], others[i]);
            }
            int extra = Mathf.Min(KeepExtraEquipped, others.Count);
            for (int i = 0; i < extra; i++)
                keep.Add(others[i]);

            // Unequip everything not kept (moves it to the backpack)...
            foreach (var slot in equippedSlots)
                if (!keep.Contains(slot))
                    inventory.UnequipItem(slot);

            // ...then the whole backpack (originals + just-unequipped) drops.
            dropped.AddRange(inventory.Backpack);
            foreach (var item in dropped)
                inventory.RemoveItem(item);

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
