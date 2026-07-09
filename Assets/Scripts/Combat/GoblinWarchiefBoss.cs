using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Combat
{
    // The Ashfields mini-boss (Phase 2). A "charge-hunter": from range it winds up
    // and CHARGES the player in a straight line (a telegraphed dash you sidestep or
    // dodge-roll through); up close it CLEAVES (a fast telegraphed melee). Standalone
    // state machine — reuses the same primitives as EnemyAI (Health/Stats/Controller/
    // CharacterAnimation, HitFeedback via Health, DamageCalculator, LootDropper) but
    // owns its own movement so the two attacks never fight over the controller.
    // Doesn't respawn: it's a set-piece, and its death drives the boss health bar +
    // loot roll + quest kill signal.
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(CharacterController))]
    public class GoblinWarchiefBoss : MonoBehaviour
    {
        private enum BossState { Idle, Chase, Cleave, Charge, Recover, Dead }

        [SerializeField] private EnemyDefinitionSO definition;
        [SerializeField] private string bossName = "Goblin Warchief";
        [SerializeField] private float aggroRange = 12f;
        [SerializeField] private float attackRange = 2.4f;
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private int baseDamage = 16;
        [SerializeField] private float gravity = -20f;

        [Header("Cleave (melee, up close)")]
        [SerializeField] private float cleaveWindUp = 0.45f;
        [SerializeField] private float cleaveInterval = 1.6f;

        [Header("Charge (telegraphed dash from range)")]
        [SerializeField] private float chargeCooldown = 4.5f;
        [SerializeField] private float chargeTelegraph = 0.7f;  // wind-up = the dodge window
        [SerializeField] private float chargeSpeed = 15f;
        [SerializeField] private float chargeDuration = 0.5f;
        [SerializeField] private float chargeHitRadius = 1.7f;
        [SerializeField] private float chargeDamageMult = 1.7f;
        [SerializeField] private float recoverTime = 0.9f;      // vulnerable pause after a charge

        private BossState state = BossState.Idle;
        private Health health;
        private StatsComponent stats;
        private CharacterController controller;
        private CharacterAnimation anim;
        private Transform model;
        private Vector3 modelBaseScale = Vector3.one;

        private Transform playerTransform;
        private Health playerHealth;
        private StatsComponent playerStats;
        private PoisonStatus playerPoison;

        private float verticalVelocity;
        private bool engaged;                 // aggroed → boss bar shown
        // Cleave
        private bool cleaveWinding;
        private float cleaveTimer;
        private float lastCleaveTime = -999f;
        // Charge
        private float chargeCdTimer;
        private bool charging;                // in the dash portion
        private float chargePhaseTimer;
        private Vector3 chargeDir;
        private bool chargeHitLanded;
        private float recoverTimer;

        public string BossName => bossName;
        public Health BossHealth => health;

        private void Awake()
        {
            health = GetComponent<Health>();
            stats = GetComponent<StatsComponent>();
            controller = GetComponent<CharacterController>();
            anim = GetComponent<CharacterAnimation>();
        }

        // Subscribe in OnEnable (not Awake) so the death handler survives a domain
        // reload — a reload clears C# event delegates but doesn't re-run Awake.
        private void OnEnable()
        {
            if (health == null) health = GetComponent<Health>();
            health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            if (health != null) health.OnDeath -= HandleDeath;
        }

        // Runtime entry for the encounter setup (mirrors EnemyAI.SetDefinition).
        public void SetDefinition(EnemyDefinitionSO def) => definition = def;

        private void Start()
        {
            if (definition != null)
            {
                stats.SetBaseStats(definition.baseStats);
                if (definition.aggroRange > 0f) aggroRange = definition.aggroRange;
                if (definition.baseDamage > 0) baseDamage = definition.baseDamage;
                if (!string.IsNullOrEmpty(definition.displayName)) bossName = definition.displayName;
                // Re-derive maxHP from the boss stats + fill it, regardless of whether
                // Health.Start already cached a default maxHP before this ran.
                health.Revive();
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerHealth = player.GetComponent<Health>();
                playerStats = player.GetComponent<StatsComponent>();
                playerPoison = player.GetComponent<PoisonStatus>();
            }

            var animator = GetComponentInChildren<Animator>();
            model = animator != null ? animator.transform : transform;
            modelBaseScale = model.localScale;
        }

        private void Update()
        {
            if (state == BossState.Dead || playerTransform == null) return;

            chargeCdTimer -= Time.deltaTime;
            float dist = Vector3.Distance(transform.position, playerTransform.position);

            switch (state)
            {
                case BossState.Idle:
                    if (dist <= aggroRange) Engage();
                    break;

                case BossState.Chase:
                    if (dist > aggroRange * 1.6f) { Disengage(); break; }
                    if (dist <= attackRange && Time.time - lastCleaveTime >= cleaveInterval) { StartCleave(); break; }
                    if (dist > attackRange && chargeCdTimer <= 0f) { StartCharge(); break; }
                    MoveToward(playerTransform.position, moveSpeed);
                    break;

                case BossState.Cleave:
                    UpdateCleave(dist);
                    break;

                case BossState.Charge:
                    UpdateCharge();
                    break;

                case BossState.Recover:
                    FacePlayer();
                    recoverTimer -= Time.deltaTime;
                    if (recoverTimer <= 0f) state = BossState.Chase;
                    ApplyGravityOnly();
                    break;
            }

            anim?.SetSpeed(state == BossState.Chase ? 1f : 0f);
        }

        private void Engage()
        {
            state = BossState.Chase;
            if (!engaged)
            {
                engaged = true;
                VoidBound.UI.BossHealthBarUI.Bind(health, bossName);
            }
        }

        private void Disengage()
        {
            state = BossState.Idle;
            if (engaged)
            {
                engaged = false;
                VoidBound.UI.BossHealthBarUI.Unbind(health);
            }
        }

        // ── Cleave: fast telegraphed melee (same coil tell as EnemyAI) ──
        private void StartCleave()
        {
            state = BossState.Cleave;
            cleaveWinding = true;
            cleaveTimer = cleaveWindUp;
            FacePlayer();
        }

        private void UpdateCleave(float dist)
        {
            FacePlayer();
            if (!cleaveWinding) return;
            cleaveTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(cleaveTimer / Mathf.Max(0.01f, cleaveWindUp));
            Coil(new Vector3(1.15f, 0.72f, 1.15f), t);
            if (cleaveTimer > 0f) return;

            cleaveWinding = false;
            lastCleaveTime = Time.time;
            ResetScale();
            anim?.TriggerAttack();
            if (playerHealth != null && !playerHealth.IsDead && dist <= attackRange * 1.35f)
            {
                int dmg = DamageCalculator.CalculateDamage(stats, playerStats, baseDamage);
                playerHealth.TakeDamage(dmg); // dodge i-frames (Invulnerable) no-op this
                TryPoison();
            }
            state = BossState.Chase;
        }

        // ── Charge: telegraphed straight-line dash ──
        private void StartCharge()
        {
            state = BossState.Charge;
            charging = false;             // telegraph phase first
            chargePhaseTimer = chargeTelegraph;
            chargeHitLanded = false;
            FacePlayer();
        }

        private void UpdateCharge()
        {
            if (!charging)
            {
                // Wind-up: track the player and coil low, then commit to a direction.
                FacePlayer();
                chargePhaseTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(chargePhaseTimer / Mathf.Max(0.01f, chargeTelegraph));
                Coil(new Vector3(1.25f, 0.6f, 1.25f), t);
                if (chargePhaseTimer > 0f) return;

                // Commit — lock the charge direction now, so a late sidestep dodges it.
                Vector3 dir = playerTransform.position - transform.position;
                dir.y = 0f;
                chargeDir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
                transform.rotation = Quaternion.LookRotation(chargeDir);
                charging = true;
                chargePhaseTimer = chargeDuration;
                chargeCdTimer = chargeCooldown;
                ResetScale();
                anim?.TriggerAttack();
                Poof.Ring(transform.position, new Color(0.9f, 0.3f, 0.2f), chargeHitRadius * 1.2f);
                return;
            }

            // Dash: barrel forward. First contact with the player lands a heavy hit.
            chargePhaseTimer -= Time.deltaTime;
            controller.Move((chargeDir * chargeSpeed + Vector3.up * NextVertical()) * Time.deltaTime);

            if (!chargeHitLanded && playerHealth != null && !playerHealth.IsDead &&
                Vector3.Distance(transform.position, playerTransform.position) <= chargeHitRadius)
            {
                chargeHitLanded = true;
                int dmg = DamageCalculator.CalculateDamage(stats, playerStats, Mathf.RoundToInt(baseDamage * chargeDamageMult));
                playerHealth.TakeDamage(dmg);
                TryPoison();
                HitStop.Punch(0.07f, 0.12f);
            }

            if (chargePhaseTimer <= 0f)
            {
                charging = false;
                recoverTimer = recoverTime;   // committed pause — the punish window
                state = BossState.Recover;
            }
        }

        private void TryPoison()
        {
            if (definition == null || !definition.appliesPoison || playerHealth == null || playerHealth.IsDead) return;
            if (Random.value > definition.poisonChance) return;
            if (playerPoison == null && playerTransform != null)
                playerPoison = playerTransform.GetComponent<PoisonStatus>();
            playerPoison?.Apply(definition.poisonDamage, definition.poisonDuration);
        }

        // ── Movement helpers ──
        private void MoveToward(Vector3 target, float speed)
        {
            Vector3 dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
            }
            controller.Move((dir * speed + Vector3.up * NextVertical()) * Time.deltaTime);
        }

        private void ApplyGravityOnly() => controller.Move(Vector3.up * NextVertical() * Time.deltaTime);

        private float NextVertical()
        {
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity = Mathf.Max(verticalVelocity + gravity * Time.deltaTime, -20f);
            return verticalVelocity;
        }

        private void FacePlayer()
        {
            if (playerTransform == null) return;
            Vector3 dir = playerTransform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        private void Coil(Vector3 squash, float t)
        {
            if (model != null)
                model.localScale = Vector3.Lerp(modelBaseScale, Vector3.Scale(modelBaseScale, squash), t);
        }

        private void ResetScale()
        {
            if (model != null) model.localScale = modelBaseScale;
        }

        private void HandleDeath()
        {
            state = BossState.Dead;
            ResetScale();
            VoidBound.Quests.QuestEvents.RaiseEnemyKilled(definition != null ? definition.enemyId : "goblin_warchief");
            if (engaged) { engaged = false; VoidBound.UI.BossHealthBarUI.Unbind(health); }

            var dropper = GetComponent<LootDropper>();
            if (dropper != null) dropper.DropLoot(transform.position);

            if (controller != null) controller.enabled = false;
            anim?.SetSpeed(0f);
            Poof.Burst(transform.position + Vector3.up * 1.2f, new Color(0.6f, 0.15f, 0.2f));
            // Set-piece: the corpse stays put (no respawn); just stop acting.
            enabled = false;
        }
    }
}
