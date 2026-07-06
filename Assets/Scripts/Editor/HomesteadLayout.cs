#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VoidBound.Editor
{
    // Rearranges the Homestead into a village square: the 12 interactive
    // buildings sit in a deliberately-imperfect ring (varied angle + radius)
    // around a central bonfire at the origin, each turned to face inward (the
    // building models' doors point local +Z). The player spawns just south of
    // the fire. This is the single source of truth for building placement —
    // EnvironmentDressing reads WorldPositions() for the paths/keep-outs.
    public static class HomesteadLayout
    {
        public static readonly Vector2 Bonfire = Vector2.zero;

        // (GameObject name, angle°, radius) — jittered so the ring isn't perfect.
        public static readonly (string name, float ang, float r)[] Ring =
        {
            ("Merchant",            45f,  3.54f), // moved in beside the square (swapped with the nearest home)
            ("Watchtower",          52f, 12.5f),
            ("Fast Travel Portal",  88f, 12.0f),
            ("Mages Guild",        120f, 11.5f),
            ("Storage Chest",      150f,  9.0f),
            ("Warriors Guild",     180f, 11.0f),
            ("Rangers Guild",      209f, 11.5f),
            ("Forge",              236f, 10.0f),
            ("Garden",             266f,  9.5f),
            ("Campfire",           296f, 10.5f),
            ("Shrine",             324f, 11.0f),
            ("Pool of Refreshment", 350f, 12.5f),
        };

        public static Vector2 PosOf(float ang, float r) =>
            new Vector2(r * Mathf.Cos(ang * Mathf.Deg2Rad), r * Mathf.Sin(ang * Mathf.Deg2Rad));

        // Turns the building's door toward the centre. The models' fronts export
        // facing away from local +Z, so the inward heading needs a 180° flip.
        public static float FaceCentreYaw(Vector2 p) => Mathf.Atan2(-p.x, -p.y) * Mathf.Rad2Deg + 180f;

        // Turns the door toward an arbitrary world direction (same +Z-away convention).
        public static float FaceDirYaw(Vector2 doorDir) => Mathf.Atan2(-doorDir.x, -doorDir.y) * Mathf.Rad2Deg;

        public static List<Vector2> WorldPositions() => Ring.Select(b => PosOf(b.ang, b.r)).ToList();

        [MenuItem("VoidBound/Polish - Rearrange Homestead")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            int moved = 0;
            foreach (var b in Ring)
            {
                var go = GameObject.Find(b.name);
                if (go == null) { Debug.LogWarning($"[Layout] '{b.name}' not found."); continue; }
                var p = PosOf(b.ang, b.r);
                go.transform.position = new Vector3(p.x, 0f, p.y);
                go.transform.rotation = Quaternion.Euler(0f, FaceCentreYaw(p), 0f);
                moved++;
            }

            var spawn = GameObject.Find("PlayerSpawnPoint");
            if (spawn != null) spawn.transform.position = new Vector3(0f, 0.1f, -4.5f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Layout] Rearranged {moved} buildings around the bonfire.");
        }
    }
}
#endif
