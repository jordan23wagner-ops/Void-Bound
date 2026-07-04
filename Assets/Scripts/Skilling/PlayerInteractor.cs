using UnityEngine;
using VoidBound.Core;

namespace VoidBound.Skilling
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float interactCheckRadius = 2f;
        [SerializeField] private float interactCheckInterval = 0.25f;

        private float lastCheckTime;
        private Interactable engaged; // non-repeat interactable already fired this approach

        private void Update()
        {
            if (Time.time - lastCheckTime < interactCheckInterval) return;
            lastCheckTime = Time.time;

            // Release the engaged station once the player leaves its range
            // (small hysteresis so edge jitter doesn't re-trigger panels)
            if (engaged != null)
            {
                float d = Vector3.Distance(transform.position, engaged.transform.position);
                if (d > engaged.InteractRange * 1.2f) engaged = null;
            }

            var hits = Physics.OverlapSphere(transform.position, interactCheckRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                var interactable = hit.GetComponent<Interactable>();
                if (interactable == null) continue;
                if (interactable == engaged) continue;

                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist <= interactable.InteractRange)
                {
                    interactable.OnInteract(gameObject);
                    if (!interactable.RepeatOnProximity)
                        engaged = interactable;
                    lastCheckTime = Time.time + 0.5f;
                    return;
                }
            }
        }
    }
}
