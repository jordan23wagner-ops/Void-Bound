using System.Collections;
using UnityEngine;

namespace VoidBound.Combat
{
    // Auto-attached to every Health entity (see Health.Start). Makes getting hit
    // readable and punchy: a floating damage number, a white impact flash, and a
    // squash-punch on the model; a burst on death. The hit amount is derived from
    // the HP-change event, so it covers ALL damage sources (melee, projectile,
    // poison) uniformly with no per-caller wiring.
    public class HitFeedback : MonoBehaviour
    {
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

        private Health health;
        private Renderer[] renderers;
        private MaterialPropertyBlock mpb;
        private Transform model;
        private Vector3 baseScale;
        private int lastHP;
        private bool isPlayer;
        private Coroutine flashCo, punchCo;

        private void Awake()
        {
            health = GetComponent<Health>();
            renderers = GetComponentsInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();
            var animT = GetComponentInChildren<Animator>();
            model = animT != null ? animT.transform : transform;
            baseScale = model.localScale;
            isPlayer = CompareTag("Player");
        }

        private void OnEnable()
        {
            if (health == null) return;
            lastHP = health.CurrentHP;
            health.OnHealthChanged += OnHealthChanged;
            health.OnDeath += OnDeath;
        }

        private void OnDisable()
        {
            if (health == null) return;
            health.OnHealthChanged -= OnHealthChanged;
            health.OnDeath -= OnDeath;
        }

        private void OnHealthChanged(int cur, int max)
        {
            if (cur < lastHP) OnHit(lastHP - cur);
            lastHP = cur;
        }

        private void OnHit(int dmg)
        {
            if (dmg <= 0) return;
            FloatingDamageNumber.SpawnText(transform.position, dmg.ToString(),
                isPlayer ? new Color(1f, 0.45f, 0.4f) : Color.white);

            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(Flash(0.09f));
            if (punchCo != null) StopCoroutine(punchCo);
            punchCo = StartCoroutine(Punch(new Vector3(1.18f, 0.85f, 1.18f), 0.14f));
        }

        private void OnDeath()
        {
            Poof.Burst(model.position + Vector3.up * 0.9f,
                isPlayer ? new Color(1f, 0.3f, 0.3f) : new Color(0.7f, 0.2f, 0.25f));
            if (flashCo != null) StopCoroutine(flashCo);
            StartCoroutine(Flash(0.16f));
        }

        private IEnumerator Flash(float dur)
        {
            SetFlash(true);
            yield return new WaitForSeconds(dur);
            SetFlash(false);
            flashCo = null;
        }

        // Override _BaseColor via a property block (never touches the shared
        // material asset); an empty block clears the override back to normal.
        private void SetFlash(bool on)
        {
            // Re-init if a domain reload cleared the non-serialized state (editor-only:
            // a mid-play recompile drops mpb/renderers and doesn't re-run Awake).
            if (mpb == null) mpb = new MaterialPropertyBlock();
            if (renderers == null) renderers = GetComponentsInChildren<Renderer>();

            mpb.Clear();
            if (on) mpb.SetColor(BaseColorID, Color.white);
            foreach (var r in renderers)
                if (r != null) r.SetPropertyBlock(mpb);
        }

        private IEnumerator Punch(Vector3 squash, float dur)
        {
            Vector3 hit = Vector3.Scale(baseScale, squash);
            model.localScale = hit;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                model.localScale = Vector3.Lerp(hit, baseScale, t / dur);
                yield return null;
            }
            model.localScale = baseScale;
            punchCo = null;
        }
    }
}
