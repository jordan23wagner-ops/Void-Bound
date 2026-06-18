using UnityEngine;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.Combat
{
    public class WorldPickup : MonoBehaviour
    {
        private GearItemSO item;
        private float pickupRange = 1.5f;
        private Transform playerTransform;

        public static void Spawn(Vector3 position, GearItemSO gearItem)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"Pickup_{gearItem.displayName}";
            go.transform.position = position + new Vector3(
                Random.Range(-0.5f, 0.5f), 0.3f, Random.Range(-0.5f, 0.5f));
            go.transform.localScale = Vector3.one * 0.4f;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard"));
                mat.color = RarityVisualEffects.GetRarityColor(gearItem.rarity);
                renderer.material = mat;
            }

            RarityVisualEffects.ApplyToRenderers(go, gearItem.rarity);

            var pickup = go.AddComponent<WorldPickup>();
            pickup.item = gearItem;
        }

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        private void Update()
        {
            if (playerTransform == null || item == null) return;

            transform.Rotate(Vector3.up, 90f * Time.deltaTime);

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= pickupRange)
                Pickup();
        }

        private void Pickup()
        {
            var inv = playerTransform.GetComponent<PlayerInventory>();
            if (inv == null) return;

            inv.AddItem(item);
            FloatingDamageNumber.SpawnText(transform.position,
                item.displayName, RarityVisualEffects.GetRarityColor(item.rarity));
            Destroy(gameObject);
        }
    }
}
