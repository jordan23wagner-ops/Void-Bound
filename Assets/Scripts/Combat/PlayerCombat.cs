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
        [SerializeField] private float attackRange = 2.5f;         // melee reach
        [SerializeField] private float rangedAttackRange = 11f;    // bow/staff engage distance
        [SerializeField] private LayerMask enemyLayer = ~0;

        private StatsComponent stats;
        private PlayerInventory inventory;
        private PlayerSkills skills;
        private CharacterAnimation anim;
        private MaterialInventory matInv;
        private float lastAttackTime = -999f;

        // Ranged fires cost 1 Arrow; casts cost 1 rune (first available element).
        private static readonly string[] RuneIds = { "rune_fire", "rune_water", "rune_air", "rune_earth" };

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
            inventory = GetComponent<PlayerInventory>();
            skills = GetComponent<PlayerSkills>();
            anim = GetComponent<CharacterAnimation>();
            matInv = GetComponent<MaterialInventory>();
        }

        private void Update()
        {
            float attackCooldown = stats.AttackInterval;
            if (Time.time - lastAttackTime < attackCooldown) return;

            WeaponType weaponType = GetEquippedWeaponType();
            var style = WeaponStyleMap.GetStyle(weaponType);
            float range = style == WeaponStyle.Melee ? attackRange : rangedAttackRange;

            var hits = Physics.OverlapSphere(transform.position, range, enemyLayer);

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

            // Face the target before firing/striking.
            Vector3 faceDir = closestTarget.transform.position - transform.position;
            faceDir.y = 0f;
            if (faceDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(faceDir);

            if (style == WeaponStyle.Melee)
            {
                int damage = DamageCalculator.CalculateDamage(stats, closestStats, baseDamage);
                closestTarget.TakeDamage(damage);
                CombatXPCalculator.AwardCombatXP(skills, weaponType, damage);
                anim?.TriggerAttack();
            }
            else
            {
                // Ranged/magic costs ammo: 1 Arrow per shot, 1 rune per cast. No
                // ammo → can't attack at range (stock up before a run, §5.7).
                if (!ConsumeAmmo(style))
                {
                    FloatingDamageNumber.SpawnText(transform.position + Vector3.up * 1.6f,
                        style == WeaponStyle.Ranged ? "Out of arrows" : "Out of runes",
                        new Color(0.9f, 0.6f, 0.3f));
                    return;
                }

                // Ranged/magic: loose a homing projectile that resolves damage +
                // XP on impact, so the fight plays out at a distance.
                var kind = style == WeaponStyle.Ranged ? ProjectileKind.Arrow : ProjectileKind.Magic;
                Vector3 muzzle = transform.position + Vector3.up * 1.2f + transform.forward * 0.4f;
                var wt = weaponType;
                Projectile.Spawn(muzzle, stats, closestTarget, closestStats, baseDamage, kind,
                    d => CombatXPCalculator.AwardCombatXP(skills, wt, d));
                if (style == WeaponStyle.Ranged) anim?.TriggerShoot();
                else anim?.TriggerCast();
            }
        }

        private WeaponType GetEquippedWeaponType()
        {
            if (inventory == null) return WeaponType.Sword;
            var weapon = inventory.GetEquipped(EquipmentSlot.Weapon);
            return weapon != null ? weapon.weaponType : WeaponType.Sword;
        }

        // Spends ammo for a ranged/magic fire. Ranged → 1 Arrow; Mage → 1 rune of
        // the first available element. Returns false if none in stock.
        private bool ConsumeAmmo(WeaponStyle style)
        {
            if (matInv == null) return true; // e.g. no material inventory
            if (style == WeaponStyle.Ranged)
                return matInv.ConsumeMaterial("arrows", 1);
            foreach (var id in RuneIds)
                if (matInv.GetCount(id) > 0) return matInv.ConsumeMaterial(id, 1);
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
