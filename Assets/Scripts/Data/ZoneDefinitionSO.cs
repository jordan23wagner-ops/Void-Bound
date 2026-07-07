using UnityEngine;

namespace VoidBound.Data
{
    [CreateAssetMenu(fileName = "New ZoneDefinition", menuName = "VoidBound/Zone Definition")]
    public class ZoneDefinitionSO : ScriptableObject
    {
        public string zoneId;
        public string displayName;
        public string sceneName;
        public bool isUnlocked = true;

        [Header("Scouting (Watchtower)")]
        public int recommendedLevel = 1;   // combat level the zone is tuned for
        [Range(0, 5)] public int dangerRating = 1; // 0 = safe … 5 = deadly
        [TextArea] public string scoutReport;      // lookout's intel blurb
    }
}
