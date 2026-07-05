using UnityEngine;
using UnityEngine.UI;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    public class DevToolsPanel : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Health playerHealth;
        [SerializeField] private GearItemSO[] testGearPool;
        [SerializeField] private Button giveGearButton;
        [SerializeField] private Button killAllButton;
        [SerializeField] private Button godModeButton;
        [SerializeField] private Button giveGoldButton;

        private bool godMode;

        private void Start()
        {
            if (giveGearButton != null) giveGearButton.onClick.AddListener(GiveTestGear);
            if (killAllButton != null) killAllButton.onClick.AddListener(KillAllEnemies);
            if (godModeButton != null) godModeButton.onClick.AddListener(ToggleGodMode);
            if (giveGoldButton != null) giveGoldButton.onClick.AddListener(GiveGold);
        }

        // Equip the full test-gear kit straight into its slots (the pool is one
        // item per slot), so the player spawns fully geared. Called by the editor
        // dev-play setup on entering play mode — safe on this (inactive) panel
        // since it's a plain method call, not a coroutine.
        public void EquipTestGearKit()
        {
            if (inventory == null || testGearPool == null) return;
            foreach (var item in testGearPool)
                if (item != null) inventory.EquipItem(item);
            Debug.Log("[DevTools] Auto-equipped test gear kit.");
        }

        public void GiveTestGear()
        {
            if (inventory == null || testGearPool == null) return;
            foreach (var item in testGearPool)
            {
                if (item != null)
                    inventory.AddItem(item);
            }
            Debug.Log("[DevTools] Test gear added to backpack.");
        }

        public void GiveGold()
        {
            var currency = inventory != null ? inventory.GetComponent<PlayerCurrency>() : null;
            if (currency == null) return;
            currency.AddGold(500);
            Debug.Log("[DevTools] +500 gold.");
        }

        public void KillAllEnemies()
        {
            var enemies = Object.FindObjectsByType<EnemyAI>();
            int count = 0;
            foreach (var enemy in enemies)
            {
                var hp = enemy.GetComponent<Health>();
                if (hp != null && !hp.IsDead)
                {
                    hp.TakeDamage(99999);
                    count++;
                }
            }
            Debug.Log($"[DevTools] Killed {count} enemies.");
        }

        public void ToggleGodMode()
        {
            godMode = !godMode;
            if (playerHealth != null)
            {
                var go = playerHealth.gameObject;
                var combat = go.GetComponent<PlayerCombat>();
                if (combat != null)
                    Debug.Log($"[DevTools] God mode: {(godMode ? "ON" : "OFF")}");
            }
            GodModeFlag.IsActive = godMode;
        }
    }

    public static class GodModeFlag
    {
        public static bool IsActive;
    }
}
