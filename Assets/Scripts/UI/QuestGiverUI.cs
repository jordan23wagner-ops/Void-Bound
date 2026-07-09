using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBound.Data;
using VoidBound.Quests;

namespace VoidBound.UI
{
    // The NPC dialog panel: offers a quest, shows live objective progress while
    // it's active, and hands out the reward on turn-in. Lives on HUDCanvas and
    // builds itself on first Open() via Panel5cFactory. One action button whose
    // meaning (Accept / Turn In / disabled) depends on the player's quest state.
    public class QuestGiverUI : MonoBehaviour
    {
        private RectTransform panel;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI bodyLabel;
        private RectTransform objectiveList;
        private Button actionButton;
        private TextMeshProUGUI actionLabel;

        private QuestSO quest;
        private GameObject player;
        private PlayerQuests quests;

        public void Open(QuestSO q, GameObject instigator, Core.Interactable station)
        {
            quest = q;
            player = instigator;
            quests = instigator != null ? instigator.GetComponent<PlayerQuests>() : null;

            Panel5cFactory.CloseOtherHomesteadPanels(gameObject, this);
            EnsureBuilt();
            panel.gameObject.SetActive(true);
            StationProximityCloser.Track(gameObject, this, station, Close);
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.gameObject.SetActive(false);
            StationProximityCloser.Untrack(gameObject, this);
        }

        private void EnsureBuilt()
        {
            if (panel != null) return;

            panel = Panel5cFactory.CreatePanel(transform, "QuestGiverPanel", "QUEST",
                480f, 380f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);

            titleLabel = Panel5cFactory.CreateLabel(content, "QuestTitle", "",
                14f, Panel5cFactory.Gold);
            Panel5cFactory.SetAnchor(titleLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1));
            titleLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleLabel.rectTransform.sizeDelta = new Vector2(0, 22);
            titleLabel.fontStyle = FontStyles.Bold;

            bodyLabel = Panel5cFactory.CreateLabel(content, "QuestBody", "",
                12f, Panel5cFactory.TextPrimary);
            Panel5cFactory.SetAnchor(bodyLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1));
            bodyLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            bodyLabel.rectTransform.anchoredPosition = new Vector2(0, -26);
            bodyLabel.rectTransform.sizeDelta = new Vector2(0, 96);
            bodyLabel.alignment = TextAlignmentOptions.TopLeft;
            bodyLabel.textWrappingMode = TextWrappingModes.Normal;

            var objViewport = Panel5cFactory.MakeRect("ObjArea", content);
            Panel5cFactory.SetAnchor(objViewport, new Vector2(0, 0), new Vector2(1, 1));
            objViewport.offsetMin = new Vector2(0, 44);
            objViewport.offsetMax = new Vector2(0, -128);
            objectiveList = Panel5cFactory.CreateScrollList(objViewport, "ObjList");
            Panel5cFactory.SetAnchor((RectTransform)objectiveList.parent, Vector2.zero, Vector2.one);

            actionButton = Panel5cFactory.CreateActionButton(content, "");
            var art = actionButton.GetComponent<RectTransform>();
            Panel5cFactory.SetAnchor(art, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            art.pivot = new Vector2(0.5f, 0f);
            art.sizeDelta = new Vector2(160, 34);
            art.anchoredPosition = new Vector2(0, 4);
            actionLabel = actionButton.GetComponentInChildren<TextMeshProUGUI>();
            actionButton.onClick.AddListener(OnAction);
        }

        private void Refresh()
        {
            ClearChildren(objectiveList);
            if (quest == null) { titleLabel.text = "—"; bodyLabel.text = ""; return; }

            titleLabel.text = quest.title;

            bool isCompleted = quests != null && quests.IsCompleted(quest.questId);
            bool isActive = quests != null && quests.HasActive && quests.Active == quest;
            bool allDone = isActive && quests.AllObjectivesDone();

            // Objective rows (skip when already turned in — nothing to show).
            if (!isCompleted && quest.objectives != null)
            {
                for (int i = 0; i < quest.objectives.Length; i++)
                {
                    var obj = quest.objectives[i];
                    int have = isActive ? quests.GetProgress(i) : 0;
                    bool done = have >= obj.required;
                    string prefix = done ? "[x] " : "[ ] ";
                    var row = Panel5cFactory.CreateListRow(objectiveList,
                        prefix + obj.label, $"{have}/{obj.required}",
                        done ? (Color)Panel5cFactory.Green : (Color)Panel5cFactory.TextPrimary,
                        done ? (Color)Panel5cFactory.Green : (Color)Panel5cFactory.TextMuted,
                        false);
                }
            }

            if (isCompleted)
            {
                bodyLabel.text = "You have already served this charge. My thanks stand.";
                SetAction("COMPLETED", false);
            }
            else if (allDone)
            {
                bodyLabel.text = string.IsNullOrEmpty(quest.turnInText) ? "It is done. Come, claim your due." : quest.turnInText;
                SetAction("TURN IN", true);
            }
            else if (isActive)
            {
                bodyLabel.text = string.IsNullOrEmpty(quest.activeText) ? "The work is not yet finished." : quest.activeText;
                SetAction("IN PROGRESS", false);
            }
            else if (quests != null && quests.HasActive)
            {
                bodyLabel.text = "Finish the charge you already carry before you take another.";
                SetAction("BUSY", false);
            }
            else
            {
                bodyLabel.text = string.IsNullOrEmpty(quest.offerText) ? "Will you take up this charge?" : quest.offerText;
                SetAction("ACCEPT", true);
            }
        }

        private void SetAction(string label, bool interactable)
        {
            if (actionLabel != null) actionLabel.text = label;
            if (actionButton != null) actionButton.interactable = interactable;
        }

        private void OnAction()
        {
            if (quest == null || quests == null) return;

            if (quests.HasActive && quests.Active == quest && quests.AllObjectivesDone())
            {
                if (quests.TurnIn(player))
                    Combat.FloatingDamageNumber.SpawnText(player.transform.position + Vector3.up * 1.6f,
                        "Quest complete!", Panel5cFactory.Gold);
            }
            else if (quests.CanAccept(quest))
            {
                quests.Accept(quest);
                Combat.FloatingDamageNumber.SpawnText(player.transform.position + Vector3.up * 1.6f,
                    "Quest accepted", Panel5cFactory.Green);
            }
            Refresh();
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
