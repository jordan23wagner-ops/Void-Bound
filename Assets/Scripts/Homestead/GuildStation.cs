using UnityEngine;
using VoidBound.Core;
using VoidBound.Data;
using VoidBound.UI;

namespace VoidBound.Homestead
{
    // Guild stat training (Warriors' → STR, Rangers' → DEX, Mages' → INT).
    // One script, three configured scene instances. Cost/XP numbers are
    // FALLBACK values pending RunePortal source confirmation — tunable.
    // VIG side-XP mirrors the 50% combat ratio from CombatXPCalculator.
    public class GuildStation : Interactable
    {
        [SerializeField] private string guildName = "Guild";
        [SerializeField] private SkillType trainedStat = SkillType.CombatSTR;
        [SerializeField] private int baseCostPerLevel = 10;
        [SerializeField] private int xpPerSession = 50;
        [SerializeField] private int vigXpPerSession = 25;

        public string GuildName => guildName;
        public SkillType TrainedStat => trainedStat;
        public int XpPerSession => xpPerSession;
        public int VigXpPerSession => vigXpPerSession;

        public override bool RepeatOnProximity => false;

        public int CostForLevel(int currentLevel) =>
            Mathf.Max(baseCostPerLevel, baseCostPerLevel * currentLevel);

        public override void OnInteract(GameObject instigator)
        {
            var ui = Object.FindAnyObjectByType<TrainingUI>();
            if (ui != null)
                ui.Open(this, instigator);
        }
    }
}
