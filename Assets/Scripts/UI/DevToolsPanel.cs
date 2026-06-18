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

        private bool godMode;

        private void Start()
        {
            if (giveGearButton != null) giveGearButton.onClick.AddListener(GiveTestGear);
            if (killAllButton != null) killAllButton.onClick.AddListener(KillAllEnemies);
            if (godModeButton != null) godModeButton.onClick.AddListener(ToggleGodMode);
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
