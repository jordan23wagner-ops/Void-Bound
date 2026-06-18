using UnityEngine;

namespace VoidBound.UI
{
    public class Minimap : MonoBehaviour
    {
        [SerializeField] private float height = 30f;
        [SerializeField] private float orthoSize = 15f;

        private Camera minimapCamera;
        private Transform playerTransform;

        public RenderTexture RenderTexture { get; private set; }

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;

            SetupCamera();
        }

        private void SetupCamera()
        {
            var camObj = new GameObject("MinimapCamera");
            camObj.transform.SetParent(transform);
            minimapCamera = camObj.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = orthoSize;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.15f, 0.12f, 0.1f, 1f);
            minimapCamera.cullingMask = ~0;
            minimapCamera.depth = -10;

            RenderTexture = new RenderTexture(256, 256, 16);
            minimapCamera.targetTexture = RenderTexture;

            camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (playerTransform == null || minimapCamera == null) return;
            minimapCamera.transform.position = new Vector3(
                playerTransform.position.x, height, playerTransform.position.z);
        }
    }
}
