using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.Skilling;

namespace VoidBound.Quests
{
    // Per-player quest log. Holds one active quest at a time, advances its
    // objectives from QuestEvents, and grants rewards on turn-in. State persists
    // through SaveSystem (active quest id + progress + completed ids). Lives on
    // the Player alongside PlayerCurrency / MaterialInventory / PlayerSkills.
    public class PlayerQuests : MonoBehaviour
    {
        private QuestSO active;
        private int[] progress;                                   // per-objective counter
        private readonly HashSet<string> completed = new();

        // Fired whenever the active quest, its progress, or completion changes —
        // the tracker/giver UIs redraw off this.
        public event Action OnQuestChanged;

        public QuestSO Active => active;
        public bool HasActive => active != null;
        public string ActiveQuestId => active != null ? active.questId : "";
        public IReadOnlyCollection<string> Completed => completed;

        private void OnEnable()
        {
            QuestEvents.EnemyKilled += HandleEnemyKilled;
            QuestEvents.Gathered += HandleGathered;
            QuestEvents.Crafted += HandleCrafted;
        }

        private void OnDisable()
        {
            QuestEvents.EnemyKilled -= HandleEnemyKilled;
            QuestEvents.Gathered -= HandleGathered;
            QuestEvents.Crafted -= HandleCrafted;
        }

        public bool IsCompleted(string questId) => completed.Contains(questId);

        // A quest can be accepted only if nothing else is active and it hasn't
        // already been finished.
        public bool CanAccept(QuestSO quest) =>
            quest != null && active == null && !completed.Contains(quest.questId);

        public void Accept(QuestSO quest)
        {
            if (!CanAccept(quest)) return;
            active = quest;
            progress = new int[quest.objectives != null ? quest.objectives.Length : 0];
            Debug.Log($"[Quest] Accepted '{quest.title}'.");
            OnQuestChanged?.Invoke();
        }

        public int GetProgress(int i) => (progress != null && i >= 0 && i < progress.Length) ? progress[i] : 0;

        public bool ObjectiveDone(int i)
        {
            if (active == null || active.objectives == null || i < 0 || i >= active.objectives.Length) return false;
            return GetProgress(i) >= active.objectives[i].required;
        }

        public bool AllObjectivesDone()
        {
            if (active == null || active.objectives == null) return false;
            for (int i = 0; i < active.objectives.Length; i++)
                if (!ObjectiveDone(i)) return false;
            return true;
        }

        // Complete the active quest: grant its reward, file it under completed,
        // and clear the slot. Returns false if it isn't ready to hand in.
        public bool TurnIn(GameObject player)
        {
            if (active == null || !AllObjectivesDone()) return false;

            GrantReward(player, active.reward);
            completed.Add(active.questId);
            Debug.Log($"[Quest] Turned in '{active.title}'.");
            active = null;
            progress = null;
            OnQuestChanged?.Invoke();
            return true;
        }

        private void GrantReward(GameObject player, QuestReward r)
        {
            if (r.xpAmount > 0)
            {
                var skills = player.GetComponent<PlayerSkills>();
                skills?.AddXP(r.xpSkill, r.xpAmount);
            }
            if (r.gold > 0 || r.voidShards > 0)
            {
                var cur = player.GetComponent<PlayerCurrency>();
                if (cur != null)
                {
                    if (r.gold > 0) cur.AddGold(r.gold);
                    if (r.voidShards > 0) cur.AddVoidShards(r.voidShards);
                }
            }
            if (r.rewardMaterial != null && r.rewardMaterialQty > 0)
            {
                var mat = player.GetComponent<MaterialInventory>();
                mat?.AddMaterial(r.rewardMaterial, r.rewardMaterialQty);
            }
            if (r.rewardGear != null)
            {
                var inv = player.GetComponent<PlayerInventory>();
                inv?.AddItem(r.rewardGear);
            }
        }

        // ── Objective advancement ──
        private void HandleEnemyKilled(string enemyId)
        {
            Advance(QuestObjectiveType.Kill, obj =>
                string.IsNullOrEmpty(obj.targetId) || obj.targetId == enemyId, 1);
        }

        private void HandleGathered(string materialId, int quantity)
        {
            Advance(QuestObjectiveType.Gather, obj => obj.targetId == materialId, quantity);
        }

        private void HandleCrafted(string recipeId, string outputId)
        {
            Advance(QuestObjectiveType.Craft, obj =>
                obj.targetId == recipeId || obj.targetId == outputId, 1);
        }

        // Bump every matching, not-yet-complete objective of the active quest.
        private void Advance(QuestObjectiveType type, Func<QuestObjective, bool> matches, int amount)
        {
            if (active == null || active.objectives == null || progress == null || amount <= 0) return;

            bool changed = false;
            for (int i = 0; i < active.objectives.Length; i++)
            {
                var obj = active.objectives[i];
                if (obj.type != type || !matches(obj)) continue;
                if (progress[i] >= obj.required) continue;
                progress[i] = Mathf.Min(obj.required, progress[i] + amount);
                changed = true;
            }
            if (changed) OnQuestChanged?.Invoke();
        }

        // ── Save/load ──
        public int[] ProgressSnapshot => progress != null ? (int[])progress.Clone() : new int[0];

        public void LoadState(QuestSO activeQuest, int[] savedProgress, List<string> completedIds)
        {
            completed.Clear();
            if (completedIds != null)
                foreach (var id in completedIds)
                    if (!string.IsNullOrEmpty(id)) completed.Add(id);

            active = activeQuest;
            if (active != null)
            {
                int n = active.objectives != null ? active.objectives.Length : 0;
                progress = new int[n];
                if (savedProgress != null)
                    for (int i = 0; i < n && i < savedProgress.Length; i++)
                        progress[i] = Mathf.Clamp(savedProgress[i], 0, active.objectives[i].required);
            }
            else progress = null;

            OnQuestChanged?.Invoke();
        }

        public void WipeState()
        {
            active = null;
            progress = null;
            completed.Clear();
            OnQuestChanged?.Invoke();
        }
    }
}
