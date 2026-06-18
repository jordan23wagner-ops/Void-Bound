using UnityEngine;

namespace VoidBound.Combat
{
    public class FloatingDamageNumber : MonoBehaviour
    {
        private float duration = 1f;
        private float riseSpeed = 1.5f;
        private float elapsed;
        private TextMesh textMesh;
        private Color startColor;

        public static void SpawnText(Vector3 worldPos, string text, Color color)
        {
            var go = new GameObject("FloatText");
            go.transform.position = worldPos + new Vector3(Random.Range(-0.3f, 0.3f), 1.8f, 0f);

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 32;
            tm.characterSize = 0.08f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;

            var fdn = go.AddComponent<FloatingDamageNumber>();
            fdn.textMesh = tm;
            fdn.startColor = color;
        }

        public static void Spawn(Vector3 worldPos, int damage, bool isCrit)
        {
            var go = new GameObject("DmgNum");
            go.transform.position = worldPos + new Vector3(Random.Range(-0.3f, 0.3f), 1.8f, 0f);

            var tm = go.AddComponent<TextMesh>();
            tm.text = isCrit ? $"{damage}!" : damage.ToString();
            tm.fontSize = isCrit ? 48 : 36;
            tm.characterSize = 0.08f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = isCrit ? new Color(1f, 0.85f, 0.1f) : Color.white;
            tm.fontStyle = isCrit ? FontStyle.Bold : FontStyle.Normal;

            var fdn = go.AddComponent<FloatingDamageNumber>();
            fdn.textMesh = tm;
            fdn.startColor = tm.color;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position += Vector3.up * riseSpeed * Time.deltaTime;

            if (Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;

            if (textMesh != null)
            {
                Color c = startColor;
                c.a = 1f - t;
                textMesh.color = c;
            }

            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}
