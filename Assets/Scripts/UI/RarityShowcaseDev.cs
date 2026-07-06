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
    // through all 9 rarities so the per-tier material treatment (colour +
    // reflectivity + emission + top-tier anims) can be eyeballed live.
    //
    //   [ / ]     cycle rarity down / up
    //   1 / 2 / 3 switch class  (Melee / Ranged / Mage)
    //   c         capture the current class as a 9-rarity strip PNG
    //   g         capture ALL three classes into one 3×9 master sheet PNG
    //
    // Re-styles the equipped gear's "Main" materials directly (doesn't mutate the
    // source assets). Attach to the HUDCanvas.
    public class RarityShowcaseDev : MonoBehaviour
    {
        private static string G(string id) => "Assets/ScriptableObjects/Gear/" + id + ".asset";
        private const string SwordPath = "Assets/ScriptableObjects/TestGear/Rusty_Sword_Common.asset";

        // Each class is a full Main-tinted silhouette so the rarity treatment
        // reads across the whole body. Weapon is equipped like the armour pieces
        // (EquipItem routes by slot).
        private static readonly (string name, string[] items)[] Sets =
        {
            ("Melee",  new[] { G("iron_helm"), G("iron_chestplate"), G("iron_greaves"),
                               G("iron_boots"), G("iron_gauntlets"), G("travelers_cape"),
                               G("iron_amulet"), G("wooden_shield"), SwordPath }),
            ("Ranged", new[] { G("ranger_hood"), G("ranger_vest"), G("iron_greaves"),
                               G("iron_boots"), G("iron_gauntlets"), G("travelers_cape"),
                               G("iron_amulet"), G("hunters_bow") }),
            ("Mage",   new[] { G("mage_hat"), G("mage_robe_top"), G("mage_robe_bottom"),
                               G("iron_amulet"), G("willow_staff") }),
        };

        private Transform player;
        private PlayerInventory inv;
        private TextMeshProUGUI label;
        private int idx;      // rarity index 0..8
        private int setIdx;   // class index into Sets
        private bool capturing;

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
            rt.sizeDelta = new Vector2(620f, 40f);
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
            inv = p.GetComponent<PlayerInventory>();

            yield return new WaitForSeconds(0.6f); // let DevPlaySetup's auto-equip run first, then override
            yield return EquipSet(0);
            Apply(0);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || capturing) return;
            if (kb.rightBracketKey.wasPressedThisFrame) { idx = (idx + 1) % 9; Apply(idx); }
            else if (kb.leftBracketKey.wasPressedThisFrame) { idx = (idx + 8) % 9; Apply(idx); }
            else if (kb.digit1Key.wasPressedThisFrame) StartCoroutine(SwitchSet(0));
            else if (kb.digit2Key.wasPressedThisFrame) StartCoroutine(SwitchSet(1));
            else if (kb.digit3Key.wasPressedThisFrame) StartCoroutine(SwitchSet(2));
            else if (kb.cKey.wasPressedThisFrame && player != null) StartCoroutine(CaptureSheet());
            else if (kb.gKey.wasPressedThisFrame && player != null) StartCoroutine(CaptureMasterGrid());
        }

        // Headless entry points (invoked from the editor via execute_code) so a
        // capture can be triggered without keyboard input.
        public void CaptureMasterGridNow() { if (!capturing && player != null) StartCoroutine(CaptureMasterGrid()); }
        public void CaptureSheetNow() { if (!capturing && player != null) StartCoroutine(CaptureSheet()); }
        public bool IsReady => player != null && !capturing;

        private IEnumerator SwitchSet(int s)
        {
            if (capturing || s == setIdx) yield break;
            capturing = true;
            yield return EquipSet(s);
            Apply(idx);
            capturing = false;
        }

        // Unequips everything, then equips the given class set. Leaves visuals a
        // couple of frames to rebuild before returning.
        private IEnumerator EquipSet(int s)
        {
            setIdx = s;
            if (inv != null)
            {
                foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot))) inv.UnequipItem(slot);
                foreach (var path in Sets[s].items) { var g = Load(path); if (g != null) inv.EquipItem(g); }
            }
            yield return null;   // let EquipmentVisuals build the pieces
            yield return null;
        }

        // Renders the current class at every rarity from a dedicated camera and
        // stitches a labelled 9-cell strip PNG for at-a-glance QA.
        private IEnumerator CaptureSheet()
        {
            capturing = true;
            if (label != null) label.text = "capturing " + Sets[setIdx].name + " sheet…";
            var restore = FloatPlayer();

            const int cell = 340;
            var cam = MakeCam();
            var rt = new RenderTexture(cell, cell, 16);
            cam.targetTexture = rt;
            var sheet = new Texture2D(cell * 9, cell, TextureFormat.RGB24, false);

            for (int i = 0; i < 9; i++)
            {
                Apply(i);
                yield return RenderInto(cam, rt, cell, sheet, i, 0);
            }
            sheet.Apply();
            Save(sheet, "RarityContactSheet_" + Sets[setIdx].name + ".png");

            cam.targetTexture = null;
            Destroy(cam.gameObject); rt.Release(); Destroy(rt); Destroy(sheet);
            restore();
            EndCapture();
        }

        // The showpiece QA artifact: every class × every rarity in one image.
        // Rows top→bottom: Melee, Ranged, Mage.  Columns left→right: Common→Void.
        private IEnumerator CaptureMasterGrid()
        {
            capturing = true;
            var restore = FloatPlayer();

            const int cell = 300;
            var cam = MakeCam();
            var rt = new RenderTexture(cell, cell, 16);
            cam.targetTexture = rt;
            var sheet = new Texture2D(cell * 9, cell * Sets.Length, TextureFormat.RGB24, false);

            for (int s = 0; s < Sets.Length; s++)
            {
                if (label != null) label.text = $"master sheet… {Sets[s].name} ({s + 1}/{Sets.Length})";
                yield return EquipSet(s);
                yield return null; // extra settle after re-equip
                int row = Sets.Length - 1 - s; // melee on top
                for (int i = 0; i < 9; i++)
                {
                    Apply(i);
                    yield return RenderInto(cam, rt, cell, sheet, i, row);
                }
            }
            sheet.Apply();
            Save(sheet, "RarityMasterSheet.png");
            Debug.Log("[Showcase] Master sheet saved: Assets/Screenshots/RarityMasterSheet.png " +
                      "(rows: Melee/Ranged/Mage, cols: Common→Void)");

            cam.targetTexture = null;
            Destroy(cam.gameObject); rt.Release(); Destroy(rt); Destroy(sheet);
            yield return EquipSet(setIdx = 0); // back to melee
            restore();
            EndCapture();
        }

        // Renders the framed hero into cell (col,row) of the sheet with a
        // rarity-coloured label strip along the bottom of the cell.
        private IEnumerator RenderInto(Camera cam, RenderTexture rt, int cell, Texture2D sheet, int col, int row)
        {
            var focus = player.position + Vector3.up * 1.0f;
            cam.transform.position = focus - player.forward * 3.6f + Vector3.up * 0.28f + player.right * 0.15f;
            cam.transform.LookAt(focus);
            yield return null; yield return null; yield return null; // let the anim advance
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tmp = new Texture2D(cell, cell, TextureFormat.RGB24, false);
            tmp.ReadPixels(new Rect(0, 0, cell, cell), 0, 0);
            var stripCol = RarityVisualEffects.GetRarityColor((RarityTier)col);
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < cell; x++)
                    tmp.SetPixel(x, y, stripCol);
            tmp.Apply();
            sheet.SetPixels(col * cell, row * cell, cell, cell, tmp.GetPixels());
            Destroy(tmp);
            RenderTexture.active = prev;
        }

        private Camera MakeCam()
        {
            var camGo = new GameObject("ContactCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.11f, 0.13f);
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.05f;
            return cam;
        }

        // Lifts the player high above the map (movement disabled) so captures get
        // a clean solid background instead of scene clutter. Returns a restore fn.
        private System.Action FloatPlayer()
        {
            var ctrl = player.GetComponent<VoidBound.Core.PlayerController>();
            var cc = player.GetComponent<CharacterController>();
            bool ctrlWas = ctrl != null && ctrl.enabled;
            bool ccWas = cc != null && cc.enabled;
            if (ctrl != null) ctrl.enabled = false;
            if (cc != null) cc.enabled = false;
            var pos = player.position;
            var rot = player.rotation;
            player.position = new Vector3(0f, 60f, 0f);
            player.rotation = Quaternion.Euler(0f, 200f, 0f);
            return () =>
            {
                player.position = pos;
                player.rotation = rot;
                if (cc != null) cc.enabled = ccWas;
                if (ctrl != null) ctrl.enabled = ctrlWas;
            };
        }

        private void Save(Texture2D sheet, string fileName)
        {
            Directory.CreateDirectory(Application.dataPath + "/Screenshots");
            File.WriteAllBytes(Application.dataPath + "/Screenshots/" + fileName, sheet.EncodeToPNG());
            Debug.Log("[Showcase] Saved: Assets/Screenshots/" + fileName);
        }

        private void EndCapture()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            if (label != null) SetLabel();
            capturing = false;
        }

        private void Apply(int i)
        {
            idx = i;
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
            SetLabel();
        }

        private void SetLabel()
        {
            if (label == null) return;
            var r = (RarityTier)idx;
            label.text = $"{Sets[setIdx].name}   [  ◄   {r}   ►  ]   1/2/3 class · c strip · g grid";
            label.color = RarityVisualEffects.GetRarityColor(r);
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
