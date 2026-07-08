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
        // Suppress auto-firing a station the player is already standing inside on
        // arrival. Defaults true so the FIRST scene (which enters Play already
        // loaded and so never raises sceneLoaded) is treated as an arrival too —
        // otherwise a player who starts within a station's range would have its
        // panel auto-open and re-open, trapping them.
        private bool justLoaded = true;

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

                    // Already engaged with a station? Don't ping-pong to another
                    // station whose range overlaps — hold the first until we leave
                    // its range (repeat interactables like resource nodes still fire).
                    if (engaged != null && !interactable.RepeatOnProximity)
                        continue;

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
