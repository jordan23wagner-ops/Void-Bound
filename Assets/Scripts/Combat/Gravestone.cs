using UnityEngine;

namespace VoidBound.Combat
{
    // The visual marker for a death drop. Built at runtime from primitives (no
    // asset import) — a stone base + headstone with a glowing gold orb so it
    // reads as lootable. Proximity to the player hands control to GraveManager,
    // which returns the stored items + currency and clears the record.
    public class Gravestone : MonoBehaviour
    {
        private const float PickupRange = 1.8f;

        private Transform player;
        private Transform orb;

        public static Gravestone Create(Vector3 position)
        {
            var root = new GameObject("Gravestone");
            root.transform.position = position;

            var stone = MakeMat(new Color(0.42f, 0.42f, 0.45f), false);
            var gold = MakeMat(new Color(1f, 0.78f, 0.2f), true);

            AddBox(root.transform, new Vector3(0f, 0.1f, 0f), new Vector3(0.7f, 0.2f, 0.45f), stone);   // base
            AddBox(root.transform, new Vector3(0f, 0.5f, -0.05f), new Vector3(0.5f, 0.7f, 0.15f), stone); // headstone
            var orbGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(orbGo.GetComponent<Collider>());
            orbGo.transform.SetParent(root.transform, false);
            orbGo.transform.localPosition = new Vector3(0f, 1.05f, -0.05f);
            orbGo.transform.localScale = Vector3.one * 0.18f;
            orbGo.GetComponent<Renderer>().material = gold;

            var g = root.AddComponent<Gravestone>();
            g.orb = orbGo.transform;
            return g;
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        private void Update()
        {
            if (orb != null) orb.Rotate(Vector3.up, 60f * Time.deltaTime);

            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p == null) return;
                player = p.transform;
            }

            if (Vector3.Distance(transform.position, player.position) <= PickupRange)
                GraveManager.Collect(player.gameObject);
        }

        private static Material MakeMat(Color c, bool emissive)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            m.color = c;
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 1.4f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return m;
        }

        private static void AddBox(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = mat;
        }
    }
}
