using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidBound.Data;
using VoidBound.Inventory;
using VoidBound.Skilling;

namespace VoidBound.Combat
{
    // Tracks the single active death drop across scene loads. The record (which
    // zone, where, what) lives here — decoupled from any scene object — so it
    // survives the respawn scene-load; the visible Gravestone is (re)spawned
    // only while its origin zone is loaded. A new death overwrites the record,
    // so an un-recovered grave is lost when you die again.
    public class GraveManager : MonoBehaviour
    {
        private class Grave
        {
            public string scene;
            public Vector3 position;
            public List<GearItemSO> items;
            public List<MaterialInventory.Stack> materials;
            public int gold;
            public int shards;
        }

        private static GraveManager instance;
        private Grave pending;
        private Gravestone visual;

        private static GraveManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("GraveManager");
                    instance = go.AddComponent<GraveManager>();
                    DontDestroyOnLoad(go);
                    SceneManager.sceneLoaded += instance.OnSceneLoaded;
                }
                return instance;
            }
        }

        public static void SetGrave(string scene, Vector3 position, List<GearItemSO> items,
            List<MaterialInventory.Stack> materials, int gold, int shards)
        {
            var m = Instance;
            m.ClearVisual();
            m.pending = new Grave { scene = scene, position = position, items = items, materials = materials, gold = gold, shards = shards };
            // The visual is NOT spawned here: at the moment of death the corpse is
            // lying on the death spot, so a stone would appear under/over the body.
            // It's revealed at respawn (RevealGrave) or when the origin zone is
            // (re)loaded (OnSceneLoaded).
        }

        // Spawn the visual for the current scene if a grave belongs here. Called
        // once the player has respawned away, so the stone appears as the player
        // leaves rather than under the fresh corpse.
        public static void RevealGrave()
        {
            if (instance == null) return;
            instance.TrySpawnVisual(SceneManager.GetActiveScene().name);
        }

        public static void Collect(GameObject player)
        {
            if (instance == null || instance.pending == null) return;
            var g = instance.pending;

            var inv = player.GetComponent<PlayerInventory>();
            if (inv != null && g.items != null)
                foreach (var item in g.items)
                    if (item != null) inv.AddItem(item);

            var matInv = player.GetComponent<MaterialInventory>();
            if (matInv != null && g.materials != null)
                foreach (var stack in g.materials)
                    if (stack.material != null) matInv.AddMaterial(stack.material, stack.quantity);

            var currency = player.GetComponent<PlayerCurrency>();
            if (currency != null)
            {
                if (g.gold > 0) currency.AddGold(g.gold);
                if (g.shards > 0) currency.AddVoidShards(g.shards);
            }

            FloatingDamageNumber.SpawnText(player.transform.position,
                "Recovered your belongings", new Color(1f, 0.85f, 0.4f));

            instance.ClearVisual();
            instance.pending = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ClearVisual();
            TrySpawnVisual(scene.name);
        }

        private void TrySpawnVisual(string activeScene)
        {
            if (pending != null && pending.scene == activeScene && visual == null)
                visual = Gravestone.Create(pending.position);
        }

        private void ClearVisual()
        {
            if (visual != null) Destroy(visual.gameObject);
            visual = null;
        }
    }
}
