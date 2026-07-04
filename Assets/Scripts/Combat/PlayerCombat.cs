using UnityEngine;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.Skilling;

namespace VoidBound.Combat
{
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(Health))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private LayerMask enemyLayer = ~0;

        private StatsComponent stats;
        private PlayerInventory inventory;
        private PlayerSkills skills;
        private CharacterAnimation anim;
        private float lastAttackTime = -999f;

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
            inventory = GetComponent<PlayerInventory>();
            skills = GetComponent<PlayerSkills>();
            anim = GetComponent<CharacterAnimation>();
        }

        private void Update()
        {
            float attackCooldown = stats.AttackInterval;
            if (Time.time - lastAttackTime < attackCooldown) return;

            var hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

            float closestDist = float.MaxValue;
            Health closestTarget = null;
            StatsComponent closestStats = null;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var targetHealth = hit.GetComponent<Health>();
                var targetStats = hit.GetComponent<StatsComponent>();
                if (targetHealth == null || targetHealth.IsDead || targetStats == null) continue;

                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestTarget = targetHealth;
                    closestStats = targetStats;
                }
            }

            if (closestTarget == null) return;

            lastAttackTime = Time.time;
            int damage = DamageCalculator.CalculateDamage(stats, closestStats, baseDamage);
            closestTarget.TakeDamage(damage);
            anim?.TriggerAttack();

            WeaponType equippedWeapon = GetEquippedWeaponType();
            CombatXPCalculator.AwardCombatXP(skills, equippedWeapon, damage);

            Vector3 faceDir = closestTarget.transform.position - transform.position;
            faceDir.y = 0f;
            if (faceDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(faceDir);
        }

        private WeaponType GetEquippedWeaponType()
        {
            if (inventory == null) return WeaponType.Sword;
            var weapon = inventory.GetEquipped(EquipmentSlot.Weapon);
            return weapon != null ? weapon.weaponType : WeaponType.Sword;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
