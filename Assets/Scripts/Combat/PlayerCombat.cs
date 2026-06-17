using UnityEngine;
using UnityEngine.InputSystem;

namespace VoidBound.Combat
{
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(Health))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private InputActionReference attackAction;
        [SerializeField] private LayerMask enemyLayer = ~0;

        private StatsComponent stats;
        private float attackCooldown;
        private float lastAttackTime = -999f;

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
        }

        private void OnEnable()
        {
            if (attackAction != null && attackAction.action != null)
                attackAction.action.Enable();
        }

        private void OnDisable()
        {
            if (attackAction != null && attackAction.action != null)
                attackAction.action.Disable();
        }

        private void Update()
        {
            attackCooldown = stats.AttackInterval;

            if (attackAction == null || attackAction.action == null) return;
            if (!attackAction.action.WasPressedThisFrame()) return;
            if (Time.time - lastAttackTime < attackCooldown) return;

            PerformAttack();
        }

        private void PerformAttack()
        {
            lastAttackTime = Time.time;

            var hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
            bool hitSomething = false;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var targetHealth = hit.GetComponent<Health>();
                var targetStats = hit.GetComponent<StatsComponent>();
                if (targetHealth == null || targetHealth.IsDead || targetStats == null) continue;

                int damage = DamageCalculator.CalculateDamage(stats, targetStats, baseDamage);
                targetHealth.TakeDamage(damage);
                hitSomething = true;
            }

            if (!hitSomething)
                Debug.Log("Attack: no targets in range.");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
