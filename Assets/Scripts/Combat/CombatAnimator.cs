using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoidBound.Combat
{
    // Procedural combat "juice" for the static low-poly meshes (no skeleton to
    // animate): PlayAttack swings the weapon hand, PlayHit flashes the whole
    // body red. Both are transform/material-only so they never fight the
    // CharacterController-owned position/rotation or the equipment sockets.
    // Auto-plays the hit flash from Health.OnDamaged; combat scripts call
    // PlayAttack when they land a hit. A future skeletal rig (GDD Phase 9) can
    // replace this without touching the callers.
    public class CombatAnimator : MonoBehaviour
    {
        [SerializeField] private Color hitFlashColor = new(1f, 0.3f, 0.25f);
        [SerializeField] private float attackDuration = 0.24f;
        [SerializeField] private float hitDuration = 0.22f;
        [SerializeField] private float swingAngle = 100f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Transform handR;
        private Health health;
        private Coroutine attackCo, hitCo;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (health != null) health.OnDamaged += PlayHit;
        }

        private void OnDisable()
        {
            if (health != null) health.OnDamaged -= PlayHit;
        }

        private Transform HandR()
        {
            // Socket_HandR is built by EquipmentVisuals.Awake; resolve lazily
            // to avoid same-frame component-order assumptions.
            if (handR == null) handR = transform.Find("Socket_HandR");
            return handR;
        }

        public void PlayAttack()
        {
            var hand = HandR();
            if (hand == null || !gameObject.activeInHierarchy) return;
            if (attackCo != null) StopCoroutine(attackCo);
            attackCo = StartCoroutine(AttackRoutine(hand));
        }

        public void PlayHit()
        {
            if (!gameObject.activeInHierarchy) return;
            if (hitCo != null) StopCoroutine(hitCo);
            hitCo = StartCoroutine(HitRoutine());
        }

        private IEnumerator AttackRoutine(Transform hand)
        {
            Quaternion rest = Quaternion.identity;
            Quaternion swung = Quaternion.Euler(-swingAngle, 0f, 0f); // strike forward/down
            float windUp = attackDuration * 0.4f;
            float recover = attackDuration * 0.6f;

            for (float t = 0f; t < windUp; t += Time.deltaTime)
            {
                hand.localRotation = Quaternion.Slerp(rest, swung, t / windUp);
                yield return null;
            }
            for (float t = 0f; t < recover; t += Time.deltaTime)
            {
                hand.localRotation = Quaternion.Slerp(swung, rest, t / recover);
                yield return null;
            }
            hand.localRotation = rest;
            attackCo = null;
        }

        private IEnumerator HitRoutine()
        {
            // Capture the current base color per renderer (mesh renderers only;
            // world-space HealthBar uses CanvasRenderer and is skipped).
            var targets = new List<(Renderer r, Color baseCol)>();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                var baseCol = r.sharedMaterial != null ? r.sharedMaterial.color : Color.white;
                targets.Add((r, baseCol));
            }

            var mpb = new MaterialPropertyBlock();
            for (float t = 0f; t < hitDuration; t += Time.deltaTime)
            {
                float k = 1f - (t / hitDuration); // flash 1 -> 0
                foreach (var (r, baseCol) in targets)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor(BaseColorId, Color.Lerp(baseCol, hitFlashColor, k));
                    r.SetPropertyBlock(mpb);
                }
                yield return null;
            }
            foreach (var (r, _) in targets)
                if (r != null) r.SetPropertyBlock(null); // clear override
            hitCo = null;
        }
    }
}
