using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        [SerializeField] private TMP_Text tmpStatReadout;
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text detailName;
        [SerializeField] private Text detailRarity;
        [SerializeField] private Text detailSlot;
        [SerializeField] private Text detailStats;
        [SerializeField] private Text detailSet;
        [SerializeField] private Button unequipButton;
        [SerializeField] private Button closeButton;

        private static readonly Color EmptyIconColor = new Color32(0x6a, 0x6d, 0x64, 0xFF);
        private static readonly Color EmptyBorderColor = new Color32(0x5a, 0x5d, 0x54, 0xFF);
        private static readonly Color VigColor = new Color32(0xE2, 0x4B, 0x4A, 0xFF);
        private static readonly Color StrColor = new Color32(0xFA, 0xC7, 0x75, 0xFF);
        private static readonly Color DexColor = new Color32(0x97, 0xC4, 0x59, 0xFF);
        private static readonly Color IntColor = new Color32(0x37, 0x8A, 0xDD, 0xFF);

        private static readonly EquipmentSlot[] LeftSlots = {
            EquipmentSlot.Helm, EquipmentSlot.Body, EquipmentSlot.Legs,
            EquipmentSlot.Boots, EquipmentSlot.Gloves
        };
        private static readonly EquipmentSlot[] RightSlots = {
            EquipmentSlot.Amulet, EquipmentSlot.Ring, EquipmentSlot.Ammo, EquipmentSlot.Cape
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
                CreateSlotIcon(leftColumn, slot, GetSlotLabel(slot));
            foreach (var slot in RightSlots)
                CreateSlotIcon(rightColumn, slot, GetSlotLabel(slot));

            CreateSlotIcon(weaponDock, EquipmentSlot.Weapon, "⚔");
            CreateSlotIcon(weaponDock, EquipmentSlot.Shield, "⛨");

            RefreshStatReadout();
        }

        private void RefreshStatReadout()
        {
            if (inventory == null) return;
            if (tmpStatReadout == null)
                tmpStatReadout = GetComponentInChildren<TMP_Text>();
            if (tmpStatReadout == null) return;

            var player = inventory.gameObject;
            var stats = player.GetComponent<StatsComponent>();
            var skills = player.GetComponent<PlayerSkills>();
            var health = player.GetComponent<Health>();

            if (stats == null)
            {
                if (tmpStatReadout != null) tmpStatReadout.text = "NO STATS";
                Debug.LogWarning("[EquipmentPanelUI] StatsComponent missing on player.");
                return;
            }

            var s = stats.EffectiveStats;
            int combatLvl = skills != null ? CombatLevelCalculator.GetCombatLevel(skills) : 1;

            var weapon = inventory.GetEquipped(EquipmentSlot.Weapon);
            int dmg = weapon != null ? weapon.baseDamage : 10;
            float totalDmg = stats.PhysicalDamage(dmg);
            int defense = (int)(100 - stats.DefenseMultiplier * 100);

            tmpStatReadout.text =
                $"<color=#d4d8d0>Level {combatLvl}</color>\n\n" +
                $"<color=#888d84>Damage</color>  <color=#d4d8d0>{totalDmg:F0}</color>\n" +
                $"<color=#888d84>Defense</color>  <color=#d4d8d0>{defense}</color>\n\n" +
                $"<color=#E24B4A>VIG  {s.vig}</color>\n" +
                $"<color=#FAC775>STR  {s.str}</color>\n" +
                $"<color=#97C459>DEX  {s.dex}</color>\n" +
                $"<color=#378ADD>INT  {s.intel}</color>";
        }

        private string GetSlotLabel(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Helm => "⛑",
                EquipmentSlot.Body => "⛉",
                EquipmentSlot.Legs => "⫍",
                EquipmentSlot.Boots => "❧",
                EquipmentSlot.Gloves => "✋",
                EquipmentSlot.Amulet => "❀",
                EquipmentSlot.Ring => "◎",
                EquipmentSlot.Ammo => "➶",
                EquipmentSlot.Cape => "⁂",
                _ => "■"
            };
        }

        private void CreateSlotIcon(Transform parent, EquipmentSlot slot, string icon)
        {
            if (parent == null) return;
            var item = inventory?.GetEquipped(slot);
            var captured = slot;

            Color slotColor = item != null
                ? RarityVisualEffects.GetRarityColor(item.rarity)
                : EmptyBorderColor;
            Color iconColor = item != null
                ? RarityVisualEffects.GetRarityColor(item.rarity)
                : EmptyIconColor;

            var obj = new GameObject(slot.ToString());
            obj.transform.SetParent(parent, false);
            var le = obj.AddComponent<LayoutElement>();
            le.preferredHeight = 52f;
            le.preferredWidth = 52f;

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);

            var border = new GameObject("Border");
            border.transform.SetParent(obj.transform, false);
            var bRect = border.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;
            var bImg = border.AddComponent<Image>();
            bImg.color = Color.clear;
            var outline = border.AddComponent<Outline>();
            outline.effectColor = slotColor;
            outline.effectDistance = new Vector2(2f, 2f);

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(obj.transform, false);
            var iRect = iconObj.AddComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0.1f, 0.2f);
            iRect.anchorMax = new Vector2(0.9f, 0.85f);
            iRect.offsetMin = Vector2.zero;
            iRect.offsetMax = Vector2.zero;
            var iconText = iconObj.AddComponent<Text>();
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 18;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color = iconColor;
            iconText.text = icon;

            var label = new GameObject("Label");
            label.transform.SetParent(obj.transform, false);
            var lRect = label.AddComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = new Vector2(1f, 0.25f);
            lRect.offsetMin = new Vector2(2f, 0f);
            lRect.offsetMax = new Vector2(-2f, 0f);
            var t = label.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 8;
            t.alignment = TextAnchor.LowerCenter;
            t.color = item != null ? Color.white : new Color(0.45f, 0.45f, 0.45f);
            string slotName = slot.ToString();
            t.text = item != null ? item.displayName : slotName;

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
                string slotName = slot.ToString();
                if (detailName != null) detailName.text = slotName;
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
