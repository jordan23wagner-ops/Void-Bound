using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoidBound.Combat
{
    // Drives the skeletal Animator on the child "Model" from gameplay: movement
    // scripts push Speed, combat scripts trigger Attack, and Health events fire
    // Hit/Death. Also keeps the material hit-flash from the old procedural
    // animator (a nice complement to the skeletal flinch). Replaces CombatAnimator.
    public class CharacterAnimation : MonoBehaviour
    {
        [SerializeField] private Color hitFlashColor = new(1f, 0.3f, 0.25f);
        [SerializeField] private float hitFlashDuration = 0.2f;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DeadId = Animator.StringToHash("Dead");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Animator animator;
        private Transform modelRoot;            // child that holds the Animator; the death-grounding offset target
        private SkinnedMeshRenderer bodyRenderer;
        private Health health;
        private Coroutine flashCo;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator != null) modelRoot = animator.transform;
            bodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (health != null)
            {
                health.OnDamaged += OnDamaged;
                health.OnDeath += OnDeath;
            }
        }

        // The Death clip topples the body around the hips and holds it there,
        // which leaves it floating ~0.5u above the floor (the hips pivot sits at
        // standing height). While dead, sink the visual so the body's lowest
        // point rests on the ground — self-adapting to hero/goblin height.
        private void LateUpdate()
        {
            if (animator == null || modelRoot == null || bodyRenderer == null) return;
            if (!animator.GetBool(DeadId)) return;

            float adjust = transform.position.y - bodyRenderer.bounds.min.y; // root sits at ground level
            var lp = modelRoot.localPosition;
            lp.y += adjust;
            modelRoot.localPosition = lp;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDamaged -= OnDamaged;
                health.OnDeath -= OnDeath;
            }
        }

        public void SetSpeed(float speed01)
        {
            if (animator != null) animator.SetFloat(SpeedId, speed01);
        }

        public void TriggerAttack()
        {
            if (animator != null) animator.SetTrigger(AttackId);
        }

        // Clear the Death state so a respawned player returns to Idle, and undo
        // the death-grounding offset so the standing model sits correctly.
        public void Revive()
        {
            if (animator != null) animator.SetBool(DeadId, false);
            if (modelRoot != null)
            {
                var lp = modelRoot.localPosition;
                lp.y = 0f;
                modelRoot.localPosition = lp;
            }
        }

        private void OnDamaged()
        {
            if (animator != null) animator.SetTrigger(HitId);
            if (gameObject.activeInHierarchy)
            {
                if (flashCo != null) StopCoroutine(flashCo);
                flashCo = StartCoroutine(Flash());
            }
        }

        private void OnDeath()
        {
            if (animator != null) animator.SetBool(DeadId, true);
        }

        private IEnumerator Flash()
        {
            var targets = new List<(Renderer r, Color baseCol)>();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                var baseCol = r.sharedMaterial != null ? r.sharedMaterial.color : Color.white;
                targets.Add((r, baseCol));
            }

            var mpb = new MaterialPropertyBlock();
            for (float t = 0f; t < hitFlashDuration; t += Time.deltaTime)
            {
                float k = 1f - (t / hitFlashDuration);
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
                if (r != null) r.SetPropertyBlock(null);
            flashCo = null;
        }
    }
}
