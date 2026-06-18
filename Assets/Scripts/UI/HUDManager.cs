using UnityEngine;
using UnityEngine.UI;
using VoidBound.Combat;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private StatsComponent playerStats;
        [SerializeField] private Health playerHealth;

        [Header("Stats Panel")]
        [SerializeField] private Text levelText;
        [SerializeField] private Image xpFill;
        [SerializeField] private Image hpFill;
        [SerializeField] private Text hpText;
        [SerializeField] private Text statsText;

        [Header("Panels")]
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private GameObject backpackPanel;
        [SerializeField] private GameObject devToolsPanel;

        [Header("Buttons")]
        [SerializeField] private Button equipButton;
        [SerializeField] private Button backpackButton;
        [SerializeField] private Button devToolsButton;

        private EquipmentPanelUI equipmentUI;
        private BackpackPanelUI backpackUI;

        private void Start()
        {
            if (equipmentPanel != null)
            {
                equipmentPanel.SetActive(false);
                equipmentUI = equipmentPanel.GetComponent<EquipmentPanelUI>();
            }
            if (backpackPanel != null)
            {
                backpackPanel.SetActive(false);
                backpackUI = backpackPanel.GetComponent<BackpackPanelUI>();
            }
            if (devToolsPanel != null)
                devToolsPanel.SetActive(false);

            if (equipButton != null) equipButton.onClick.AddListener(ToggleEquipment);
            if (backpackButton != null) backpackButton.onClick.AddListener(ToggleBackpack);
            if (devToolsButton != null) devToolsButton.onClick.AddListener(ToggleDevTools);

            if (playerHealth != null)
                playerHealth.OnHealthChanged += OnHealthChanged;

            if (inventory != null)
                inventory.OnInventoryChanged += RefreshStats;

            RefreshStats();
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
                    ToggleEquipment();
                if (UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame)
                    ToggleBackpack();
            }
        }

        public void ToggleEquipment()
        {
            bool opening = equipmentPanel != null && !equipmentPanel.activeSelf;
            CloseAllPanels();
            if (opening && equipmentPanel != null)
            {
                equipmentPanel.SetActive(true);
                equipmentUI?.Refresh();
            }
        }

        public void ToggleBackpack()
        {
            bool opening = backpackPanel != null && !backpackPanel.activeSelf;
            CloseAllPanels();
            if (opening && backpackPanel != null)
            {
                backpackPanel.SetActive(true);
                backpackUI?.Refresh();
            }
        }

        public void ToggleDevTools()
        {
            if (devToolsPanel != null)
            {
                CloseAllPanels();
                devToolsPanel.SetActive(!devToolsPanel.activeSelf);
            }
        }

        private void CloseAllPanels()
        {
            if (equipmentPanel != null) equipmentPanel.SetActive(false);
            if (backpackPanel != null) backpackPanel.SetActive(false);
            if (devToolsPanel != null) devToolsPanel.SetActive(false);
        }

        private void OnHealthChanged(int current, int max)
        {
            if (hpFill != null && max > 0)
                hpFill.fillAmount = (float)current / max;
            if (hpText != null)
                hpText.text = $"{current}/{max}";
        }

        private void RefreshStats()
        {
            if (playerStats == null) return;

            var s = playerStats.BaseStats;
            if (statsText != null)
                statsText.text = $"STR {s.str}  DEX {s.dex}\nVIG {s.vig}  INT {s.intel}";

            if (levelText != null)
                levelText.text = "Lv 1";

            if (xpFill != null)
                xpFill.fillAmount = 0f;

            if (playerHealth != null && hpFill != null)
                hpFill.fillAmount = playerHealth.MaxHP > 0 ? (float)playerHealth.CurrentHP / playerHealth.MaxHP : 1f;
            if (playerHealth != null && hpText != null)
                hpText.text = $"{playerHealth.CurrentHP}/{playerHealth.MaxHP}";
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.OnHealthChanged -= OnHealthChanged;
            if (inventory != null)
                inventory.OnInventoryChanged -= RefreshStats;
        }
    }
}
