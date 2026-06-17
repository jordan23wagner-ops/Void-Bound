#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoidBound.Combat;
using VoidBound.Core;

namespace VoidBound.Editor
{
    public static class Phase1SceneSetup
    {
        [MenuItem("VoidBound/Setup Homestead Scene")]
        public static void SetupHomesteadScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            SetupLighting();
            var player = SetupPlayer();
            SetupCamera(player.transform);
            SetupGround();
            SetupMobileControls();
            SetupTestEnemy();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Homestead.unity");
            Debug.Log("[Phase 2] Homestead scene created with combat setup.");
        }

        private static void SetupLighting()
        {
            var light = GameObject.Find("Directional Light");
            if (light != null)
            {
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                var lightComp = light.GetComponent<Light>();
                if (lightComp != null)
                {
                    lightComp.intensity = 2f;
                    lightComp.useColorTemperature = true;
                    lightComp.colorTemperature = 5500f;
                }
            }
        }

        private static GameObject SetupPlayer()
        {
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/Models/PlayerPlaceholder.fbx");

            GameObject player;
            if (modelPrefab != null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
                player.name = "Player";
            }
            else
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                Debug.LogWarning("PlayerPlaceholder.fbx not found — using capsule fallback.");
            }

            player.transform.position = new Vector3(0f, 0.1f, 0f);
            player.tag = "Player";

            var cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.95f, 0f);
            cc.height = 1.9f;
            cc.radius = 0.3f;

            var pc = player.AddComponent<PlayerController>();

            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            if (inputActions != null)
            {
                var moveAction = inputActions.FindActionMap("Player")?.FindAction("Move");
                if (moveAction != null)
                {
                    var actionRef = InputActionReference.Create(moveAction);
                    var so = new SerializedObject(pc);
                    var moveField = so.FindProperty("moveAction");
                    if (moveField != null)
                    {
                        moveField.objectReferenceValue = actionRef;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
            else
            {
                Debug.LogWarning("InputSystem_Actions.inputactions not found.");
            }

            player.AddComponent<StatsComponent>();
            player.AddComponent<Health>();
            player.AddComponent<PlayerCombat>();
            AddHealthBar(player);

            ApplyPlayerMaterial(player);
            return player;
        }

        private static void ApplyPlayerMaterial(GameObject player)
        {
            var renderers = player.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.name = "PlayerMaterial";
            mat.color = new Color(0.76f, 0.60f, 0.42f, 1f);
            mat.SetFloat("_Smoothness", 0.15f);

            string matPath = "Assets/Art/Materials/PlayerMaterial.mat";
            var existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existingMat != null)
            {
                mat = existingMat;
            }
            else
            {
                AssetDatabase.CreateAsset(mat, matPath);
            }

            foreach (var r in renderers)
                r.sharedMaterial = mat;
        }

        private static void SetupCamera(Transform playerTransform)
        {
            var camObj = GameObject.Find("Main Camera");
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            var cam = camObj.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;

            camObj.transform.rotation = Quaternion.Euler(30f, 45f, 0f);
            camObj.transform.position = new Vector3(7f, 10f, -7f);

            var follow = camObj.GetComponent<IsometricCameraFollow>();
            if (follow == null)
                follow = camObj.AddComponent<IsometricCameraFollow>();

            var so = new SerializedObject(follow);
            var targetProp = so.FindProperty("target");
            if (targetProp != null)
            {
                targetProp.objectReferenceValue = playerTransform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetupGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);
            ground.tag = "Untagged";
            ground.isStatic = true;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.name = "GroundMaterial";
            mat.color = new Color(0.45f, 0.55f, 0.35f, 1f);
            mat.SetFloat("_Smoothness", 0.1f);

            string groundMatPath = "Assets/Art/Materials/GroundMaterial.mat";
            var existingGroundMat = AssetDatabase.LoadAssetAtPath<Material>(groundMatPath);
            if (existingGroundMat != null)
                mat = existingGroundMat;
            else
                AssetDatabase.CreateAsset(mat, groundMatPath);
            ground.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void SetupMobileControls()
        {
            var canvasObj = new GameObject("MobileControls");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            var bgObj = new GameObject("JoystickBackground");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(1f, 1f, 1f, 0.3f);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(0f, 0f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = new Vector2(180f, 180f);
            bgRect.sizeDelta = new Vector2(200f, 200f);

            var handleObj = new GameObject("JoystickHandle");
            handleObj.transform.SetParent(bgObj.transform, false);
            var handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 0.6f);
            var handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(80f, 80f);

            var stick = handleObj.AddComponent<OnScreenStick>();
            stick.controlPath = "<Gamepad>/leftStick";
            stick.movementRange = 60f;

            var eventSystem = GameObject.Find("EventSystem");
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
        private static void SetupTestEnemy()
        {
            var enemyModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/Models/EnemyPlaceholder.fbx");

            GameObject enemy;
            if (enemyModel != null)
            {
                enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyModel);
                enemy.name = "TestEnemy";
            }
            else
            {
                enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                enemy.name = "TestEnemy";
                Debug.LogWarning("EnemyPlaceholder.fbx not found — using cube fallback.");
            }

            enemy.transform.position = new Vector3(5f, 0.1f, 5f);

            var cc = enemy.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.7f, 0f);
            cc.height = 1.4f;
            cc.radius = 0.35f;

            enemy.AddComponent<StatsComponent>();
            enemy.AddComponent<Health>();
            enemy.AddComponent<EnemyAI>();
            AddHealthBar(enemy, new Vector3(0f, 1.6f, 0f));

            ApplyEnemyMaterial(enemy);
        }

        private static void AddHealthBar(GameObject target, Vector3 offset = default)
        {
            var hbObj = new GameObject("HealthBar");
            hbObj.transform.SetParent(target.transform, false);
            var hb = hbObj.AddComponent<HealthBar>();
            if (offset != default)
            {
                var so = new SerializedObject(hb);
                var offsetProp = so.FindProperty("offset");
                if (offsetProp != null)
                {
                    offsetProp.vector3Value = offset;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void ApplyEnemyMaterial(GameObject enemy)
        {
            var renderers = enemy.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            string matPath = "Assets/Art/Materials/EnemyMaterial.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader);
                mat.name = "EnemyMaterial";
                mat.color = new Color(0.65f, 0.25f, 0.18f, 1f);
                mat.SetFloat("_Smoothness", 0.15f);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            foreach (var r in renderers)
                r.sharedMaterial = mat;
        }
    }
}
#endif
