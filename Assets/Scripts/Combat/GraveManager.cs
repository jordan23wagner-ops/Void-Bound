using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidBound.Data;
using VoidBound.Inventory;

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

        public static void SetGrave(string scene, Vector3 position, List<GearItemSO> items, int gold, int shards)
        {
            var m = Instance;
            m.ClearVisual();
            m.pending = new Grave { scene = scene, position = position, items = items, gold = gold, shards = shards };
            m.TrySpawnVisual(SceneManager.GetActiveScene().name);
        }

        public static void Collect(GameObject player)
        {
            if (instance == null || instance.pending == null) return;
            var g = instance.pending;

            var inv = player.GetComponent<PlayerInventory>();
            if (inv != null && g.items != null)
                foreach (var item in g.items)
                    if (item != null) inv.AddItem(item);

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
