using UnityEngine;
using UnityEngine.UI;

namespace VoidBound.Skilling
{
    public class CraftingCloseWirer : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private CraftingUI craftingUI;

        private void Start()
        {
            if (closeButton != null && craftingUI != null)
                closeButton.onClick.AddListener(() => craftingUI.Close());
        }
    }
}
