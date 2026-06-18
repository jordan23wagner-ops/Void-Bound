using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.Skilling;

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
        [SerializeField] private Text currencyText;
        [SerializeField] private PlayerCurrency playerCurrency;

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
            ResolvePlayerReferences();

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

            if (playerCurrency != null)
                playerCurrency.OnCurrencyChanged += RefreshCurrency;

            RefreshStats();
            RefreshCurrency();
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
            ToggleInventoryGroup();
        }

        public void ToggleBackpack()
        {
            ToggleInventoryGroup();
        }

        private void ToggleInventoryGroup()
        {
            var panel = equipmentPanel ?? backpackPanel;
            if (panel == null) return;

            bool opening = !panel.activeSelf;
            CloseAllPanels();
            if (opening)
            {
                panel.SetActive(true);
                if (equipmentUI == null)
                    equipmentUI = panel.GetComponentInChildren<EquipmentPanelUI>(true);
                if (backpackUI == null)
                    backpackUI = panel.GetComponentInChildren<BackpackPanelUI>(true);
                equipmentUI?.Refresh();
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

        private void ResolvePlayerReferences()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[HUDManager] No Player found — stats panel will be blank.");
                return;
            }

            if (playerStats == null) playerStats = player.GetComponent<StatsComponent>();
            if (playerHealth == null) playerHealth = player.GetComponent<Health>();
            if (inventory == null) inventory = player.GetComponent<PlayerInventory>();
            if (playerCurrency == null) playerCurrency = player.GetComponent<PlayerCurrency>();

            if (playerStats == null) Debug.LogError("[HUDManager] Player missing StatsComponent.");
            if (playerHealth == null) Debug.LogError("[HUDManager] Player missing Health.");

            ResolveUIReferences();
        }

        private void ResolveUIReferences()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) return;

            if (statsText == null) statsText = FindTextInChildren(transform, "StatsText");
            if (levelText == null) levelText = FindTextInChildren(transform, "LevelText");
            if (hpText == null) hpText = FindTextInChildren(transform, "HPText");
            if (currencyText == null) currencyText = FindTextInChildren(transform, "CurrencyText");

            if (hpFill == null)
            {
                var hpBar = FindInChildren(transform, "HPBar") ?? FindInChildren(transform, "HPBarBG");
                if (hpBar != null)
                {
                    var fill = hpBar.Find("Fill") ?? hpBar.Find("HPFill");
                    if (fill != null) hpFill = fill.GetComponent<Image>();
                }
            }
            if (xpFill == null)
            {
                var xpBar = FindInChildren(transform, "XPBar");
                if (xpBar != null)
                {
                    var fill = xpBar.Find("Fill");
                    if (fill != null) xpFill = fill.GetComponent<Image>();
                }
            }

            if (statsText == null) Debug.LogError("[HUDManager] Could not find StatsText in HUD hierarchy.");
            if (levelText == null) Debug.LogError("[HUDManager] Could not find LevelText in HUD hierarchy.");
        }

        private static Text FindTextInChildren(Transform root, string name)
        {
            var t = FindInChildren(root, name);
            if (t == null) return null;
            var legacy = t.GetComponent<Text>();
            if (legacy != null) return legacy;
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                Debug.Log($"[HUDManager] {name} is TMP, not legacy Text — adding legacy Text wrapper.");
            }
            return legacy;
        }

        private static Transform FindInChildren(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                var found = FindInChildren(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void RefreshStats()
        {
            if (playerStats == null)
            {
                ResolvePlayerReferences();
                if (playerStats == null)
                {
                    SetText(statsText, "NO PLAYER");
                    Debug.LogWarning("[HUDManager] RefreshStats — playerStats null after resolve.");
                    return;
                }
            }

            var s = playerStats.EffectiveStats;

            if (statsText != null)
            {
                statsText.supportRichText = true;
                statsText.text =
                    $"<color=#E24B4A>VIG {s.vig}</color>  <color=#FAC775>STR {s.str}</color>\n" +
                    $"<color=#97C459>DEX {s.dex}</color>  <color=#378ADD>INT {s.intel}</color>";
            }
            else
            {
                Debug.LogWarning("[HUDManager] statsText is null — cannot display stats.");
            }

            var skills = playerStats.GetComponent<PlayerSkills>();
            if (levelText != null)
            {
                int combatLvl = skills != null ? CombatLevelCalculator.GetCombatLevel(skills) : 1;
                levelText.text = $"Lv {combatLvl}";
            }

            if (xpFill != null)
                xpFill.fillAmount = 0f;

            if (playerHealth != null)
            {
                if (hpFill != null)
                    hpFill.fillAmount = playerHealth.MaxHP > 0 ? (float)playerHealth.CurrentHP / playerHealth.MaxHP : 1f;
                if (hpText != null)
                    hpText.text = $"{playerHealth.CurrentHP}/{playerHealth.MaxHP}";
            }
            else if (hpText != null)
            {
                hpText.text = "NO HP REF";
            }
        }

        private void RefreshCurrency()
        {
            if (currencyText == null || playerCurrency == null) return;
            currencyText.text = $"Gold: {playerCurrency.Gold}  Shards: {playerCurrency.VoidShards}";
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.OnHealthChanged -= OnHealthChanged;
            if (inventory != null)
                inventory.OnInventoryChanged -= RefreshStats;
            if (playerCurrency != null)
                playerCurrency.OnCurrencyChanged -= RefreshCurrency;
        }

        private static void SetText(Text t, string val)
        {
            if (t != null) t.text = val;
        }
    }
}
