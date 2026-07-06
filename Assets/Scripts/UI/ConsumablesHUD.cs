using UnityEngine;
using UnityEngine.UI;
using VoidBound.Combat;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    // Minimal consumables affordance: a HUD "Eat Food" button that eats the
    // weakest cooked food (FoodConsumer.EatLowest) and applies its HoT — proving
    // the cook → eat → heal loop. A full consumables/potions panel arrives with
    // the Alchemy slice. Self-builds on the HUDCanvas it's attached to.
    public class ConsumablesHUD : MonoBehaviour
    {
        private void Start()
        {
            var go = new GameObject("EatFoodButton", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-24f, 320f);
            rt.sizeDelta = new Vector2(128f, 42f);

            go.AddComponent<Image>().color = new Color(0.5f, 0.28f, 0.16f, 0.92f);
            go.AddComponent<Button>().onClick.AddListener(Eat);
            go.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);

            var textGO = new GameObject("Label", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var trt = (RectTransform)textGO.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var label = textGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 15;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "Eat Food";
        }

        private void Eat()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            var fc = player != null ? player.GetComponent<FoodConsumer>() : null;
            if (fc == null) return;

            var food = fc.LowestFood();
            if (food == null || !fc.EatLowest())
            {
                if (player != null)
                    FloatingDamageNumber.SpawnText(player.transform.position, "No food",
                        new Color(0.8f, 0.5f, 0.3f));
                return;
            }
            FloatingDamageNumber.SpawnText(player.transform.position,
                $"Ate {food.displayName} (+{food.healOverTime} HoT)", new Color(0.5f, 0.9f, 0.4f));
        }
    }
}
