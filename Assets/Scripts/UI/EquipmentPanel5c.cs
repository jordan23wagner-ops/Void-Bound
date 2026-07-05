using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.Skilling;

namespace VoidBound.UI
{
    // Runtime data binding for the Phase 5c Equipment panel built by Phase5cUIBuilder.
    // Resolves the builder-generated hierarchy by name and child order — the builder's
    // visuals are the approved mockup and are not modified here, only recolored/retexted
    // from live PlayerInventory / StatsComponent / PlayerSkills data.
    public class EquipmentPanel5c : MonoBehaviour
    {
        private static readonly Color32 EmptyBorder = new(0x3a, 0x3d, 0x3a, 255);
        private static readonly Color32 EmptyIcon   = new(0x5a, 0x60, 0x55, 255);
        private static readonly Color32 EmptyLabel  = new(0x3a, 0x3d, 0x3a, 255);

        // Child order of the columns/dock as created by Phase5cUIBuilder.
        private static readonly EquipmentSlot[] LeftSlots = {
            EquipmentSlot.Helm, EquipmentSlot.Body, EquipmentSlot.Legs,
            EquipmentSlot.Boots, EquipmentSlot.Gloves
        };
        private static readonly EquipmentSlot[] RightSlots = {
            EquipmentSlot.Amulet, EquipmentSlot.Ring, EquipmentSlot.Ammo, EquipmentSlot.Cape
        };
        private static readonly EquipmentSlot[] DockSlots = {
            EquipmentSlot.Weapon, EquipmentSlot.Shield
        };

        private class SlotWidgets
        {
            public Outline border;
            public TextMeshProUGUI icon;
            public TextMeshProUGUI label;
            public Image iconImg;
        }

        private PlayerInventory inventory;
        private StatsComponent stats;
        private PlayerSkills skills;

        private readonly Dictionary<EquipmentSlot, SlotWidgets> slots = new();
        private TextMeshProUGUI levelTMP;
        private readonly Dictionary<string, TextMeshProUGUI> statValues = new();
        private ItemDetailView5c detailView;
        private Phase5cUIRoot root;
        private bool initialized;

        private void OnEnable()
        {
            EnsureInit();
            if (inventory != null) inventory.OnInventoryChanged += Refresh;
            if (detailView != null) detailView.Hide();
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnInventoryChanged -= Refresh;
        }

        private void EnsureInit()
        {
            ResolvePlayer();
            if (initialized) return;

            root = GetComponentInParent<Phase5cUIRoot>(true);

            var body = transform.Find("Body");
            if (body == null)
            {
                Debug.LogError("[EquipmentPanel5c] Body not found — was the panel built by Phase5cUIBuilder?");
                return;
            }

            BindColumn(body.Find("LeftCol"), LeftSlots);
            BindColumn(body.Find("RightCol"), RightSlots);
            BindColumn(transform.Find("Dock"), DockSlots);

            var statCenter = body.Find("StatCenter");
            if (statCenter != null)
            {
                var level = statCenter.Find("Level");
                if (level != null) levelTMP = level.GetComponent<TextMeshProUGUI>();

                var card = statCenter.Find("StatCard");
                if (card != null)
                {
                    foreach (var key in new[] { "Damage", "Defense", "VIG", "STR", "DEX", "INT" })
                    {
                        var row = card.Find("Row_" + key);
                        var val = row != null ? row.Find("Val") : null;
                        if (val != null) statValues[key] = val.GetComponent<TextMeshProUGUI>();
                        else Debug.LogWarning($"[EquipmentPanel5c] Stat row '{key}' not found.");
                    }
                }
            }

            WireCloseButton();
            detailView = ItemDetailView5c.Create((RectTransform)transform);
            initialized = true;
        }

        private void ResolvePlayer()
        {
            if (inventory != null) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[EquipmentPanel5c] No Player found — panel will be empty.");
                return;
            }
            inventory = player.GetComponent<PlayerInventory>();
            stats = player.GetComponent<StatsComponent>();
            skills = player.GetComponent<PlayerSkills>();
        }

        private void BindColumn(Transform column, EquipmentSlot[] mapping)
        {
            if (column == null)
            {
                Debug.LogWarning("[EquipmentPanel5c] Slot column missing from built hierarchy.");
                return;
            }

            int slotIndex = 0;
            for (int i = 0; i < column.childCount && slotIndex < mapping.Length; i++)
            {
                var child = column.GetChild(i);
                if (!child.name.StartsWith("Slot_")) continue;

                var slot = mapping[slotIndex++];
                var widgets = new SlotWidgets
                {
                    border = child.GetComponent<Outline>(),
                    icon = child.Find("Icon")?.GetComponent<TextMeshProUGUI>(),
                    label = child.Find("Label")?.GetComponent<TextMeshProUGUI>()
                };

                // Swap the placeholder letter glyph for a real line-art icon.
                var sprite = SlotIconGenerator.SpriteFor(slot);
                if (sprite != null && widgets.icon != null)
                {
                    widgets.icon.text = "";
                    var imgGO = new GameObject("IconImg", typeof(RectTransform), typeof(Image));
                    var irt = (RectTransform)imgGO.transform;
                    irt.SetParent(widgets.icon.transform, false);
                    irt.anchorMin = irt.anchorMax = irt.pivot = new Vector2(0.5f, 0.5f);
                    irt.anchoredPosition = Vector2.zero;
                    irt.sizeDelta = new Vector2(28f, 32f);
                    widgets.iconImg = imgGO.GetComponent<Image>();
                    widgets.iconImg.sprite = sprite;
                    widgets.iconImg.preserveAspect = true;
                    widgets.iconImg.raycastTarget = false;
                }
                slots[slot] = widgets;

                var button = child.GetComponent<Button>();
                if (button == null) button = child.gameObject.AddComponent<Button>();
                var raycast = child.GetComponent<Image>();
                if (raycast != null) raycast.raycastTarget = true;
                var captured = slot;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ShowDetail(captured));
            }

            if (slotIndex < mapping.Length)
                Debug.LogWarning($"[EquipmentPanel5c] Only bound {slotIndex}/{mapping.Length} slots in {column.name}.");
        }

        private void WireCloseButton()
        {
            var closeBtn = transform.Find("Header/CloseBtn");
            if (closeBtn == null) return;
            var button = closeBtn.GetComponent<Button>();
            if (button == null) button = closeBtn.gameObject.AddComponent<Button>();
            var img = closeBtn.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (root != null) root.ClosePanel(gameObject);
                else gameObject.SetActive(false);
            });
        }

        public void Refresh()
        {
            if (!initialized) return;

            foreach (var pair in slots)
            {
                var item = inventory != null ? inventory.GetEquipped(pair.Key) : null;
                var w = pair.Value;

                if (item != null)
                {
                    Color rarityColor = RarityVisualEffects.GetRarityColor(item.rarity);
                    if (w.border != null) w.border.effectColor = rarityColor;
                    if (w.icon != null) w.icon.color = rarityColor;
                    if (w.iconImg != null) w.iconImg.color = rarityColor;
                    if (w.label != null) w.label.color = rarityColor;
                }
                else
                {
                    if (w.border != null) w.border.effectColor = EmptyBorder;
                    if (w.icon != null) w.icon.color = EmptyIcon;
                    if (w.iconImg != null) w.iconImg.color = EmptyIcon;
                    if (w.label != null) w.label.color = EmptyLabel;
                }

                // The weapon slot's icon reflects the equipped weapon's type
                // (bow/staff/wand/…); empty falls back to the generic sword.
                if (pair.Key == EquipmentSlot.Weapon && w.iconImg != null)
                    w.iconImg.sprite = SlotIconGenerator.SpriteFor(
                        EquipmentSlot.Weapon, item != null ? item.weaponType : WeaponType.None);
            }

            RefreshStatReadout();
        }

        private void RefreshStatReadout()
        {
            if (stats == null) return;
            var s = stats.EffectiveStats;

            if (levelTMP != null)
            {
                int combatLvl = skills != null ? CombatLevelCalculator.GetCombatLevel(skills) : 1;
                levelTMP.text = $"Level {combatLvl}";
            }

            var weapon = inventory != null ? inventory.GetEquipped(EquipmentSlot.Weapon) : null;
            int baseDmg = weapon != null ? weapon.baseDamage : 10;
            bool magic = weapon != null && WeaponStyleMap.GetStyle(weapon.weaponType) == WeaponStyle.Magic;
            float totalDmg = magic ? stats.MagicDamage(baseDmg) : stats.PhysicalDamage(baseDmg);
            int defense = (int)(100 - stats.DefenseMultiplier * 100);

            SetStat("Damage", $"{totalDmg:F0}");
            SetStat("Defense", defense.ToString());
            SetStat("VIG", s.vig.ToString());
            SetStat("STR", s.str.ToString());
            SetStat("DEX", s.dex.ToString());
            SetStat("INT", s.intel.ToString());
        }

        private void SetStat(string key, string value)
        {
            if (statValues.TryGetValue(key, out var tmp) && tmp != null)
                tmp.text = value;
        }

        private void ShowDetail(EquipmentSlot slot)
        {
            if (detailView == null) return;
            var item = inventory != null ? inventory.GetEquipped(slot) : null;
            if (item != null)
            {
                detailView.Show(item, "UNEQUIP", () => inventory.UnequipItem(slot));
            }
            else
            {
                detailView.ShowEmpty(slot.ToString());
            }
        }
    }
}
