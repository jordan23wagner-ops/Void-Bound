using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidBound.Save;

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
            if (player != null && player.GetComponent<Combat.GatherAnimator>() == null)
                player.AddComponent<Combat.GatherAnimator>();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // Load the save once at boot. Deferred one frame so it runs AFTER other
        // Start() setup (starter gear grants, auto-equip) and authoritatively
        // overrides it. Only the original instance reaches Start — duplicates are
        // destroyed in Awake.
        private void Start()
        {
            if (instance == this && SaveSystem.AutoEnabled && SaveSystem.HasSave)
                StartCoroutine(LoadAfterStartup());
        }

        private IEnumerator LoadAfterStartup()
        {
            yield return null; // let every other component's Start() run first
            SaveSystem.Load(player);
        }

        private void OnApplicationQuit()
        {
            if (instance == this && SaveSystem.AutoEnabled)
                SaveSystem.Save(player);
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
