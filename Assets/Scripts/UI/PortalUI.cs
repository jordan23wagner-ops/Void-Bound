using UnityEngine;
using UnityEngine.SceneManagement;
using VoidBound.Data;

namespace VoidBound.UI
{
    // Fast Travel Portal destination list, data-driven from ZoneDefinitionSO
    // (Phase 7). Unlocked, non-current zones actually load via SceneManager;
    // locked zones stay "Coming soon" (visual only, per Phase 6 spec).
    public class PortalUI : MonoBehaviour
    {
        [SerializeField] private ZoneDefinitionSO[] destinations;

        private RectTransform panel;
        private RectTransform list;

        public void Open()
        {
            Panel5cFactory.CloseOtherHomesteadPanels(gameObject, this);
            EnsureBuilt();
            panel.gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.gameObject.SetActive(false);
        }

        private void EnsureBuilt()
        {
            if (panel != null) return;

            panel = Panel5cFactory.CreatePanel(transform, "PortalPanel", "FAST TRAVEL",
                300f, 220f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);

            list = Panel5cFactory.CreateScrollList(content, "DestinationList");
            Panel5cFactory.SetAnchor((RectTransform)list.parent, Vector2.zero, Vector2.one);
        }

        private void Refresh()
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
                    row.onClick.AddListener(() => SceneManager.LoadScene(targetScene));
                }
            }
        }
    }
}
