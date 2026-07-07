using UnityEngine;
using UnityEngine.SceneManagement;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    // Fast Travel Portal destination list, data-driven from ZoneDefinitionSO
    // (Phase 7). Unlocked, non-current zones actually load via SceneManager;
    // locked zones stay "Coming soon" (visual only, per Phase 6 spec).
    //
    // Because dying in the field drops all but your KeepCount most valuable items
    // (§4A), the panel also previews exactly what you'd keep — right where you
    // commit to a push — so you can weigh the risk before travelling.
    public class PortalUI : MonoBehaviour
    {
        [SerializeField] private ZoneDefinitionSO[] destinations;

        private RectTransform panel;
        private RectTransform list;
        private RectTransform keptList;
        private PlayerInventory inventory;
        private bool subscribed;

        public void Open(VoidBound.Core.Interactable station)
        {
            ResolveInventory();
            Panel5cFactory.CloseOtherHomesteadPanels(gameObject, this);
            EnsureBuilt();
            panel.gameObject.SetActive(true);
            if (station != null)
                StationProximityCloser.Track(gameObject, this, station, Close);
            Subscribe(); // keep the preview live while the panel is open
            Refresh();
        }

        public void Close()
        {
            Unsubscribe();
            if (panel != null) panel.gameObject.SetActive(false);
            StationProximityCloser.Untrack(gameObject, this);
        }

        // The "kept on death" column reflects the player's current gear, so keep
        // it in sync while the panel is open (e.g. equipping/dropping at a nearby
        // station). Only the kept column depends on inventory; destinations don't.
        private void Subscribe()
        {
            if (subscribed || inventory == null) return;
            inventory.OnInventoryChanged += RefreshKept;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || inventory == null) return;
            inventory.OnInventoryChanged -= RefreshKept;
            subscribed = false;
        }

        private void OnDisable() => Unsubscribe();

        private void ResolveInventory()
        {
            if (inventory != null) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) inventory = player.GetComponent<PlayerInventory>();
        }

        private void EnsureBuilt()
        {
            if (panel != null) return;

            panel = Panel5cFactory.CreatePanel(transform, "PortalPanel", "FAST TRAVEL",
                520f, 300f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);

            // Left column — destinations
            var destHeader = Panel5cFactory.CreateLabel(content, "DestHeader", "DESTINATION",
                11f, Panel5cFactory.TextMuted);
            Panel5cFactory.SetAnchor(destHeader.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1));
            destHeader.rectTransform.pivot = new Vector2(0.5f, 1f);
            destHeader.rectTransform.sizeDelta = new Vector2(-8, 18);
            destHeader.characterSpacing = 3f;

            var destViewport = Panel5cFactory.MakeRect("DestArea", content);
            Panel5cFactory.SetAnchor(destViewport, new Vector2(0, 0), new Vector2(0.5f, 1));
            destViewport.offsetMin = Vector2.zero;
            destViewport.offsetMax = new Vector2(-4, -22);
            list = Panel5cFactory.CreateScrollList(destViewport, "DestinationList");
            Panel5cFactory.SetAnchor((RectTransform)list.parent, Vector2.zero, Vector2.one);

            // Right column — "kept on death" preview (§4A)
            var keptHeader = Panel5cFactory.CreateLabel(content, "KeptHeader", "KEPT ON DEATH",
                11f, Panel5cFactory.TextMuted);
            Panel5cFactory.SetAnchor(keptHeader.rectTransform, new Vector2(0.5f, 1), new Vector2(1, 1));
            keptHeader.rectTransform.pivot = new Vector2(0.5f, 1f);
            keptHeader.rectTransform.sizeDelta = new Vector2(-8, 18);
            keptHeader.characterSpacing = 3f;

            var keptViewport = Panel5cFactory.MakeRect("KeptArea", content);
            Panel5cFactory.SetAnchor(keptViewport, new Vector2(0.5f, 0), new Vector2(1, 1));
            keptViewport.offsetMin = new Vector2(4, 0);
            keptViewport.offsetMax = new Vector2(0, -22);
            keptList = Panel5cFactory.CreateScrollList(keptViewport, "KeptList");
            Panel5cFactory.SetAnchor((RectTransform)keptList.parent, Vector2.zero, Vector2.one);
        }

        private void Refresh()
        {
            RefreshDestinations();
            RefreshKept();
        }

        private void RefreshDestinations()
        {
            for (int i = list.childCount - 1; i >= 0; i--)
                Destroy(list.GetChild(i).gameObject);

            if (destinations == null) return;

            string currentScene = SceneManager.GetActiveScene().name;
            foreach (var zone in destinations)
            {
                if (zone == null) continue;
                bool isHere = zone.sceneName == currentScene;
                bool canTravel = !isHere && zone.isUnlocked;

                string status = isHere ? "HERE" : zone.isUnlocked ? "Travel" : "Coming soon";
                Color nameColor = isHere ? Panel5cFactory.Green
                    : zone.isUnlocked ? Panel5cFactory.TextPrimary : Panel5cFactory.TextMuted;

                var row = Panel5cFactory.CreateListRow(list, zone.displayName, status,
                    nameColor, Panel5cFactory.TextMuted, interactable: canTravel);
                if (canTravel)
                {
                    var targetScene = zone.sceneName;
                    // Close the panel before travelling — PortalUI lives on the
                    // persisted HUDCanvas, so it would otherwise stay open after
                    // the destination scene loads. Autosave first so progress
                    // sticks across the trip.
                    row.onClick.AddListener(() =>
                    {
                        Close();
                        VoidBound.Save.SaveSystem.Save(GameObject.FindGameObjectWithTag("Player"));
                        SceneManager.LoadScene(targetScene);
                    });
                }
            }
        }

        private void RefreshKept()
        {
            for (int i = keptList.childCount - 1; i >= 0; i--)
                Destroy(keptList.GetChild(i).gameObject);

            var kept = PlayerDeath.PreviewKept(inventory);
            if (kept.Count == 0)
            {
                Panel5cFactory.CreateListRow(keptList, "Nothing to keep", "",
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
