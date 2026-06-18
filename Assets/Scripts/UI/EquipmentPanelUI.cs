using UnityEngine;
using UnityEngine.UI;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.Skilling;

namespace VoidBound.UI
{
    public class EquipmentPanelUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Transform leftColumn;
        [SerializeField] private Transform rightColumn;
        [SerializeField] private Transform weaponDock;
        [SerializeField] private Text statReadout;
        [SerializeField] private Text charLevelText;
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text detailName;
        [SerializeField] private Text detailRarity;
        [SerializeField] private Text detailSlot;
        [SerializeField] private Text detailStats;
        [SerializeField] private Text detailSet;
        [SerializeField] private Button unequipButton;
        [SerializeField] private Button closeButton;

        private static readonly EquipmentSlot[] LeftSlots = {
            EquipmentSlot.Head, EquipmentSlot.Body, EquipmentSlot.Legs,
            EquipmentSlot.Hands, EquipmentSlot.Feet
        };
        private static readonly EquipmentSlot[] RightSlots = {
            EquipmentSlot.Cape, EquipmentSlot.Neck, EquipmentSlot.Ring, EquipmentSlot.Ammo
        };

        private EquipmentSlot selectedSlot;

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        public void Refresh()
        {
            ClearChildren(leftColumn);
            ClearChildren(rightColumn);
            ClearChildren(weaponDock);
            if (detailPanel != null) detailPanel.SetActive(false);

            foreach (var slot in LeftSlots)
                CreateSlotIcon(leftColumn, slot);
            foreach (var slot in RightSlots)
                CreateSlotIcon(rightColumn, slot);

            CreateSlotIcon(weaponDock, EquipmentSlot.Weapon);
            CreateSlotIcon(weaponDock, EquipmentSlot.Shield);

            RefreshStatReadout();
        }

        private void RefreshStatReadout()
        {
            if (statReadout == null || inventory == null) return;

            var player = inventory.gameObject;
            var stats = player.GetComponent<StatsComponent>();
            var skills = player.GetComponent<PlayerSkills>();

            if (stats == null) return;
            var s = stats.EffectiveStats;

            string text = "";
            if (skills != null)
            {
                int combatLvl = CombatLevelCalculator.GetCombatLevel(skills);
                text += $"Level {combatLvl}\n\n";
            }

            var weapon = inventory.GetEquipped(EquipmentSlot.Weapon);
            int dmg = weapon != null ? weapon.baseDamage : 10;
            float totalDmg = stats.PhysicalDamage(dmg);

            text += $"Damage  {totalDmg:F0}\n";
            text += $"Defense  {(int)(100 - stats.DefenseMultiplier * 100)}\n\n";
            text += $"VIG  {s.vig}\n";
            text += $"STR  {s.str}\n";
            text += $"DEX  {s.dex}\n";
            text += $"INT  {s.intel}";
            statReadout.text = text;
        }

        private void CreateSlotIcon(Transform parent, EquipmentSlot slot)
        {
            if (parent == null) return;
            var item = inventory?.GetEquipped(slot);
            var captured = slot;

            var obj = new GameObject(slot.ToString());
            obj.transform.SetParent(parent, false);
            var le = obj.AddComponent<LayoutElement>();
            le.preferredHeight = 52f;
            le.preferredWidth = 52f;

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            if (item != null)
            {
                var border = new GameObject("Border");
                border.transform.SetParent(obj.transform, false);
                var bRect = border.AddComponent<RectTransform>();
                bRect.anchorMin = Vector2.zero;
                bRect.anchorMax = Vector2.one;
                bRect.offsetMin = Vector2.zero;
                bRect.offsetMax = Vector2.zero;
                var bImg = border.AddComponent<Image>();
                bImg.color = RarityVisualEffects.GetRarityColor(item.rarity);
                var outline = border.AddComponent<Outline>();
                outline.effectColor = RarityVisualEffects.GetRarityColor(item.rarity);
                outline.effectDistance = new Vector2(2f, 2f);

                var inner = new GameObject("Inner");
                inner.transform.SetParent(border.transform, false);
                var iRect = inner.AddComponent<RectTransform>();
                iRect.anchorMin = Vector2.zero;
                iRect.anchorMax = Vector2.one;
                iRect.offsetMin = new Vector2(3f, 3f);
                iRect.offsetMax = new Vector2(-3f, -3f);
                inner.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 1f);
            }

            var label = new GameObject("Label");
            label.transform.SetParent(obj.transform, false);
            var lRect = label.AddComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.offsetMin = new Vector2(2f, 2f);
            lRect.offsetMax = new Vector2(-2f, -2f);
            var t = label.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 9;
            t.alignment = TextAnchor.LowerCenter;
            t.color = item != null ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            t.text = item != null ? item.displayName : slot.ToString();

            var btn = obj.AddComponent<Button>();
            btn.onClick.AddListener(() => ShowDetail(captured));
        }

        private void ShowDetail(EquipmentSlot slot)
        {
            selectedSlot = slot;
            var item = inventory?.GetEquipped(slot);
            if (detailPanel != null) detailPanel.SetActive(true);

            if (item != null)
            {
                if (detailName != null) detailName.text = item.displayName;
                if (detailRarity != null)
                {
                    detailRarity.text = item.rarity.ToString();
                    detailRarity.color = RarityVisualEffects.GetRarityColor(item.rarity);
                }
                if (detailSlot != null)
                    detailSlot.text = item.slot == EquipmentSlot.Weapon
                        ? $"{item.slot} ({item.weaponType})" : item.slot.ToString();
                if (detailStats != null)
                {
                    var m = item.statModifiers;
                    detailStats.text = $"STR +{m.str}  DEX +{m.dex}\nVIG +{m.vig}  INT +{m.intel}";
                    if (item.baseDamage > 0) detailStats.text += $"\nDamage: {item.baseDamage}";
                }
                if (detailSet != null)
                    detailSet.text = string.IsNullOrEmpty(item.setId) ? "" : $"Set: {item.setId}";
                if (unequipButton != null)
                {
                    unequipButton.gameObject.SetActive(true);
                    unequipButton.onClick.RemoveAllListeners();
                    unequipButton.onClick.AddListener(() => { inventory?.UnequipItem(selectedSlot); Refresh(); });
                }
            }
            else
            {
                if (detailName != null) detailName.text = slot.ToString();
                if (detailRarity != null) { detailRarity.text = "Empty"; detailRarity.color = Color.gray; }
                if (detailSlot != null) detailSlot.text = "";
                if (detailStats != null) detailStats.text = "---";
                if (detailSet != null) detailSet.text = "";
                if (unequipButton != null) unequipButton.gameObject.SetActive(false);
            }
        }

        private void ClearChildren(Transform p)
        {
            if (p == null) return;
            for (int i = p.childCount - 1; i >= 0; i--) Destroy(p.GetChild(i).gameObject);
        }
    }
}
