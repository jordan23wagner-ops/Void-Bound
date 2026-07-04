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

        private EnemyState state = EnemyState.Idle;
        private Transform playerTransform;
        private Health health;
        private StatsComponent stats;
        private StatsComponent playerStats;
        private Health playerHealth;
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

        private void Start()
        {
            if (definition != null)
            {
                stats.SetBaseStats(definition.baseStats);
                aggroRange = definition.aggroRange;
                attackRange = definition.attackRange;
                moveSpeed = definition.moveSpeed;
                baseDamage = definition.baseDamage;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerStats = player.GetComponent<StatsComponent>();
                playerHealth = player.GetComponent<Health>();
            }
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
                    if (distToPlayer > attackRange * 1.2f)
                    {
                        state = EnemyState.Chase;
                        break;
                    }
                    TryAttackPlayer();
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

        private void TryAttackPlayer()
        {
            if (playerHealth == null || playerHealth.IsDead) return;

            Vector3 direction = (playerTransform.position - transform.position);
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(direction);

            float attackInterval = stats.AttackInterval;
            if (Time.time - lastAttackTime < attackInterval) return;

            lastAttackTime = Time.time;
            int damage = DamageCalculator.CalculateDamage(stats, playerStats, baseDamage);
            playerHealth.TakeDamage(damage);
            anim?.TriggerAttack();
            Debug.Log($"{gameObject.name} attacks Player for {damage} damage.");
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
