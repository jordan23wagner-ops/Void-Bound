using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using VoidBound.Data;
using VoidBound.Inventory;

namespace VoidBound.UI
{
    // Dev QA tool: equips a full Main-material set on the player and cycles it
    // through all 9 rarities with the [ and ] keys, so the per-tier material
    // treatment (colour + reflectivity + emission + Radiant shimmer) can be
    // eyeballed live. Re-styles the equipped gear's "Main" materials directly
    // (doesn't mutate the source assets). Attach to the HUDCanvas.
    public class RarityShowcaseDev : MonoBehaviour
    {
        private static readonly string[] SetIds = {
            "iron_helm", "iron_chestplate", "iron_greaves", "iron_boots",
            "iron_gauntlets", "travelers_cape", "iron_amulet", "wooden_shield",
        };
        private const string GearDir = "Assets/ScriptableObjects/Gear/";
        private const string SwordPath = "Assets/ScriptableObjects/TestGear/Rusty_Sword_Common.asset";

        private Transform player;
        private TextMeshProUGUI label;
        private int idx;

        // EditorPrefs gate (default off) so the showcase stays dormant in normal
        // play; toggle via VoidBound → Dev - Rarity Showcase.
        public const string EnabledKey = "VoidBound.RarityShowcase";

        private IEnumerator Start()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorPrefs.GetBool(EnabledKey, false)) yield break;
#else
            yield break;
#endif
            var go = new GameObject("RarityShowcaseLabel", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -14f);
            rt.sizeDelta = new Vector2(560f, 40f);
            label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = 26f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.text = "loading rarity showcase…";
            go.AddComponent<UnityEngine.UI.Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);

            GameObject p = null;
            for (float t = 0f; t < 6f && p == null; t += Time.deltaTime) { p = GameObject.FindGameObjectWithTag("Player"); yield return null; }
            if (p == null) { label.text = "No player found"; yield break; }
            player = p.transform;
            var inv = p.GetComponent<PlayerInventory>();

            yield return new WaitForSeconds(0.6f); // let DevPlaySetup's auto-equip run first, then override
            if (inv != null)
            {
                foreach (EquipmentSlot s in System.Enum.GetValues(typeof(EquipmentSlot))) inv.UnequipItem(s);
                foreach (var id in SetIds) { var g = Load(GearDir + id + ".asset"); if (g != null) inv.EquipItem(g); }
                var sword = Load(SwordPath); if (sword != null) inv.EquipItem(sword);
            }
            yield return null;   // let EquipmentVisuals build the pieces
            yield return null;
            Apply(0);
        }

        private bool capturing;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.rightBracketKey.wasPressedThisFrame) { idx = (idx + 1) % 9; Apply(idx); }
            else if (kb.leftBracketKey.wasPressedThisFrame) { idx = (idx + 8) % 9; Apply(idx); }
            else if (kb.cKey.wasPressedThisFrame && !capturing && player != null) StartCoroutine(CaptureSheet());
        }

        // Renders the set at every rarity from a dedicated camera framing the hero
        // and stitches a labelled 9-cell strip PNG for at-a-glance QA.
        private IEnumerator CaptureSheet()
        {
            capturing = true;
            if (label != null) label.text = "capturing contact sheet…";

            var camGo = new GameObject("ContactCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.11f, 0.13f);
            cam.fieldOfView = 32f;
            const int cell = 340;
            var rt = new RenderTexture(cell, cell, 16);
            cam.targetTexture = rt;
            var sheet = new Texture2D(cell * 9, cell, TextureFormat.RGB24, false);

            for (int i = 0; i < 9; i++)
            {
                Apply(i);
                var focus = player.position + Vector3.up * 1.0f;
                camGo.transform.position = focus - player.forward * 3.4f + Vector3.up * 0.25f + player.right * 0.15f;
                camGo.transform.LookAt(focus);
                yield return null; yield return null; yield return null; // let the anim advance
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tmp = new Texture2D(cell, cell, TextureFormat.RGB24, false);
                tmp.ReadPixels(new Rect(0, 0, cell, cell), 0, 0);
                tmp.Apply();
                // rarity-coloured label strip along the bottom of each cell
                var col = RarityVisualEffects.GetRarityColor((RarityTier)i);
                for (int y = 0; y < 10; y++)
                    for (int x = 0; x < cell; x++)
                        tmp.SetPixel(x, y, col);
                tmp.Apply();
                sheet.SetPixels(i * cell, 0, cell, cell, tmp.GetPixels());
                Destroy(tmp);
                RenderTexture.active = prev;
            }
            sheet.Apply();
            Directory.CreateDirectory(Application.dataPath + "/Screenshots");
            File.WriteAllBytes(Application.dataPath + "/Screenshots/RarityContactSheet.png", sheet.EncodeToPNG());
            Debug.Log("[Showcase] Contact sheet saved: Assets/Screenshots/RarityContactSheet.png (Common→Void)");
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            cam.targetTexture = null;
            Destroy(camGo); rt.Release(); Destroy(rt); Destroy(sheet);
            if (label != null) label.text = $"[  ◄   {(RarityTier)idx}   ►  ]";
            capturing = false;
        }

        private void Apply(int i)
        {
            var r = (RarityTier)i;
            if (player != null)
                foreach (var rend in player.GetComponentsInChildren<Renderer>())
                {
                    if (!rend.gameObject.name.StartsWith("Gear_")) continue;
                    foreach (var m in rend.materials)
                        if (m != null && m.name.StartsWith("Main"))
                            RarityVisualEffects.StyleMainMaterial(m, r);
                    RarityVisualEffects.ApplyAnim(rend.gameObject, r);
                }
            if (label != null)
            {
                label.text = $"[  ◄   {r}   ►  ]";
                label.color = RarityVisualEffects.GetRarityColor(r);
            }
        }

        private static GearItemSO Load(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GearItemSO>(path);
#else
            return null;
#endif
        }
    }
}
