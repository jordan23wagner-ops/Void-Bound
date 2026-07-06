using System.Collections.Generic;
using UnityEngine;

namespace VoidBound.Inventory
{
    // Runtime "rainbow sparkle" for Radiant gear: cycles the emission colour of
    // every emissive material on this object through the hue wheel, so the
    // diamond-bright surface catches shifting prismatic light. Added by
    // EquipmentVisuals to equipped Radiant pieces; harmless if none emit.
    public class RarityShimmer : MonoBehaviour
    {
        [SerializeField] private float speed = 0.45f;
        [SerializeField] private float intensity = 0.6f;
        [SerializeField] private float saturation = 0.5f;

        private Material[] mats;
        private float phase;

        private void Start()
        {
            var list = new List<Material>();
            foreach (var r in GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials)
                    if (m != null && m.IsKeywordEnabled("_EMISSION"))
                        list.Add(m);
            mats = list.ToArray();
            phase = Random.value; // desync pieces so the set shimmers unevenly
        }

        private void Update()
        {
            if (mats == null) return;
            float hue = Mathf.Repeat(Time.time * speed + phase, 1f);
            Color c = Color.HSVToRGB(hue, saturation, 1f) * intensity;
            foreach (var m in mats)
                if (m != null) m.SetColor("_EmissionColor", c);
        }
    }
}
