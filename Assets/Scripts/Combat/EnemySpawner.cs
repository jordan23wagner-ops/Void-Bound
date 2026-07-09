using System.Collections.Generic;
using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Combat
{
    // Runtime spawner that keeps a small pocket of enemies alive around a point
    // so the Ashfields zone stays populated instead of being cleared once. It
    // assembles enemies the SAME way the editor placement scripts do
    // (SceneSetupTools.SpawnEnemy / Phase7AshfieldsSetup.SpawnEnemyIfMissing):
    //   CharacterController -> StatsComponent -> Health -> EnemyAI(definition)
    //   -> LootDropper(SetLootTable) -> HealthBar child.
    // EnemyAI.Start reads the definition for stats/damage/ranges/poison, and
    // LootDropper handles the drop on death, so this class only has to build the
    // GameObject and refill the pack when a member dies.
    public class EnemySpawner : MonoBehaviour
    {
        [Header("What to spawn")]
        [SerializeField] private EnemyDefinitionSO definition;
        [SerializeField] private LootTableSO lootTable;
        [SerializeField] private EnemyTier tier = EnemyTier.Weak;

        [Header("Rigged body (matches CharacterModelSwap output on placed enemies)")]
        [Tooltip("Rigged character FBX (e.g. Goblin_Warrior.fbx); falls back to a capsule if null.")]
        [SerializeField] private GameObject modelFbx;
        [SerializeField] private RuntimeAnimatorController animatorController;
        [Tooltip("Per-submesh materials (skin + baked gear), ordered to the model's slots.")]
        [SerializeField] private Material[] slotMaterials;
        [SerializeField, Min(0.1f)] private float modelScale = 1f;

        [Header("Pack")]
        [Tooltip("How many live enemies this spawner tries to keep alive.")]
        [SerializeField, Min(1)] private int maxAlive = 2;
        [Tooltip("Seconds after a death before the slot is refilled.")]
        [SerializeField, Min(0f)] private float respawnDelay = 8f;
        [Tooltip("Enemies spawn on the ground within this radius of the spawner.")]
        [SerializeField, Min(0f)] private float spawnRadius = 3f;
        [Tooltip("If the player is inside this radius, hold respawns so foes don't pop in on top of them.")]
        [SerializeField, Min(0f)] private float playerHoldRadius = 6f;

        private readonly List<Health> alive = new List<Health>();
        private float nextSpawnTime;
        private Transform player;

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;

            // Don't pop the initial pack in on top of a player who's already
            // standing here on scene load — Update() fills the deferred slots
            // once they leave playerHoldRadius, same as any later respawn.
            bool held = player != null && playerHoldRadius > 0f &&
                Vector3.Distance(transform.position, player.position) < playerHoldRadius;
            if (!held)
                for (int i = 0; i < maxAlive; i++) SpawnOne();
        }

        private void Update()
        {
            // Drop any dead/destroyed members from the roster.
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                if (alive[i] == null || alive[i].IsDead)
                    alive.RemoveAt(i);
            }

            if (alive.Count >= maxAlive) return;
            if (Time.time < nextSpawnTime) return;

            // Don't pop a foe in right next to the player.
            if (player != null && playerHoldRadius > 0f &&
                Vector3.Distance(transform.position, player.position) < playerHoldRadius)
                return;

            SpawnOne();
            nextSpawnTime = Time.time + respawnDelay;
        }

        private void SpawnOne()
        {
            if (definition == null)
            {
                Debug.LogWarning($"[EnemySpawner] '{name}' has no EnemyDefinition assigned — nothing to spawn.");
                enabled = false;
                return;
            }

            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = transform.position + new Vector3(offset.x, 0f, offset.y);

            GameObject go = BuildBody(pos);
            go.name = $"{definition.displayName} (spawned)";
            go.transform.position = pos;

            var cc = go.GetComponent<CharacterController>();
            if (cc == null) cc = go.AddComponent<CharacterController>();
            cc.radius = 0.35f;
            cc.height = 1.4f;
            cc.center = new Vector3(0f, 0.7f, 0f);

            if (go.GetComponent<StatsComponent>() == null) go.AddComponent<StatsComponent>();

            var health = go.GetComponent<Health>();
            if (health == null) health = go.AddComponent<Health>();

            var ai = go.GetComponent<EnemyAI>();
            if (ai == null) ai = go.AddComponent<EnemyAI>();
            ai.SetDefinition(definition);

            var dropper = go.GetComponent<LootDropper>();
            if (dropper == null) dropper = go.AddComponent<LootDropper>();
            dropper.SetLootTable(lootTable != null ? lootTable : definition.lootTable, tier);

            if (go.GetComponentInChildren<HealthBar>(true) == null)
            {
                var hb = new GameObject("HealthBar");
                hb.transform.SetParent(go.transform, false);
                hb.AddComponent<HealthBar>();
            }

            alive.Add(health);
        }

        private GameObject BuildBody(Vector3 pos)
        {
            var go = new GameObject("EnemyBody");
            go.transform.position = pos;

            if (modelFbx != null)
            {
                // Rigged model as a child, exactly like the placed enemies + boss.
                RiggedModelBuilder.Attach(go, modelFbx, animatorController, slotMaterials, modelScale);
            }
            else
            {
                // Fallback so the spawner still works if no model is wired.
                var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                var col = cap.GetComponent<Collider>();
                if (col != null) Destroy(col);
                cap.transform.SetParent(go.transform, false);
                cap.transform.localPosition = new Vector3(0f, 1f, 0f);
            }
            return go;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, playerHoldRadius);
        }
    }
}
