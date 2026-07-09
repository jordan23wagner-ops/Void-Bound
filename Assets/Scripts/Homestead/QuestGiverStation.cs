using UnityEngine;
using VoidBound.Core;
using VoidBound.Data;
using VoidBound.UI;

namespace VoidBound.Homestead
{
    // A stationary NPC that offers one quest. Walking into range opens the
    // QuestGiverUI, which handles accept / progress / turn-in based on the
    // player's PlayerQuests state. Mirrors MerchantStation's panel-open pattern.
    public class QuestGiverStation : Interactable
    {
        [SerializeField] private QuestSO quest;

        public QuestSO Quest => quest;
        public void SetQuest(QuestSO q) => quest = q;

        public override bool RepeatOnProximity => false;

        public override void OnInteract(GameObject instigator)
        {
            var ui = Object.FindAnyObjectByType<QuestGiverUI>();
            if (ui != null)
                ui.Open(quest, instigator, this);
        }
    }
}
