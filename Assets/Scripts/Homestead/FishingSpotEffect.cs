using UnityEngine;

namespace VoidBound.Homestead
{
    // Runtime "fish here" indicator for a lake fishing spot: a small stack of
    // concentric ripple rings (LineRenderer circles) that expand and fade on a
    // loop across the water surface. All built on Awake (nothing stored in the
    // scene) — attach to the "Fishing Spot" GameObject. Style mirrors
    // BonfireEffect: self-contained, Update-driven, no external art assets,
    // no per-frame allocations.
    public class FishingSpotEffect : MonoBehaviour
    {
        private const int RingCount = 3;       // number of staggered ripples
        private const int RingSegments = 32;   // vertices per ripple circle
        private const float RingMinRadius = 0.15f;
        private const float RingMaxRadius = 1.6f;
        private const float RingPeriod = 2.4f; // seconds for a ripple to grow + fade
        private const float SurfaceOffsetY = 0.9f; // sit just above the lake water surface (~0.85)

        private static readonly Color RingColor = new(0.6f, 0.9f, 1f); // soft cyan/white

        private LineRenderer[] rings;
        private float[] ringPhase;
        private Vector3[] ringBuffer; // reused each frame — no per-frame alloc
        private Material ringMat;

        private void Awake()
        {
            ringBuffer = new Vector3[RingSegments + 1];
            ringMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                color = RingColor,
            };
            EnableTransparency(ringMat);

            rings = new LineRenderer[RingCount];
            ringPhase = new float[RingCount];
            for (int i = 0; i < RingCount; i++)
            {
                var go = new GameObject("Ripple");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, SurfaceOffsetY, 0f);

                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.loop = true;
                lr.positionCount = RingSegments + 1;
                lr.widthMultiplier = 0.05f;
                lr.numCornerVertices = 0;
                lr.numCapVertices = 0;
                lr.alignment = LineAlignment.TransformZ; // flat on the XZ plane
                lr.textureMode = LineTextureMode.Stretch;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.sharedMaterial = ringMat;

                // Lay the LineRenderer flat on the water surface (rotate up-axis to Y).
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                rings[i] = lr;
                ringPhase[i] = (float)i / RingCount; // stagger the ripples
            }
        }

        private void Update()
        {
            float t = Time.time;

            for (int i = 0; i < RingCount; i++)
            {
                float cyc = ((t / RingPeriod) + ringPhase[i]) % 1f;
                float radius = Mathf.Lerp(RingMinRadius, RingMaxRadius, cyc);

                var lr = rings[i];
                for (int s = 0; s <= RingSegments; s++)
                {
                    float ang = (float)s / RingSegments * Mathf.PI * 2f;
                    ringBuffer[s] = new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius, 0f);
                }
                lr.SetPositions(ringBuffer);

                // Fade alpha out as the ripple expands; also thin the line.
                float alpha = 1f - cyc;
                var c = RingColor; c.a = alpha * 0.95f;
                lr.startColor = c;
                lr.endColor = c;
                lr.widthMultiplier = Mathf.Lerp(0.12f, 0.03f, cyc);
            }
        }

        private void OnDestroy()
        {
            if (ringMat != null) Destroy(ringMat);
        }

        private static void EnableTransparency(Material m)
        {
            // Configure a URP Unlit material for alpha-blended transparency.
            m.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
            m.SetFloat("_Blend", 0f);   // 0 = Alpha
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
