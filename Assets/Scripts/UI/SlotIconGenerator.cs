using System.Collections.Generic;
using UnityEngine;
using VoidBound.Data;

namespace VoidBound.UI
{
    public static class SlotIconGenerator
    {
        // Shared cache so every panel reuses one sprite per slot (the line-art
        // textures are white; callers tint by rarity). Weapon uses the sword icon
        // for all weapon types; Ammo has no icon (callers fall back to a label).
        private static readonly Dictionary<EquipmentSlot, Sprite> spriteCache = new();

        public static Sprite SpriteFor(EquipmentSlot slot)
        {
            if (spriteCache.TryGetValue(slot, out var cached)) return cached;
            Texture2D tex = slot switch
            {
                EquipmentSlot.Helm   => GenerateHelm(),
                EquipmentSlot.Body   => GenerateBody(),
                EquipmentSlot.Legs   => GenerateLegs(),
                EquipmentSlot.Boots  => GenerateBoots(),
                EquipmentSlot.Gloves => GenerateGlove(),
                EquipmentSlot.Amulet => GenerateAmulet(),
                EquipmentSlot.Ring   => GenerateRing(),
                EquipmentSlot.Cape   => GenerateCape(),
                EquipmentSlot.Weapon => GenerateSword(),
                EquipmentSlot.Shield => GenerateShield(),
                _ => null,
            };
            Sprite sprite = tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f)
                : null;
            spriteCache[slot] = sprite;
            return sprite;
        }

        public static Texture2D GenerateHelm()
        {
            var tex = CreateTex();
            DrawLine(tex, 12, 28, 20, 8, 32, 8, 40, 28);
            DrawLine(tex, 12, 28, 10, 40, 16, 44, 16, 32);
            DrawLine(tex, 40, 28, 42, 40, 36, 44, 36, 32);
            DrawHLine(tex, 16, 36, 30);
            DrawLine(tex, 16, 44, 24, 48, 32, 48, 36, 44);
            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateBody()
        {
            var tex = CreateTex();
            DrawLine(tex, 10, 16, 10, 44, 42, 44, 42, 16);
            DrawLine(tex, 10, 16, 18, 12, 24, 12, 32, 12, 34, 12, 42, 16);
            DrawLine(tex, 18, 12, 24, 8, 32, 8, 34, 12);
            DrawVLine(tex, 24, 12, 44);
            DrawLine(tex, 10, 36, 6, 40);
            DrawLine(tex, 42, 36, 46, 40);
            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateLegs()
        {
            var tex = CreateTex();
            DrawRect(tex, 12, 6, 36, 16);
            DrawRect(tex, 12, 18, 22, 50);
            DrawRect(tex, 26, 18, 36, 50);
            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateBoots()
        {
            var tex = CreateTex();
            DrawRect(tex, 6, 6, 20, 30);
            DrawRect(tex, 6, 30, 20, 40);
            DrawLine(tex, 6, 40, 4, 46, 20, 46, 20, 40);
            DrawRect(tex, 28, 6, 42, 30);
            DrawRect(tex, 28, 30, 42, 40);
            DrawLine(tex, 28, 40, 28, 46, 44, 46, 42, 40);
            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateCape()
        {
            var tex = CreateTex();
            DrawLine(tex, 18, 6, 24, 10, 30, 10, 34, 6);
            DrawLine(tex, 18, 6, 10, 50, 42, 50, 34, 6);
            DrawVLine(tex, 24, 10, 50);
            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateSword()
        {
            var tex = CreateTex();
            DrawLine(tex, 14, 50, 20, 38);
            DrawLine(tex, 12, 38, 26, 38);
            DrawLine(tex, 20, 38, 38, 8);
            DrawLine(tex, 36, 12, 40, 8);
            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateShield()
        {
            var tex = CreateTex();
            DrawLine(tex, 12, 8, 12, 32, 24, 48, 36, 32, 36, 8, 12, 8);
            DrawVLine(tex, 24, 8, 48);
            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateGlove()
        {
            var tex = CreateTex();
            DrawRect(tex, 14, 24, 38, 52);
            DrawRect(tex, 14, 14, 20, 26);
            DrawRect(tex, 22, 8, 28, 26);
            DrawRect(tex, 30, 10, 36, 26);
            DrawRect(tex, 38, 16, 44, 32);
            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateAmulet()
        {
            var tex = CreateTex();
            DrawLine(tex, 16, 10, 24, 6, 32, 6, 36, 10);
            DrawLine(tex, 16, 10, 16, 24, 24, 44, 32, 44, 36, 24, 36, 10);
            DrawLine(tex, 20, 28, 24, 36, 28, 28);
            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateRing()
        {
            var tex = CreateTex();
            for (int angle = 0; angle < 360; angle += 5)
            {
                float rad = angle * Mathf.Deg2Rad;
                int x = 24 + (int)(14 * Mathf.Cos(rad));
                int y = 28 + (int)(14 * Mathf.Sin(rad));
                SetPixelSafe(tex, x, y, Color.white);
                SetPixelSafe(tex, x + 1, y, Color.white);
                SetPixelSafe(tex, x, y + 1, Color.white);
            }
            for (int angle = 0; angle < 360; angle += 8)
            {
                float rad = angle * Mathf.Deg2Rad;
                int x = 24 + (int)(4 * Mathf.Cos(rad));
                int y = 28 + (int)(4 * Mathf.Sin(rad));
                SetPixelSafe(tex, x, y, Color.white);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateTex()
        {
            var tex = new Texture2D(48, 56, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color(0, 0, 0, 0);
            for (int x = 0; x < 48; x++)
                for (int y = 0; y < 56; y++)
                    tex.SetPixel(x, y, clear);
            return tex;
        }

        private static void SetPixelSafe(Texture2D tex, int x, int y, Color c)
        {
            int fy = tex.height - 1 - y;
            if (x >= 0 && x < tex.width && fy >= 0 && fy < tex.height)
                tex.SetPixel(x, fy, c);
        }

        private static void DrawHLine(Texture2D tex, int x1, int x2, int y)
        {
            for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
            {
                SetPixelSafe(tex, x, y, Color.white);
                SetPixelSafe(tex, x, y + 1, Color.white);
            }
        }

        private static void DrawVLine(Texture2D tex, int x, int y1, int y2)
        {
            for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
            {
                SetPixelSafe(tex, x, y, Color.white);
                SetPixelSafe(tex, x + 1, y, Color.white);
            }
        }

        private static void DrawRect(Texture2D tex, int x1, int y1, int x2, int y2)
        {
            DrawHLine(tex, x1, x2, y1);
            DrawHLine(tex, x1, x2, y2);
            DrawVLine(tex, x1, y1, y2);
            DrawVLine(tex, x2, y1, y2);
        }

        private static void DrawLine(Texture2D tex, params int[] coords)
        {
            for (int i = 0; i < coords.Length - 2; i += 2)
            {
                int x0 = coords[i], y0 = coords[i + 1];
                int x1 = coords[i + 2], y1 = coords[i + 3];
                int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
                int err = dx - dy;
                while (true)
                {
                    SetPixelSafe(tex, x0, y0, Color.white);
                    SetPixelSafe(tex, x0 + 1, y0, Color.white);
                    SetPixelSafe(tex, x0, y0 + 1, Color.white);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 > -dy) { err -= dy; x0 += sx; }
                    if (e2 < dx) { err += dx; y0 += sy; }
                }
            }
        }
    }
}
