using System.Collections.Generic;
using UnityEngine;

namespace VoidBound.Inventory
{
    // Base for the animated rarity treatments (Radiant shimmer / Obsidian sheen /
    // Void). Caches every emissive material under this piece and drives their
    // emission colour each frame. Added/removed by RarityVisualEffects.ApplyAnim.
    public abstract class RarityAnimBase : MonoBehaviour
    {
        public abstract RarityAnim AnimType { get; }

        protected Material[] mats;
        protected float phase;

        protected virtual void Start() => Cache();

        private void Cache()
        {
            var list = new List<Material>();
            foreach (var r in GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials)
                    if (m != null && m.IsKeywordEnabled("_EMISSION"))
                        list.Add(m);
            mats = list.ToArray();
            phase = Random.value * 6.2832f;
        }

        protected void SetEmission(Color c)
        {
            if (mats == null) return;
            foreach (var m in mats)
                if (m != null) m.SetColor("_EmissionColor", c);
        }

        private void Update()
        {
            if (mats == null) Cache();
            Animate(Time.time);
        }

        protected abstract void Animate(float t);
    }
}
