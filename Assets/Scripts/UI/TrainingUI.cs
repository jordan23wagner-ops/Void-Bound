using UnityEngine;
using TMPro;
using VoidBound.Data;
using VoidBound.Homestead;
using VoidBound.Inventory;
using VoidBound.Skilling;

namespace VoidBound.UI
{
    // Guild stat-training panel. One UI shared by all three guilds — the
    // interacting GuildStation supplies the stat, costs, and XP amounts.
    public class TrainingUI : MonoBehaviour
    {
        private RectTransform panel;
        private TextMeshProUGUI title;
        private TextMeshProUGUI info;
        private TextMeshProUGUI costLabel;
        private UnityEngine.UI.Button trainButton;

        private GuildStation station;
        private PlayerSkills skills;
        private PlayerCurrency currency;

        public void Open(GuildStation guildStation, GameObject instigator)
        {
            station = guildStation;
            skills = instigator.GetComponent<PlayerSkills>();
            currency = instigator.GetComponent<PlayerCurrency>();

            Panel5cFactory.CloseOtherHomesteadPanels(gameObject, this);
            EnsureBuilt();
            panel.gameObject.SetActive(true);
            StationProximityCloser.Track(gameObject, this, guildStation, Close);
            title.text = station.GuildName.ToUpperInvariant();
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

            panel = Panel5cFactory.CreatePanel(transform, "TrainingPanel", "GUILD",
                340f, 240f, out var content, out var closeBtn);
            closeBtn.onClick.AddListener(Close);
            title = panel.Find("Header/Title").GetComponent<TextMeshProUGUI>();

            info = Panel5cFactory.CreateLabel(content, "Info", "", 12f, Panel5cFactory.TextPrimary);
            Panel5cFactory.SetAnchor(info.rectTransform, new Vector2(0, 1), new Vector2(1, 1));
            info.rectTransform.pivot = new Vector2(0.5f, 1f);
            info.rectTransform.sizeDelta = new Vector2(-8, 84);
            info.rectTransform.anchoredPosition = new Vector2(0, -4);
            info.alignment = TextAlignmentOptions.TopLeft;

            costLabel = Panel5cFactory.CreateLabel(content, "Cost", "", 12f, Panel5cFactory.Gold);
            Panel5cFactory.SetAnchor(costLabel.rectTransform, new Vector2(0, 0), new Vector2(1, 0));
            costLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            costLabel.rectTransform.anchoredPosition = new Vector2(0, 46);
            costLabel.rectTransform.sizeDelta = new Vector2(-8, 20);
            costLabel.alignment = TextAlignmentOptions.Center;

            trainButton = Panel5cFactory.CreateActionButton(content, "TRAIN");
            var btnRT = (RectTransform)trainButton.transform;
            Panel5cFactory.SetAnchor(btnRT, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            btnRT.pivot = new Vector2(0.5f, 0f);
            btnRT.sizeDelta = new Vector2(120, 30);
            btnRT.anchoredPosition = new Vector2(0, 8);
            trainButton.onClick.AddListener(Train);
        }

        private void Refresh()
        {
            if (station == null || skills == null) return;

            int level = skills.GetLevel(station.TrainedStat);
            int xp = skills.GetXP(station.TrainedStat);
            int xpNext = skills.GetXPToNext(station.TrainedStat);
            int cost = station.CostForLevel(level);
            int gold = currency != null ? currency.Gold : 0;

            info.text = $"Trains: {StatShortName(station.TrainedStat)}\n" +
                        $"Level: {level}\n" +
                        $"XP: {xp} / {xpNext}\n" +
                        $"Session: +{station.XpPerSession} XP (+{station.VigXpPerSession} VIG XP)";
            costLabel.text = $"Cost: {cost}g   (Gold {gold})";
            trainButton.interactable = gold >= cost;
        }

        private void Train()
        {
            if (station == null || skills == null || currency == null) return;

            int cost = station.CostForLevel(skills.GetLevel(station.TrainedStat));
            if (!currency.SpendGold(cost)) return;

            skills.AddXP(station.TrainedStat, station.XpPerSession);
            if (station.VigXpPerSession > 0)
                skills.AddXP(SkillType.CombatVIG, station.VigXpPerSession);

            Combat.FloatingDamageNumber.SpawnText(currency.transform.position,
                $"+{station.XpPerSession} {StatShortName(station.TrainedStat)} XP",
                Panel5cFactory.Green);
            Refresh();
        }

        private static string StatShortName(SkillType type) => type switch
        {
            SkillType.CombatSTR => "STR",
            SkillType.CombatDEX => "DEX",
            SkillType.CombatINT => "INT",
            SkillType.CombatVIG => "VIG",
            _ => type.ToString()
        };
    }
}
