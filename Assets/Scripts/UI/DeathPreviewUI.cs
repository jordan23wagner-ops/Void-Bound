using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    // "Kept on death" preview (GDD §4A): toggle with K to see exactly which items
    // you'd keep if you died right now — your KeepCount most valuable across
    // equipped + backpack — so you can weigh a risky push before committing.
    // Lives on the persisted HUDCanvas; self-builds from Panel5cFactory and
    // refreshes live off inventory changes while open.
    public class DeathPreviewUI : MonoBehaviour
    {
        private RectTransform panel;
        private RectTransform list;

        private PlayerInventory inventory;
        private bool open;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.kKey.wasPressedThisFrame)
                Toggle();
        }

        public void Toggle()
        {
            if (open) { Close(); return; }
            EnsureBuilt();
            ResolveInventory();
            Panel5cFactory.CloseOtherHomesteadPanels(gameObject, null);
            open = true;
            panel.gameObject.SetActive(true);
            if (inventory != null) inventory.OnInventoryChanged += Refresh;
            Refresh();
        }

        public void Close()
        {
            open = false;
            if (panel != null) panel.gameObject.SetActive(false);
            if (inventory != null) inventory.OnInventoryChanged -= Refresh;
        }

        private void ResolveInventory()
        {
            if (inventory != null) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) inventory = player.GetComponent<PlayerInventory>();
        }

        private void EnsureBuilt()
        {
            if (panel != null) return;

            panel = Panel5cFactory.CreatePanel(transform, "DeathPreviewPanel5c", "KEPT ON DEATH",
                340f, 250f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);

            var subtitle = Panel5cFactory.CreateLabel(content, "Subtitle",
                $"Your {PlayerDeath.KeepCount} most valuable — everything else drops.",
                10f, Panel5cFactory.TextMuted);
            Panel5cFactory.SetAnchor(subtitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1));
            subtitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            subtitle.rectTransform.sizeDelta = new Vector2(-4, 26);
            subtitle.textWrappingMode = TextWrappingModes.Normal;

            var listArea = Panel5cFactory.MakeRect("ListArea", content);
            Panel5cFactory.SetAnchor(listArea, new Vector2(0, 0), new Vector2(1, 1));
            listArea.offsetMin = Vector2.zero;
            listArea.offsetMax = new Vector2(0, -28);
            list = Panel5cFactory.CreateScrollList(listArea, "KeptList");
            Panel5cFactory.SetAnchor((RectTransform)list.parent, Vector2.zero, Vector2.one);

            panel.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            if (list == null) return;
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);

            var kept = PlayerDeath.PreviewKept(inventory);
            if (kept.Count == 0)
            {
                Panel5cFactory.CreateListRow(list, "Nothing to keep", "",
                    Panel5cFactory.TextMuted, Panel5cFactory.TextMuted, interactable: false);
                return;
            }

            foreach (var item in kept)
            {
                if (item == null) continue;
                Panel5cFactory.CreateListRow(list, item.displayName, $"{item.goldValue}g",
                    RarityVisualEffects.GetRarityColor(item.rarity),
                    Panel5cFactory.Gold, interactable: false);
            }
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnInventoryChanged -= Refresh;
        }
    }
}
