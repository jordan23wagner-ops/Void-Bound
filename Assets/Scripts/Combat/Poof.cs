using UnityEngine;

namespace VoidBound.Combat
{
    // Brief procedural effect that swells from a start scale to an end scale while
    // its emissive glow dims out, then self-destroys. Independent of any entity so
    // it finishes even after a corpse despawns. Two flavours:
    //   Burst — a sphere pop (enemy/player death)
    //   Ring  — a flat expanding shockwave disc (the player's AoE ability)
    public class Poof : MonoBehaviour
    {
        private float life;
        private float age;
        private float baseEmission;
        private Vector3 startScale, endScale;
        private Material mat;
        private Color color;

        public static void Burst(Vector3 pos, Color c)
        {
            var p = Make(PrimitiveType.Sphere, pos, c, 0.35f, 2.4f);
            p.startScale = Vector3.one * 0.3f;
            p.endScale = Vector3.one * 1.7f;
            p.transform.localScale = p.startScale;
        }

        public static void Ring(Vector3 pos, Color c, float radius)
        {
            var p = Make(PrimitiveType.Cylinder, pos + Vector3.up * 0.06f, c, 0.4f, 2.2f);
            p.startScale = new Vector3(0.6f, 0.03f, 0.6f);
            p.endScale = new Vector3(radius * 2f, 0.03f, radius * 2f); // cylinder diameter = 1 unit
            p.transform.localScale = p.startScale;
        }

        private static Poof Make(PrimitiveType type, Vector3 pos, Color c, float life, float emission)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.name = "Poof";
            go.transform.position = pos;

            var p = go.AddComponent<Poof>();
            p.life = life;
            p.baseEmission = emission;
            p.color = c;
            p.mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = c };
            p.mat.EnableKeyword("_EMISSION");
            p.mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            p.mat.SetColor("_EmissionColor", c * emission);
            go.GetComponent<Renderer>().sharedMaterial = p.mat;
            return p;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / life);
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            if (mat != null) mat.SetColor("_EmissionColor", color * (baseEmission * (1f - t)));
            if (t >= 1f) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (mat != null) Destroy(mat);
        }
    }
}
