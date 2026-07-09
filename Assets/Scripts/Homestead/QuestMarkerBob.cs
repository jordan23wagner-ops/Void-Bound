using UnityEngine;

namespace VoidBound.Homestead
{
    // Gentle idle animation for a quest-giver's floating marker: slow spin plus a
    // vertical bob, so the "!" reads as an interactive beacon. Self-contained; no
    // external assets. Baseline height is captured on Start.
    public class QuestMarkerBob : MonoBehaviour
    {
        [SerializeField] private float spinSpeed = 60f;   // deg/sec, yaw
        [SerializeField] private float bobHeight = 0.12f;
        [SerializeField] private float bobSpeed = 2f;

        private Vector3 baseLocalPos;
        private float phase;

        private void Start() => baseLocalPos = transform.localPosition;

        private void Update()
        {
            phase += Time.deltaTime * bobSpeed;
            transform.localPosition = baseLocalPos + Vector3.up * (Mathf.Sin(phase) * bobHeight);
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
    }
}
