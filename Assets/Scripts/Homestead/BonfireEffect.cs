using UnityEngine;

namespace VoidBound.Homestead
{
    // Runtime life for the town bonfire: a flickering fire light, a couple of
    // pulsing flame overlays, and a stream of rising embers. All spawned on
    // Start (nothing stored in the scene) — attach to the Bonfire GameObject.
    public class BonfireEffect : MonoBehaviour
    {
        private const int EmberCount = 14;
        private static readonly Color FireColor = new(1f, 0.55f, 0.18f);

        private Light fireLight;
        private Transform[] flames;
        private Vector3[] flameBase;
        private Transform[] embers;
        private Vector3[] emberOrigin;
        private float[] emberPhase;
        private Material glow;

        private void Start()
        {
            glow = EmissiveMat(FireColor, 1.9f);

            var lgo = new GameObject("FireLight");
            lgo.transform.SetParent(transform, false);
            lgo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            fireLight = lgo.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.6f, 0.26f);
            fireLight.range = 12f;
            fireLight.intensity = 3f;
            fireLight.shadows = LightShadows.None;

            flames = new Transform[2];
            flameBase = new Vector3[2];
            flames[0] = MakeFlame(new Vector3(0f, 1.5f, 0f), new Vector3(0.75f, 1.7f, 0.75f), 0);
            flames[1] = MakeFlame(new Vector3(0.18f, 1.15f, 0.1f), new Vector3(0.5f, 1.2f, 0.5f), 1);

            embers = new Transform[EmberCount];
            emberOrigin = new Vector3[EmberCount];
            emberPhase = new float[EmberCount];
            for (int i = 0; i < EmberCount; i++)
            {
                var e = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var col = e.GetComponent<Collider>(); if (col != null) Destroy(col);
                e.name = "Ember";
                e.transform.SetParent(transform, false);
                e.GetComponent<Renderer>().sharedMaterial = glow;
                emberOrigin[i] = new Vector3(Random.Range(-0.45f, 0.45f), 0.7f, Random.Range(-0.45f, 0.45f));
                emberPhase[i] = Random.value;
                embers[i] = e.transform;
            }
        }

        private void Update()
        {
            float t = Time.time;
            fireLight.intensity = 2.6f + Mathf.PerlinNoise(t * 9f, 0.3f) * 1.7f;

            for (int i = 0; i < flames.Length; i++)
            {
                float f = 0.82f + Mathf.PerlinNoise(t * 6f + i * 11f, 0f) * 0.5f;
                var s = flameBase[i];
                flames[i].localScale = new Vector3(s.x * (1.25f - f * 0.25f), s.y * f, s.z * (1.25f - f * 0.25f));
            }

            for (int i = 0; i < EmberCount; i++)
            {
                float cyc = (t * 0.4f + emberPhase[i]) % 1f;
                var pos = emberOrigin[i] + Vector3.up * (cyc * 2.8f);
                pos.x += Mathf.Sin((t + emberPhase[i] * 6f) * 2f) * 0.18f;
                embers[i].localPosition = pos;
                embers[i].localScale = Vector3.one * (0.09f * (1f - cyc));
            }
        }

        private void OnDestroy()
        {
            if (glow != null) Destroy(glow);
        }

        private Transform MakeFlame(Vector3 pos, Vector3 scale, int index)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.name = "Flame";
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = glow;
            flameBase[index] = scale;
            return go.transform;
        }

        private static Material EmissiveMat(Color c, float e)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = c };
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", c * e);
            return m;
        }
    }
}
