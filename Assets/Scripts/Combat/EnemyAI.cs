using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Combat
{
    public enum EnemyState { Idle, Chase, Attack, Dead }

    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(CharacterController))]
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private EnemyDefinitionSO definition;
        [SerializeField] private float aggroRange = 8f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private int baseDamage = 5;
        [SerializeField] private float gravity = -20f;

        [Header("Poison (§4) — mirrors the definition; off by default")]
        [SerializeField] private bool appliesPoison;
        [SerializeField, Range(0f, 1f)] private float poisonChance = 1f;
        [SerializeField] private int poisonDamage = 8;
        [SerializeField] private float poisonDuration = 6f;

        [Header("Attack telegraph")]
        [SerializeField] private float windUpTime = 0.5f;   // coil time before a strike lands
        private bool windingUp;
        private float windUpTimer;
        private Transform model;
        private Vector3 modelBaseScale = Vector3.one;

        private EnemyState state = EnemyState.Idle;
        private Transform playerTransform;
        private Health health;
        private StatsComponent stats;
        private StatsComponent playerStats;
        private Health playerHealth;
        private PoisonStatus playerPoison;
        private CharacterController controller;
        private CharacterAnimation anim;
        private float lastAttackTime;
        private float verticalVelocity;

        private void Awake()
        {
            health = GetComponent<Health>();
            stats = GetComponent<StatsComponent>();
            controller = GetComponent<CharacterController>();
            anim = GetComponent<CharacterAnimation>();
            health.OnDeath += HandleDeath;
        }

        // Runtime entry point for spawners (EnemySpawner): assign the definition
        // after AddComponent but before Start runs, so Start() applies its stats/
        // ranges/damage/poison. The editor placement scripts set the private
        // [SerializeField] via SerializedObject instead; this is the runtime path.
        public void SetDefinition(EnemyDefinitionSO def) => definition = def;

        private void Start()
        {
            if (definition != null)
            {
                stats.SetBaseStats(definition.baseStats);
                aggroRange = definition.aggroRange;
                attackRange = definition.attackRange;
                moveSpeed = definition.moveSpeed;
                baseDamage = definition.baseDamage;
                appliesPoison = definition.appliesPoison;
                poisonChance = definition.poisonChance;
                poisonDamage = definition.poisonDamage;
                poisonDuration = definition.poisonDuration;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerStats = player.GetComponent<StatsComponent>();
                playerHealth = player.GetComponent<Health>();
                playerPoison = player.GetComponent<PoisonStatus>();
            }

            // The model transform we squash during a wind-up (mirrors HitFeedback).
            var animator = GetComponentInChildren<Animator>();
            model = animator != null ? animator.transform : transform;
            modelBaseScale = model.localScale;
        }

        private void Update()
        {
            if (state == EnemyState.Dead || playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            switch (state)
            {
                case EnemyState.Idle:
                    if (distToPlayer <= aggroRange)
                        state = EnemyState.Chase;
                    break;

                case EnemyState.Chase:
                    if (distToPlayer > aggroRange * 1.5f)
                    {
                        state = EnemyState.Idle;
                        break;
                    }
                    if (distToPlayer <= attackRange)
                    {
                        state = EnemyState.Attack;
                        break;
                    }
                    ChasePlayer();
                    break;

                case EnemyState.Attack:
                    // While winding up, the enemy is committed — it won't drop back
                    // to Chase mid-telegraph, so the player can dodge the coiled hit.
                    if (!windingUp && distToPlayer > attackRange * 1.2f)
                    {
                        state = EnemyState.Chase;
                        break;
                    }
                    TryAttackPlayer(distToPlayer);
                    break;
            }

            anim?.SetSpeed(state == EnemyState.Chase ? 1f : 0f);
            ApplyGravity();
        }

        private void ChasePlayer()
        {
            Vector3 direction = (playerTransform.position - transform.position);
            direction.y = 0f;
            direction.Normalize();

            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(direction);

            controller.Move(direction * moveSpeed * Time.deltaTime + Vector3.up * verticalVelocity * Time.deltaTime);
        }

        private void TryAttackPlayer(float distToPlayer)
        {
            if (playerHealth == null || playerHealth.IsDead) return;

            // Coiling: telegraph a strike, then land it after windUpTime. During the
            // wind-up the enemy holds still and squashes down — the tell that gives
            // the player a window to dodge (i-frames) or step out of reach.
            if (windingUp)
            {
                FacePlayer();
                windUpTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(windUpTimer / Mathf.Max(0.01f, windUpTime));
                if (model != null)
                    model.localScale = Vector3.Lerp(modelBaseScale,
                        Vector3.Scale(modelBaseScale, new Vector3(1.18f, 0.68f, 1.18f)), t);
                if (windUpTimer <= 0f)
                    ReleaseAttack(distToPlayer);
                return;
            }

            float attackInterval = stats.AttackInterval;
            if (Time.time - lastAttackTime < attackInterval) return;

            FacePlayer();
            windingUp = true;
            windUpTimer = windUpTime;
        }

        // Resolves a committed strike at the end of a wind-up. The hit only lands if
        // the player is still within reach; a dodge that carries them out (or whose
        // i-frames make TakeDamage a no-op) turns it into a whiff.
        private void ReleaseAttack(float distToPlayer)
        {
            windingUp = false;
            lastAttackTime = Time.time;
            if (model != null) model.localScale = modelBaseScale;
            anim?.TriggerAttack();

            if (playerHealth == null || playerHealth.IsDead) return;
            if (distToPlayer > attackRange * 1.35f)
            {
                Debug.Log($"{gameObject.name}'s strike whiffs — player dodged clear.");
                return;
            }

            int damage = DamageCalculator.CalculateDamage(stats, playerStats, baseDamage);
            playerHealth.TakeDamage(damage); // dodge i-frames (Health.Invulnerable) no-op this

            // Poison-coated attackers apply a DoT on a landed, non-killing hit (§4).
            if (appliesPoison && !playerHealth.IsDead && Random.value <= poisonChance)
            {
                if (playerPoison == null && playerTransform != null)
                    playerPoison = playerTransform.GetComponent<PoisonStatus>();
                playerPoison?.Apply(poisonDamage, poisonDuration);
            }

            Debug.Log($"{gameObject.name} strikes Player for {damage} damage.");
        }

        private void FacePlayer()
        {
            if (playerTransform == null) return;
            Vector3 direction = (playerTransform.position - transform.position);
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        private void ApplyGravity()
        {
            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f)
                    verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
                verticalVelocity = Mathf.Max(verticalVelocity, -20f);
            }

            if (!controller.isGrounded)
                controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        private void HandleDeath()
        {
            state = EnemyState.Dead;
            Vector3 deathPos = transform.position;

            var dropper = GetComponent<LootDropper>();
            if (dropper != null)
                dropper.DropLoot(deathPos);

            if (controller != null) controller.enabled = false;
            anim?.SetSpeed(0f);
            // Keep the corpse visible briefly so the Death clip plays before despawn.
            StartCoroutine(DespawnAfterDeath());
        }

        private System.Collections.IEnumerator DespawnAfterDeath()
        {
            yield return new WaitForSeconds(0.9f);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeath -= HandleDeath;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aggroRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
