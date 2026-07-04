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
    }
}
