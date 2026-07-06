using UnityEngine;
using TMPro;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.Skilling;

namespace VoidBound.UI
{
    // Lists the player's owned consumables (cooked food + potions) with tap-to-
    // use, applying each item's effect via FoodConsumer. Self-builds on the
    // HUDCanvas from Panel5cFactory, like CraftingUI. Retires the temporary
    // "Eat Food" auto-button.
    public class ConsumablesPanel : MonoBehaviour
    {
        private RectTransform panel;
        private RectTransform list;
        private GameObject instigator;

        public void Open(GameObject player)
        {
            instigator = player;
            Panel5cFactory.CloseOtherHomesteadPanels(gameObject, this);
            EnsureBuilt();
            panel.gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.gameObject.SetActive(false);
        }

        public bool IsOpen => panel != null && panel.gameObject.activeSelf;

        private void EnsureBuilt()
        {
            if (panel != null) return;
            panel = Panel5cFactory.CreatePanel(transform, "ConsumablesPanel5c", "CONSUMABLES",
                460f, 380f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);
            list = Panel5cFactory.CreateScrollList(content, "ConsumablesList");
            Panel5cFactory.SetAnchor((RectTransform)list.parent, Vector2.zero, Vector2.one);
            panel.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            if (instigator == null) return;
            var matInv = instigator.GetComponent<MaterialInventory>();
            var fc = instigator.GetComponent<FoodConsumer>();
            if (matInv == null || fc == null) return;

            for (int i = list.childCount - 1; i >= 0; i--)
                Destroy(list.GetChild(i).gameObject);

            bool any = false;
            foreach (var kv in matInv.GetAllMaterials())
            {
                var def = matInv.GetMaterialDef(kv.Key);
                if (def == null || !def.isConsumable) continue;
                any = true;
                var captured = def;
                var row = Panel5cFactory.CreateListRow(list,
                    $"{def.displayName}  x{kv.Value}",
                    EffectSummary(def),
                    Panel5cFactory.TextPrimary, Panel5cFactory.TextMuted, interactable: true);
                row.onClick.AddListener(() => { fc.Use(captured); Refresh(); });
            }

            if (!any)
                Panel5cFactory.CreateListRow(list, "No consumables", "", Panel5cFactory.TextMuted,
                    Panel5cFactory.TextMuted, interactable: false);
        }

        private static string EffectSummary(MaterialItemSO m)
        {
            switch (m.effect)
            {
                case ConsumableEffect.Heal: return $"+{m.effectMagnitude} HP";
                case ConsumableEffect.BuffSTR: return $"+{m.effectMagnitude} STR {m.effectDuration:0}s";
                case ConsumableEffect.BuffDEX: return $"+{m.effectMagnitude} DEX {m.effectDuration:0}s";
                case ConsumableEffect.BuffVIG: return $"+{m.effectMagnitude} VIG {m.effectDuration:0}s";
                case ConsumableEffect.BuffINT: return $"+{m.effectMagnitude} INT {m.effectDuration:0}s";
                case ConsumableEffect.Swiftness: return "Swiftness";
                case ConsumableEffect.CurePoison: return "Cure Poison";
                case ConsumableEffect.Luck: return "Luck";
                default: return m.healOverTime > 0 ? $"+{m.healOverTime} HoT" : "";
            }
        }
    }
}
