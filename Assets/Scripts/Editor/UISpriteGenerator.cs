#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VoidBound.Editor
{
    // Generates the white rounded-rect 9-slice sprites the UI tints everywhere
    // (SDF-based anti-aliasing, no external art). Written to Resources/ so the
    // runtime Panel5cFactory can load them without serialized references.
    //   UI/panel_rounded  — 64px, radius 16, border 16 (panels, viewports)
    //   UI/button_rounded — 32px, radius 8,  border 8  (rows, buttons, slots)
    public static class UISpriteGenerator
    {
        private const string OutDir = "Assets/Resources/UI";

        [MenuItem("VoidBound/Polish - Generate UI Sprites")]
        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(OutDir))
                AssetDatabase.CreateFolder("Assets/Resources", "UI");

            WriteRoundedRect($"{OutDir}/panel_rounded.png", 64, 16f);
            WriteRoundedRect($"{OutDir}/button_rounded.png", 32, 8f);
            AssetDatabase.Refresh();
            ConfigureImporter($"{OutDir}/panel_rounded.png", 16);
            ConfigureImporter($"{OutDir}/button_rounded.png", 8);
            Debug.Log("[UISprites] Generated rounded 9-slice sprites.");
        }

        private static void WriteRoundedRect(string path, int size, float radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Signed distance to a rounded box centered in the texture
                    float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    float py = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    float ox = Mathf.Max(px, 0f);
                    float oy = Mathf.Max(py, 0f);
                    float dist = Mathf.Sqrt(ox * ox + oy * oy)
                                 + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                    float alpha = Mathf.Clamp01(0.5f - dist); // 1px AA feather
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureImporter(string path, int border)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
#endif
