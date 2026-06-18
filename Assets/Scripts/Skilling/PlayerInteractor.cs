using UnityEngine;
using VoidBound.Core;

namespace VoidBound.Skilling
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float interactCheckRadius = 2f;
        [SerializeField] private float interactCheckInterval = 0.25f;

        private float lastCheckTime;

        private void Update()
        {
            if (Time.time - lastCheckTime < interactCheckInterval) return;
            lastCheckTime = Time.time;

            var hits = Physics.OverlapSphere(transform.position, interactCheckRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                var interactable = hit.GetComponent<Interactable>();
                if (interactable == null) continue;

                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist <= interactable.InteractRange)
                {
                    interactable.OnInteract(gameObject);
                    lastCheckTime = Time.time + 0.5f;
                    return;
                }
            }
        }
    }
}
