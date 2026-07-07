using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoidBound.Core
{
    // Lives only in Homestead.unity. First load: marks Player/Main Camera/HUDCanvas
    // DontDestroyOnLoad so gold/inventory/skills survive scene travel (Phase 7).
    // Reloading Homestead later (e.g. traveling back from Ashfields) spawns a
    // fresh duplicate set of these from the scene file — this instance destroys
    // those duplicates and keeps the original persisted ones instead.
    // Each new zone scene just needs a "PlayerSpawnPoint" object; no Player/
    // Camera/HUDCanvas/GameBootstrap of its own.
    public class GameBootstrap : MonoBehaviour
    {
        private static GameBootstrap instance;

        [SerializeField] private GameObject player;
        [SerializeField] private GameObject mainCamera;
        [SerializeField] private GameObject hudCanvas;
        [SerializeField] private GameObject eventSystem;

        private void Awake()
        {
            if (instance != null)
            {
                if (player != null) Destroy(player);
                if (mainCamera != null) Destroy(mainCamera);
                if (hudCanvas != null) Destroy(hudCanvas);
                if (eventSystem != null) Destroy(eventSystem);
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            if (player != null) DontDestroyOnLoad(player);
            if (mainCamera != null) DontDestroyOnLoad(mainCamera);
            if (hudCanvas != null) DontDestroyOnLoad(hudCanvas);
            if (eventSystem != null) DontDestroyOnLoad(eventSystem);

            // Ensure the persisted player has the death/gravestone handler and the
            // poison-status DoT holder (§4).
            if (player != null && player.GetComponent<Combat.PlayerDeath>() == null)
                player.AddComponent<Combat.PlayerDeath>();
            if (player != null && player.GetComponent<Combat.PoisonStatus>() == null)
                player.AddComponent<Combat.PoisonStatus>();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var spawn = GameObject.Find("PlayerSpawnPoint");
            if (spawn == null || player == null) return;

            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.position = spawn.transform.position;
            if (controller != null) controller.enabled = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
