using UnityEngine;

namespace VoidBound.Data
{
    [CreateAssetMenu(fileName = "New Material Item", menuName = "VoidBound/Material Item")]
    public class MaterialItemSO : ScriptableObject
    {
        public string itemId;
        public string displayName;
        public string description;
        public Sprite icon;

        [Header("Economy")]
        public int goldValue; // 0 = unsellable; merchant sell price is a ratio of this
    }
}
