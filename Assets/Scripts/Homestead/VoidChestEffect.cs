using UnityEngine;

namespace VoidBound.Homestead
{
    // Mysterious void aura for the Enchanted Chest: dark violet motes that rise and
    // dissipate around it, a few slow-orbiting void wisps, and a dim under-glow.
    // All procedural, spawned on Start (nothing stored in the scene) — attach to
    // the "Enchanted Chest" GameObject. Style mirrors BonfireEffect's embers.
    public class VoidChestEffect : MonoBehaviour
    {
        private const int MoteCount = 18;
        private const int WispCount = 3;
        private static readonly Color VoidTint = new(0.34f, 0.10f, 0.55f); // deep violet

        private Transform[] motes;
        private Vector3[] moteOrigin;
        private float[] motePhase;
        private float[] moteSpeed;
        private Transform wispRing;
        private Light glow;
        private Material dark;

        private void Start()
        {
            // Near-black body with a violet emissive rim — reads as "void energy".
            dark = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.06f, 0.03f, 0.10f) };
            dark.EnableKeyword("_EMISSION");
            dark.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            dark.SetColor("_EmissionColor", VoidTint * 0.7f);

            // Dim violet under-glow.
            var lgo = new GameObject("VoidGlow");
            lgo.transform.SetParent(transform, false);
            lgo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            glow = lgo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = VoidTint;
            glow.range = 5.5f;
            glow.intensity = 1.3f;
            glow.shadows = LightShadows.None;

            // Rising void motes around the chest base.
            motes = new Transform[MoteCount];
            moteOrigin = new Vector3[MoteCount];
            motePhase = new float[MoteCount];
            moteSpeed = new float[MoteCount];
            for (int i = 0; i < MoteCount; i++)
            {
                var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var col = m.GetComponent<Collider>(); if (col != null) Destroy(col);
                m.name = "VoidMote";
                m.transform.SetParent(transform, false);
                m.GetComponent<Renderer>().sharedMaterial = dark;
                float a = Random.value * Mathf.PI * 2f;
                float r = Random.Range(0.35f, 0.75f);
                moteOrigin[i] = new Vector3(Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r);
                motePhase[i] = Random.value;
                moteSpeed[i] = Random.Range(0.18f, 0.32f);
                motes[i] = m.transform;
            }

            // Slow-orbiting void wisps at mid height.
            wispRing = new GameObject("VoidWisps").transform;
            wispRing.SetParent(transform, false);
            wispRing.localPosition = new Vector3(0f, 0.95f, 0f);
            for (int i = 0; i < WispCount; i++)
            {
                float a = i * Mathf.PI * 2f / WispCount;
                var w = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var col = w.GetComponent<Collider>(); if (col != null) Destroy(col);
                w.name = "Wisp";
                w.transform.SetParent(wispRing, false);
                w.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.7f, 0f, Mathf.Sin(a) * 0.7f);
                w.transform.localScale = Vector3.one * 0.16f;
                w.GetComponent<Renderer>().sharedMaterial = dark;
            }
        }

        private void Update()
        {
            float t = Time.time;

            for (int i = 0; i < MoteCount; i++)
            {
                float cyc = (t * moteSpeed[i] + motePhase[i]) % 1f;
                var p = moteOrigin[i] + Vector3.up * (cyc * 1.9f);
                // drift outward + gentle sway as they rise
                float outward = 1f + cyc * 0.5f;
                p.x = moteOrigin[i].x * outward + Mathf.Sin((t + motePhase[i] * 6f) * 1.3f) * 0.06f;
                p.z = moteOrigin[i].z * outward + Mathf.Cos((t + motePhase[i] * 5f) * 1.1f) * 0.06f;
                motes[i].localPosition = p;
                motes[i].localScale = Vector3.one * (0.09f * (1f - cyc)); // shrink + fade out as it rises
                motes[i].Rotate(40f * Time.deltaTime, 55f * Time.deltaTime, 0f);
            }

            if (wispRing != null) wispRing.Rotate(0f, 14f * Time.deltaTime, 0f);
            if (glow != null) glow.intensity = 1.0f + Mathf.PerlinNoise(t * 1.6f, 0.5f) * 0.9f;
        }

        private void OnDestroy()
        {
            if (dark != null) Destroy(dark);
        }
    }
}
