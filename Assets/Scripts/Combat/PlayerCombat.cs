using UnityEngine;
using UnityEngine.InputSystem;
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

        [Header("Shockwave ability")]
        [SerializeField] private float shockRadius = 4.5f;
        [SerializeField] private float shockCooldown = 5f;
        [SerializeField] private float shockDamageMult = 2.2f;
        private float shockCdTimer;

        private StatsComponent stats;
        private PlayerInventory inventory;
        private PlayerSkills skills;
        private CharacterAnimation anim;
        private MaterialInventory matInv;
        private PlayerUpgrades upgrades;
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
            upgrades = GetComponent<PlayerUpgrades>();
        }

        private void Update()
        {
            // Active ability (Q now; a touch button can call TryShockwave on mobile).
            shockCdTimer -= Time.deltaTime;
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
                TryShockwave();

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
                HitStop.Punch();
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

        // Nova burst: damage every enemy within shockRadius for bonus damage, with
        // a ring VFX + hit-stop, on a cooldown. Public so a mobile button can fire
        // it too. Weapon-agnostic (works for melee/ranged/mage).
        public bool TryShockwave()
        {
            if (shockCdTimer > 0f) return false;
            shockCdTimer = shockCooldown;

            var hits = Physics.OverlapSphere(transform.position, shockRadius, enemyLayer);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                var h = hit.GetComponent<Health>();
                var st = hit.GetComponent<StatsComponent>();
                if (h == null || h.IsDead || st == null) continue;
                int dmg = DamageCalculator.CalculateDamage(stats, st, Mathf.RoundToInt(baseDamage * shockDamageMult));
                h.TakeDamage(dmg);
                CombatXPCalculator.AwardCombatXP(skills, GetEquippedWeaponType(), dmg);
            }

            Poof.Ring(transform.position, new Color(0.5f, 0.8f, 1f), shockRadius);
            HitStop.Punch(0.08f, 0.1f);
            anim?.TriggerAttack();
            return true;
        }

        private WeaponType GetEquippedWeaponType()
        {
            if (inventory == null) return WeaponType.Sword;
            var weapon = inventory.GetEquipped(EquipmentSlot.Weapon);
            return weapon != null ? weapon.weaponType : WeaponType.Sword;
        }

        // Spends ammo for a ranged/magic fire: 1 Arrow (ranged) or 1 rune of the
        // first available element (mage). An equipped saver offhand (Quiver /
        // Mage's Book) has a tier-scaled chance to fire without consuming.
        // Returns false only if there's no ammo in stock.
        private bool ConsumeAmmo(WeaponStyle style)
        {
            if (matInv == null) return true; // e.g. no material inventory

            string ammoId = null;
            if (style == WeaponStyle.Ranged)
            {
                if (matInv.GetCount("arrows") > 0) ammoId = "arrows";
            }
            else
            {
                foreach (var id in RuneIds)
                    if (matInv.GetCount(id) > 0) { ammoId = id; break; }
            }
            if (ammoId == null) return false; // nothing to fire with

            var offhand = inventory != null ? inventory.GetEquipped(EquipmentSlot.Shield) : null;
            if (offhand != null && offhand.ammoSaver)
            {
                var tier = upgrades != null ? upgrades.GetTier(offhand) : offhand.rarity;
                if (Random.value < 0.10f + (int)tier * 0.08f) return true; // saved — no consumption
            }
            return matInv.ConsumeMaterial(ammoId, 1);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
