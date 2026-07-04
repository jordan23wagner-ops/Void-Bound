using UnityEngine;

namespace VoidBound.Core
{
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] private string interactPrompt = "Interact";
        [SerializeField] private float interactRange = 2f;

        public string InteractPrompt => interactPrompt;
        public float InteractRange => interactRange;

        // When false, the proximity interactor fires once per approach and
        // won't re-fire until the player leaves range (panel-opening stations).
        // Default true preserves Phase 5 behavior (resource nodes, crafting).
        public virtual bool RepeatOnProximity => true;

        public abstract void OnInteract(GameObject instigator);
    }
}
