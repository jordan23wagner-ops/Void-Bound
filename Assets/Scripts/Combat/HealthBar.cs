using UnityEngine;
using UnityEngine.UI;

namespace VoidBound.Combat
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.2f, 0f);

        private Health health;
        private Transform target;
        private Camera mainCamera;
        private Canvas canvas;
        private Image fillImage;

        private void Awake()
        {
            health = GetComponentInParent<Health>();
            if (health == null)
            {
                enabled = false;
                return;
            }
            target = health.transform;
        }

        private void Start()
        {
            mainCamera = Camera.main;
            CreateUI();
            health.OnHealthChanged += UpdateFill;
        }

        private void CreateUI()
        {
            var canvasObj = new GameObject("HealthBarCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            var rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120f, 15f);
            rt.localScale = Vector3.one * 0.01f;

            canvasObj.AddComponent<CanvasScaler>();

            var bgObj = new GameObject("BG");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(canvasObj.transform, false);
            fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.pivot = new Vector2(0f, 0.5f);
        }

        private void LateUpdate()
        {
            if (target == null || mainCamera == null || canvas == null) return;

            canvas.transform.position = target.position + offset;
            canvas.transform.rotation = mainCamera.transform.rotation;
        }

        private void UpdateFill(int current, int max)
        {
            if (fillImage == null) return;

            float ratio = max > 0 ? (float)current / max : 0f;
            fillImage.rectTransform.anchorMax = new Vector2(ratio, 1f);

            if (ratio > 0.5f)
                fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            else if (ratio > 0.25f)
                fillImage.color = new Color(0.9f, 0.7f, 0.1f, 1f);
            else
                fillImage.color = new Color(0.9f, 0.2f, 0.1f, 1f);
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnHealthChanged -= UpdateFill;
        }
    }
}
