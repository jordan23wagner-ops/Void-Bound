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

        // (GameObject name, x, z) — hand-placed by district.
        public static readonly (string name, float x, float z)[] Buildings =
        {
            ("Merchant",              4f,  -4f),   // market by the green
            ("Storage Chest",         7f,  -1f),   // bank beside the market
            ("Forge",                -8f,  -3f),   // crafting quarter (W)
            ("Campfire",            -11f,  -6f),
            ("Garden",              -11f,   4f),   // herb garden on the west edge
            ("Warriors Guild",       -7f,   9f),   // guild row (N)
            ("Rangers Guild",        -1f,  12f),
            ("Mages Guild",           5f,  10f),
            ("Shrine",               10f,   4f),   // spiritual corner (E)
            ("Pool of Refreshment",  11f,  -5f),
            ("Fast Travel Portal",    0f, -11f),   // south gateway
            ("Watchtower",          -10f, -11f),   // SW lookout
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
