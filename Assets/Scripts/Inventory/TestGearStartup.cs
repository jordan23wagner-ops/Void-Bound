using UnityEngine;
using VoidBound.Data;

namespace VoidBound.Inventory
{
    public class TestGearStartup : MonoBehaviour
    {
        [SerializeField] private GearItemSO[] testItems;

        private void Start()
        {
            var inv = GetComponent<PlayerInventory>();
            if (inv == null || testItems == null) return;

            foreach (var item in testItems)
            {
                if (item != null)
                    inv.AddItem(item);
            }

            Debug.Log($"[TestGearStartup] Added {testItems.Length} test items to inventory. Press Tab to open inventory.");
        }
    }
}
