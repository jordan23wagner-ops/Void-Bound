using UnityEngine;

namespace VoidBound.Combat
{
    public enum ProjectileKind { Arrow, Magic }

    // A homing projectile (arrow / bolt of magic) that flies to its target and
    // deals damage on arrival — so ranged/magic combat happens at a distance.
    // Built from primitives at runtime (no imported asset). Fired by PlayerCombat;
    // damage, the floating number and XP are all resolved on impact.
    public class Projectile : MonoBehaviour
    {
        private const float HitDist = 0.5f;
        private const float MaxLife = 4f;

        private Transform target;
        private Health targetHealth;
        private StatsComponent targetStats;
        private StatsComponent attacker;
        private int baseDamage;
        private float speed;
        private ProjectileKind kind;
        private System.Action<int> onHit;
        private Vector3 lastAim;
        private float age;

        public static void Spawn(Vector3 from, StatsComponent attacker, Health target,
            StatsComponent targetStats, int baseDamage, ProjectileKind kind, System.Action<int> onHit)
        {
            if (target == null || targetStats == null || attacker == null) return;

            var go = BuildVisual(kind);
            go.transform.position = from;

            var p = go.AddComponent<Projectile>();
            p.attacker = attacker;
            p.target = target.transform;
            p.targetHealth = target;
            p.targetStats = targetStats;
            p.baseDamage = baseDamage;
            p.kind = kind;
            p.onHit = onHit;
            p.speed = kind == ProjectileKind.Arrow ? 22f : 14f;
            p.lastAim = AimPoint(target.transform);
            var dir = p.lastAim - from;
            if (dir.sqrMagnitude > 0.001f) go.transform.rotation = Quaternion.LookRotation(dir);
        }

        private static Vector3 AimPoint(Transform t) => t.position + Vector3.up * 0.9f;

        private void Update()
        {
            age += Time.deltaTime;
            if (age > MaxLife) { Destroy(gameObject); return; }

            bool alive = target != null && targetHealth != null && !targetHealth.IsDead;
            Vector3 aim = alive ? AimPoint(target) : lastAim;
            if (alive) lastAim = aim;

            Vector3 to = aim - transform.position;
            float step = speed * Time.deltaTime;
            if (to.magnitude <= Mathf.Max(step, HitDist))
            {
                if (alive)
                {
                    int dmg = DamageCalculator.CalculateDamage(attacker, targetStats, baseDamage);
                    targetHealth.TakeDamage(dmg);
                    HitStop.Punch();
                    onHit?.Invoke(dmg);
                }
                Destroy(gameObject);
                return;
            }

            transform.position += to.normalized * step;
            if (kind == ProjectileKind.Arrow)
                transform.rotation = Quaternion.LookRotation(to);   // arrow points along its flight
            else
                transform.Rotate(Vector3.up, 540f * Time.deltaTime, Space.Self); // orb spins
        }

        // ── Procedural visuals (no imported model) ──
        private static GameObject BuildVisual(ProjectileKind kind)
        {
            var root = new GameObject(kind == ProjectileKind.Arrow ? "Arrow" : "MagicBolt");
            if (kind == ProjectileKind.Arrow)
            {
                Prim(PrimitiveType.Cube, root.transform, new Vector3(0, 0, 0), new Vector3(0.03f, 0.03f, 0.5f),
                    new Color(0.5f, 0.35f, 0.2f), false);                                   // shaft
                Prim(PrimitiveType.Cube, root.transform, new Vector3(0, 0, 0.3f), new Vector3(0.06f, 0.06f, 0.12f),
                    new Color(0.75f, 0.75f, 0.8f), false);                                  // head
                Prim(PrimitiveType.Cube, root.transform, new Vector3(0, 0, -0.22f), new Vector3(0.01f, 0.11f, 0.11f),
                    new Color(0.9f, 0.9f, 0.9f), false);                                    // fletching
            }
            else
            {
                Prim(PrimitiveType.Sphere, root.transform, Vector3.zero, Vector3.one * 0.24f,
                    new Color(0.55f, 0.35f, 1f), true);                                     // glowing orb
                Prim(PrimitiveType.Sphere, root.transform, Vector3.zero, Vector3.one * 0.34f,
                    new Color(0.4f, 0.2f, 0.9f, 0.4f), true);                               // faint halo
            }
            return root;
        }

        private static void Prim(PrimitiveType type, Transform parent, Vector3 lp, Vector3 ls, Color c, bool emissive)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lp;
            go.transform.localScale = ls;

            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            m.color = c;
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 1.6f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            go.GetComponent<Renderer>().material = m;
        }
    }
}
