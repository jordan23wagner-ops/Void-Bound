using UnityEngine;

namespace VoidBound.Homestead
{
    // Runtime "life" for the scrying Pool of Refreshment: a floating violet orb
    // that bobs and spins, a slow ring of crystal shards orbiting above the void
    // surface, and a breathing emissive pulse. Spawns its own primitives on
    // Start (nothing is stored in the scene) — attach to the Pool GameObject.
    public class PoolMysticEffect : MonoBehaviour
    {
        [SerializeField] private float orbHeight = 1.15f;
        [SerializeField] private int shardCount = 6;

        private Transform orb;
        private Transform ring;
        private Material glow;
        private static readonly Color Violet = new(0.62f, 0.28f, 0.95f);

        private void Start()
        {
            glow = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Violet };
            glow.EnableKeyword("_EMISSION");
            glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            glow.SetColor("_EmissionColor", Violet * 1.6f);

            orb = MakePrimitive(PrimitiveType.Sphere, "MysticOrb", Vector3.one * 0.34f);
            orb.SetParent(transform, false);
            orb.localPosition = new Vector3(0f, orbHeight, 0f);

            ring = new GameObject("ShardRing").transform;
            ring.SetParent(transform, false);
            ring.localPosition = new Vector3(0f, 0.95f, 0f);
            for (int i = 0; i < shardCount; i++)
            {
                float a = i * Mathf.PI * 2f / shardCount;
                var s = MakePrimitive(PrimitiveType.Cube, "Shard", new Vector3(0.06f, 0.3f, 0.06f));
                s.SetParent(ring, false);
                s.localPosition = new Vector3(Mathf.Cos(a) * 0.62f, 0f, Mathf.Sin(a) * 0.62f);
                s.localRotation = Quaternion.Euler(18f, -a * Mathf.Rad2Deg, 0f);
            }
        }

        private void Update()
        {
            float t = Time.time;
            if (ring != null) ring.Rotate(0f, 22f * Time.deltaTime, 0f);
            if (orb != null)
            {
                orb.localPosition = new Vector3(0f, orbHeight + Mathf.Sin(t * 1.4f) * 0.09f, 0f);
                orb.Rotate(0f, 45f * Time.deltaTime, 0f);
            }
            if (glow != null)
                glow.SetColor("_EmissionColor", Violet * (1.2f + Mathf.Sin(t * 2.2f) * 0.55f));
        }

        private void OnDestroy()
        {
            if (glow != null) Destroy(glow);
        }

        private Transform MakePrimitive(PrimitiveType type, string name, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.name = name;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = glow;
            return go.transform;
        }
    }
}
