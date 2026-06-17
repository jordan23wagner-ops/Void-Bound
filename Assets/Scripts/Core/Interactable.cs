using UnityEngine;

namespace VoidBound.Core
{
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] private string interactPrompt = "Interact";
        [SerializeField] private float interactRange = 2f;

        public string InteractPrompt => interactPrompt;
        public float InteractRange => interactRange;

        public abstract void OnInteract(GameObject instigator);
    }
}
