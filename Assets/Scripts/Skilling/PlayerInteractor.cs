using UnityEngine;
using UnityEngine.SceneManagement;
using VoidBound.Core;

namespace VoidBound.Skilling
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float interactCheckRadius = 2f;
        [SerializeField] private float interactCheckInterval = 0.25f;

        private float lastCheckTime;
        private Interactable engaged; // non-repeat interactable already fired this approach
        private bool justLoaded;      // just arrived in a scene — suppress stations we spawned inside

        private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
        private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        // On arrival, run the next check immediately and mark any station we're
        // already standing inside as engaged instead of firing it — so a fast
        // travel that drops you onto the destination portal doesn't instantly
        // re-open its menu. It re-triggers normally once you leave and walk back.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            justLoaded = true;
            lastCheckTime = 0f;
        }

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
                    if (justLoaded && !interactable.RepeatOnProximity)
                    {
                        engaged = interactable; // spawned inside it — don't auto-fire on arrival
                        continue;
                    }

                    interactable.OnInteract(gameObject);
                    if (!interactable.RepeatOnProximity)
                        engaged = interactable;
                    lastCheckTime = Time.time + 0.5f;
                    justLoaded = false;
                    return;
                }
            }
            justLoaded = false;
        }
    }
}
