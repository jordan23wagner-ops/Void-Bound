using UnityEngine;

namespace VoidBound.Combat
{
    // A brief death burst: an emissive sphere that swells and dims out, then
    // self-destroys. Independent of the dying entity (which despawns), so it
    // finishes even after the corpse is gone. Spawned by HitFeedback on death.
    public class Poof : MonoBehaviour
    {
        private const float Life = 0.35f;
        private float age;
        private Material mat;
        private Color color;

        public static void Burst(Vector3 pos, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.name = "Poof";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.3f;

            var p = go.AddComponent<Poof>();
            p.color = c;
            p.mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = c };
            p.mat.EnableKeyword("_EMISSION");
            p.mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            p.mat.SetColor("_EmissionColor", c * 2.4f);
            go.GetComponent<Renderer>().sharedMaterial = p.mat;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Life);
            transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.7f, t);
            if (mat != null) mat.SetColor("_EmissionColor", color * (2.4f * (1f - t)));
            if (t >= 1f) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (mat != null) Destroy(mat);
        }
    }
}
