using UnityEngine;
using UnityEngine.UI;

namespace VoidBound.UI
{
    public class MinimapWirer : MonoBehaviour
    {
        [SerializeField] private Minimap minimap;
        [SerializeField] private RawImage display;

        private void Start()
        {
            if (minimap != null && display != null && minimap.RenderTexture != null)
                display.texture = minimap.RenderTexture;
        }

        private void Update()
        {
            if (display != null && display.texture == null && minimap != null && minimap.RenderTexture != null)
            {
                display.texture = minimap.RenderTexture;
                enabled = false;
            }
        }
    }
}
