using System.Collections.Generic;
using UnityEngine;
using TMPro;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    // "YOU DIED" overlay shown during the respawn delay (§4A). Reinforces the
    // stakes by listing exactly what you kept — your most valuable items — while
    // everything else drops to your grave. Auto-hidden when you respawn. Lives on
    // the persisted HUDCanvas; PlayerDeath drives Show/Hide.
    public class DeathScreenUI : MonoBehaviour
    {
        private RectTransform root;
        private RectTransform keptList;
        private TextMeshProUGUI subtitle;

        public void Show(IReadOnlyList<GearItemSO> kept)
        {
            EnsureBuilt();
            Populate(kept);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling(); // draw above the rest of the HUD
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
        }

        private void EnsureBuilt()
        {
            if (root != null) return;

            root = Panel5cFactory.MakeRect("DeathScreen", transform);
            Panel5cFactory.SetAnchor(root, Vector2.zero, Vector2.one);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            Panel5cFactory.AddPanelBg(root.gameObject, new Color32(6, 7, 9, 214), raycast: true);

            var col = Panel5cFactory.MakeRect("Col", root);
            Panel5cFactory.SetAnchor(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            col.pivot = new Vector2(0.5f, 0.5f);
            col.sizeDelta = new Vector2(380, 320);

            var title = Panel5cFactory.CreateLabel(col, "Title", "YOU DIED",
                34f, new Color(0.86f, 0.24f, 0.22f));
            Panel5cFactory.SetAnchor(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1));
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(0, 50);
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.characterSpacing = 6f;

            subtitle = Panel5cFactory.CreateLabel(col, "Subtitle", "",
                12f, Panel5cFactory.TextMuted);
            Panel5cFactory.SetAnchor(subtitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1));
            subtitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0, -54);
            subtitle.rectTransform.sizeDelta = new Vector2(-20, 34);
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.textWrappingMode = TextWrappingModes.Normal;

            var listArea = Panel5cFactory.MakeRect("KeptArea", col);
            Panel5cFactory.SetAnchor(listArea, new Vector2(0, 0), new Vector2(1, 1));
            listArea.offsetMin = new Vector2(40, 34);
            listArea.offsetMax = new Vector2(-40, -96);
            keptList = Panel5cFactory.CreateScrollList(listArea, "DeathKeptList");
            Panel5cFactory.SetAnchor((RectTransform)keptList.parent, Vector2.zero, Vector2.one);

            var respawn = Panel5cFactory.CreateLabel(col, "Respawn",
                "Respawning at the Homestead…", 11f, Panel5cFactory.TextMuted);
            Panel5cFactory.SetAnchor(respawn.rectTransform, new Vector2(0, 0), new Vector2(1, 0));
            respawn.rectTransform.pivot = new Vector2(0.5f, 0f);
            respawn.rectTransform.sizeDelta = new Vector2(0, 20);
            respawn.alignment = TextAlignmentOptions.Center;

            root.gameObject.SetActive(false);
        }

        private void Populate(IReadOnlyList<GearItemSO> kept)
        {
            for (int i = keptList.childCount - 1; i >= 0; i--)
                Destroy(keptList.GetChild(i).gameObject);

            int n = kept != null ? kept.Count : 0;
            subtitle.text = n > 0
                ? $"Kept your {n} most valuable — everything else dropped to your grave:"
                : "Everything you carried dropped to your grave.";

            if (n == 0)
            {
                Panel5cFactory.CreateListRow(keptList, "Nothing kept", "",
                    Panel5cFactory.TextMuted, Panel5cFactory.TextMuted, interactable: false);
                return;
            }

            foreach (var item in kept)
            {
                if (item == null) continue;
                Panel5cFactory.CreateListRow(keptList, item.displayName, $"{item.goldValue}g",
                    RarityVisualEffects.GetRarityColor(item.rarity),
                    Panel5cFactory.Gold, interactable: false);
            }
        }
    }
}
