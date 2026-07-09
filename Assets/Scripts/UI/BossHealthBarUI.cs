using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Combat;

namespace VoidBound.UI
{
    // Prominent boss health bar across the top of the screen, distinct from the
    // small floating enemy bars. A boss binds to it on aggro (BossHealthBarUI.Bind)
    // and unbinds on death/leash. Lives on HUDCanvas; self-builds on first Bind.
    public class BossHealthBarUI : MonoBehaviour
    {
        private static BossHealthBarUI instance;

        private RectTransform root;
        private RectTransform fill;
        private TextMeshProUGUI nameLabel;
        private TextMeshProUGUI valueLabel;
        private Health bound;

        private static BossHealthBarUI Instance =>
            instance != null ? instance : (instance = Object.FindAnyObjectByType<BossHealthBarUI>());

        public static void Bind(Health health, string bossName)
        {
            if (Instance == null || health == null) return;
            Instance.BindInternal(health, bossName);
        }

        public static void Unbind(Health health)
        {
            if (Instance == null) return;
            if (Instance.bound == health) Instance.HideInternal();
        }

        private void BindInternal(Health health, string bossName)
        {
            EnsureBuilt();
            if (bound != null) bound.OnHealthChanged -= OnHealthChanged;
            bound = health;
            bound.OnHealthChanged += OnHealthChanged;
            nameLabel.text = bossName != null ? bossName.ToUpper() : "BOSS";
            root.gameObject.SetActive(true);
            OnHealthChanged(bound.CurrentHP, bound.MaxHP);
        }

        private void HideInternal()
        {
            if (bound != null) bound.OnHealthChanged -= OnHealthChanged;
            bound = null;
            if (root != null) root.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (bound != null) bound.OnHealthChanged -= OnHealthChanged;
        }

        // Safety net: if the bound boss was destroyed (zone unload) without a clean
        // unbind, the reference goes (fake-)null — hide so the bar can't linger on
        // the persistent HUD into the next scene.
        private void LateUpdate()
        {
            if (root != null && root.gameObject.activeSelf && bound == null)
                HideInternal();
        }

        private void OnHealthChanged(int cur, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
            if (fill != null) fill.anchorMax = new Vector2(ratio, 1f);
            if (valueLabel != null) valueLabel.text = $"{Mathf.Max(0, cur)} / {max}";
            if (cur <= 0) HideInternal();
        }

        private void EnsureBuilt()
        {
            if (root != null) return;

            root = Panel5cFactory.MakeRect("BossHealthBar", transform);
            root.anchorMin = new Vector2(0.5f, 1f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = new Vector2(0f, -18f);
            root.sizeDelta = new Vector2(560f, 46f);

            nameLabel = Panel5cFactory.CreateLabel(root, "BossName", "BOSS", 14f, Panel5cFactory.Gold);
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.characterSpacing = 4f;
            nameLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            nameLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            nameLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            nameLabel.rectTransform.offsetMin = new Vector2(0f, -18f);
            nameLabel.rectTransform.offsetMax = new Vector2(0f, 0f);

            // Bar track (dark) + red fill anchored to grow/shrink from the left.
            var track = Panel5cFactory.MakeRect("Track", root);
            track.anchorMin = new Vector2(0f, 0f);
            track.anchorMax = new Vector2(1f, 0f);
            track.pivot = new Vector2(0.5f, 0f);
            track.offsetMin = new Vector2(0f, 0f);
            track.offsetMax = new Vector2(0f, 0f);
            track.sizeDelta = new Vector2(0f, 22f);
            Panel5cFactory.AddPanelBg(track.gameObject, new Color(0.05f, 0.05f, 0.06f, 0.92f), false);
            Panel5cFactory.AddOutline(track.gameObject, new Color32(0x2a, 0x2f, 0x2a, 255));

            fill = Panel5cFactory.MakeRect("Fill", track);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(1f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = new Vector2(2f, 2f);
            fill.offsetMax = new Vector2(-2f, -2f);
            Panel5cFactory.AddPanelBg(fill.gameObject, new Color(0.72f, 0.13f, 0.14f, 1f), false);

            valueLabel = Panel5cFactory.CreateLabel(track, "BossHP", "", 11f, Panel5cFactory.TextPrimary);
            valueLabel.alignment = TextAlignmentOptions.Center;
            valueLabel.fontStyle = FontStyles.Bold;
            Panel5cFactory.SetAnchor(valueLabel.rectTransform, Vector2.zero, Vector2.one);

            root.gameObject.SetActive(false);
        }
    }
}
