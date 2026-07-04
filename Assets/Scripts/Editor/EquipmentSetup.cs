#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VoidBound.Editor
{
    // One-shot orchestrator for the equipment-visuals feature: generate the
    // starter gear set + wire enemy gear, then re-run the character swap (which
    // picks up the regenerated weapon-less Hero/Goblin meshes and attaches an
    // EquipmentVisuals component to the player and every enemy in both scenes).
    // Idempotent — safe to re-run.
    public static class EquipmentSetup
    {
        [MenuItem("VoidBound/Gear - Full Equipment Setup")]
        public static void Run()
        {
            StarterGearGenerator.Generate();
            CharacterModelSwap.Run();
            Debug.Log("[EquipmentSetup] Gear generated and equipment visuals wired in both scenes.");
        }
    }
}
#endif
