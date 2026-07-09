#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VoidBound.Editor
{
    // Places the Homestead's 12 interactive buildings into a purposeful,
    // organic starter-town layout (grouped by function) around the central
    // bonfire green — a market by the green, a crafting quarter, a guild row, a
    // spiritual corner, and a south gateway with the portal + watchtower. This
    // is the single source of truth for building placement; EnvironmentDressing
    // reads WorldPositions() for keep-outs and adjacent props. Buildings turn to
    // face the green (their doors export facing away from local +Z → +180°).
    public static class HomesteadLayout
    {
        public static readonly Vector2 Bonfire = Vector2.zero;

        // Fishing lake centre — shared by EnvironmentDressing (the water/dock) and
        // FishingContentSetup (the fishing spots) so they can never drift apart.
        public static readonly Vector2 Lake = new Vector2(21f, 16f);

        // (GameObject name, x, z) — a plaza-and-lanes village: buildings ring the
        // central bonfire green in loose districts, ~12–27 from centre and ≥10
        // apart (solver-spaced, keeping the NE clear for the lake), with cobblestone
        // lanes radiating out to each (see EnvironmentDressing.Roads).
        public static readonly (string name, float x, float z)[] Buildings =
        {
            ("Merchant",             14f,   0f),   // market row (E)
            ("Storage Chest",       7.7f, 5.5f),   // hand-placed by Jordon (off the plaza grid)
            ("Forge",               -22f,   6f),   // craft yard (W)
            ("Campfire",            -19f, -11f),
            ("Garden",              -10f, -20f),   // garden plot (S)
            ("Warriors Guild",      -16f,  16f),   // guild green (NW → N)
            ("Rangers Guild",        -6f,  22f),
            ("Mages Guild",           4f,  27f),
            ("Shrine",               13f,  20f),   // mystic quarter (NNE, by the lake)
            ("Pool of Refreshment",  23f,  -5f),   // E edge
            ("Fast Travel Portal",    2f, -23f),   // south gateway
            ("Watchtower",           23f, -15f),   // SE lookout
        };

        public static float FaceCentreYaw(Vector2 p) => Mathf.Atan2(-p.x, -p.y) * Mathf.Rad2Deg + 180f;
        public static float FaceDirYaw(Vector2 doorDir) => Mathf.Atan2(-doorDir.x, -doorDir.y) * Mathf.Rad2Deg;

        public static List<Vector2> WorldPositions() => Buildings.Select(b => new Vector2(b.x, b.z)).ToList();

        [MenuItem("VoidBound/Polish - Rearrange Homestead")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Homestead.unity");
            int moved = 0;
            foreach (var b in Buildings)
            {
                var go = GameObject.Find(b.name);
                if (go == null) { Debug.LogWarning($"[Layout] '{b.name}' not found."); continue; }
                var p = new Vector2(b.x, b.z);
                go.transform.position = new Vector3(p.x, 0f, p.y);
                go.transform.rotation = Quaternion.Euler(0f, FaceCentreYaw(p), 0f);
                moved++;
            }

            var spawn = GameObject.Find("PlayerSpawnPoint");
            if (spawn != null) spawn.transform.position = new Vector3(0f, 0.1f, -5f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Layout] Placed {moved} buildings in the starter-town layout.");
        }
    }
}
#endif
