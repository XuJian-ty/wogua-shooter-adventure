using System.Collections.Generic;
using UnityEngine;

namespace WoguaShooter.Core
{
    public static class GameplaySpriteLibrary
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string resourceName)
        {
            if (Cache.TryGetValue(resourceName, out Sprite cached))
                return cached;

            Sprite sprite = Resources.Load<Sprite>($"Gameplay/{resourceName}");
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>($"Gameplay/{resourceName}");
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        Mathf.Max(texture.width, texture.height));
                    sprite.name = resourceName;
                }
            }

            if (sprite == null)
                sprite = RuntimeEffectSprites.GetFallback(resourceName);

            Cache[resourceName] = sprite;
            return sprite;
        }
    }

    public static class RuntimeEffectSprites
    {
        private const int Size = 96;
        private static Sprite shieldRing;
        private static Sprite shieldGlow;
        private static Sprite shieldGlass;
        private static Sprite shieldHighlight;
        private static Sprite bubble;
        private static Sprite flame;
        private static Sprite flameCore;
        private static Sprite ember;
        private static Sprite smoke;
        private static Sprite fireBase;
        private static readonly Dictionary<string, Sprite> Fallbacks = new Dictionary<string, Sprite>();

        public static Sprite ShieldRing => shieldRing != null ? shieldRing : shieldRing = CreateShield(false);
        public static Sprite ShieldGlow => shieldGlow != null ? shieldGlow : shieldGlow = CreateShield(true);
        public static Sprite ShieldGlass => shieldGlass != null ? shieldGlass : shieldGlass = CreateShieldGlass();
        public static Sprite ShieldHighlight => shieldHighlight != null ? shieldHighlight : shieldHighlight = CreateShieldHighlight();
        public static Sprite Bubble => bubble != null ? bubble : bubble = CreateBubble();
        public static Sprite Flame => flame != null ? flame : flame = CreateFlame();
        public static Sprite FlameCore => flameCore != null ? flameCore : flameCore = CreateFlameCore();
        public static Sprite Ember => ember != null ? ember : ember = CreateEmber();
        public static Sprite Smoke => smoke != null ? smoke : smoke = CreateSmoke();
        public static Sprite FireBase => fireBase != null ? fireBase : fireBase = CreateFireBase();

        public static Sprite GetFallback(string resourceName)
        {
            if (Fallbacks.TryGetValue(resourceName, out Sprite sprite))
                return sprite;

            Texture2D texture = CreateTexture(resourceName);
            switch (resourceName)
            {
                case "coin":
                    DrawCircle(texture, 48, 48, 28, new Color32(117, 70, 14, 255));
                    DrawCircle(texture, 48, 48, 23, new Color32(255, 194, 45, 255));
                    DrawCircle(texture, 48, 48, 15, new Color32(255, 226, 94, 255));
                    DrawLine(texture, 48, 33, 48, 63, new Color32(196, 117, 24, 255), 5);
                    break;
                case "spikes":
                    for (int i = 0; i < 4; i++)
                        DrawTriangle(texture, 8 + i * 21, 18, 20 + i * 21, 78, 31 + i * 21, 18,
                            new Color32(102, 117, 124, 255));
                    break;
                case "missile":
                    DrawEllipse(texture, 50, 48, 34, 14, new Color32(190, 204, 209, 255));
                    DrawTriangle(texture, 12, 48, 28, 35, 28, 61, new Color32(231, 73, 50, 255));
                    DrawTriangle(texture, 67, 37, 83, 25, 76, 45, new Color32(63, 92, 111, 255));
                    DrawCircle(texture, 36, 48, 5, new Color32(52, 91, 116, 255));
                    break;
                case "icon_magnet":
                    DrawMagnet(texture);
                    break;
                case "icon_shield":
                    DrawShieldIcon(texture);
                    break;
                case "icon_growth":
                    DrawGrowthIcon(texture);
                    break;
                default:
                    DrawCircle(texture, 48, 48, 24, new Color32(238, 180, 71, 255));
                    break;
            }

            texture.Apply();
            sprite = CreateSprite(texture, resourceName);
            Fallbacks[resourceName] = sprite;
            return sprite;
        }

        private static Sprite CreateShield(bool glow)
        {
            Texture2D texture = CreateTexture(glow ? "ShieldGlow" : "ShieldRing", 128);
            Vector2 center = new Vector2(63.5f, 63.5f);
            Color32[] pixels = new Color32[128 * 128];
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / 63.5f;
                    float nx = (x - center.x) / 63.5f;
                    float ny = (y - center.y) / 63.5f;
                    float alpha;
                    Color color;
                    if (glow)
                    {
                        alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 1.7f) * 0.5f;
                        color = new Color(0.22f, 0.78f, 1f, alpha);
                    }
                    else
                    {
                        float outerRim = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.88f) / 0.055f);
                        float innerRim = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.79f) / 0.035f);
                        float light = Mathf.Clamp01(0.52f + (-nx + ny) * 0.28f);
                        alpha = Mathf.Max(outerRim * Mathf.Lerp(0.68f, 1f, light), innerRim * 0.35f);
                        color = Color.Lerp(
                            new Color(0.04f, 0.46f, 0.9f, alpha),
                            new Color(0.84f, 1f, 1f, alpha),
                            light);
                    }
                    pixels[y * 128 + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return CreateSprite(texture, texture.name);
        }

        private static Sprite CreateShieldGlass()
        {
            Texture2D texture = CreateTexture("ShieldGlass", 128);
            Vector2 center = new Vector2(63.5f, 63.5f);
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float nx = (x - center.x) / 63.5f;
                    float ny = (y - center.y) / 63.5f;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    if (distance > 0.82f)
                        continue;

                    float edgeDepth = Mathf.Pow(Mathf.Clamp01(distance / 0.82f), 2.4f);
                    float light = Mathf.Clamp01(0.56f + (-nx + ny) * 0.18f);
                    float alpha = Mathf.Lerp(0.045f, 0.17f, edgeDepth);
                    Color color = Color.Lerp(
                        new Color(0.08f, 0.48f, 0.92f, alpha),
                        new Color(0.72f, 0.98f, 1f, alpha),
                        light);
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return CreateSprite(texture, texture.name);
        }

        private static Sprite CreateShieldHighlight()
        {
            Texture2D texture = CreateTexture("ShieldHighlight", 128);
            Vector2 center = new Vector2(63.5f, 63.5f);
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float nx = (x - center.x) / 63.5f;
                    float ny = (y - center.y) / 63.5f;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float upperLeft = Mathf.Clamp01((-nx + ny - 0.15f) * 1.25f);
                    float arc = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.62f) / 0.075f) * upperLeft;
                    float spot = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(nx, ny), new Vector2(-0.28f, 0.34f)) / 0.17f);
                    float alpha = Mathf.Max(arc * 0.5f, spot * spot * 0.34f);
                    texture.SetPixel(x, y, new Color(0.9f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return CreateSprite(texture, texture.name);
        }

        private static Sprite CreateBubble()
        {
            Texture2D texture = CreateTexture("ShieldBubble", 48);
            Vector2 center = new Vector2(23.5f, 23.5f);
            for (int y = 0; y < 48; y++)
            {
                for (int x = 0; x < 48; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / 23.5f;
                    float rim = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.72f) / 0.16f);
                    float shine = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(17f, 31f)) / 6f);
                    float alpha = Mathf.Max(rim * 0.72f, shine * 0.9f);
                    texture.SetPixel(x, y, new Color(0.8f, 0.98f, 1f, alpha));
                }
            }
            texture.Apply();
            return CreateSprite(texture, texture.name);
        }

        private static Sprite CreateFlame()
        {
            Texture2D texture = CreateTexture("FireFlame");
            for (int y = 6; y < 91; y++)
            {
                float normalizedY = y / 95f;
                float halfWidth = Mathf.Sin(normalizedY * Mathf.PI) * 24f * (1.15f - normalizedY * 0.45f);
                float centerX = 48f + Mathf.Sin(normalizedY * 7f) * 3f;
                for (int x = 0; x < 96; x++)
                {
                    float distance = Mathf.Abs(x - centerX);
                    if (distance > halfWidth)
                        continue;
                    float edge = Mathf.Clamp01((halfWidth - distance) / 5f);
                    Color color = Color.Lerp(new Color(1f, 0.86f, 0.22f), new Color(1f, 0.16f, 0.03f), normalizedY);
                    color.a = edge;
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return CreateSprite(texture, texture.name);
        }

        private static Sprite CreateFlameCore()
        {
            Texture2D texture = CreateTexture("FireFlameCore");
            for (int y = 5; y < 83; y++)
            {
                float normalizedY = y / 82f;
                float halfWidth = Mathf.Sin(normalizedY * Mathf.PI) * 15f * (1.08f - normalizedY * 0.5f);
                float centerX = 48f + Mathf.Sin(normalizedY * 8f + 0.6f) * 2f;
                for (int x = 0; x < 96; x++)
                {
                    float distance = Mathf.Abs(x - centerX);
                    if (distance > halfWidth)
                        continue;
                    float edge = Mathf.Clamp01((halfWidth - distance) / 3f);
                    Color color = Color.Lerp(new Color(0.95f, 1f, 0.82f), new Color(1f, 0.67f, 0.08f), normalizedY);
                    color.a = edge * (1f - normalizedY * 0.18f);
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return CreateSprite(texture, texture.name);
        }

        private static Sprite CreateEmber()
        {
            Texture2D texture = CreateTexture("FireEmber", 32);
            Vector2 center = new Vector2(15.5f, 15.5f);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / 15.5f;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 0.65f);
                    texture.SetPixel(x, y, new Color(1f, Mathf.Lerp(0.2f, 0.95f, alpha), 0.03f, alpha));
                }
            }
            texture.Apply();
            return CreateSprite(texture, texture.name);
        }

        private static Sprite CreateSmoke()
        {
            Texture2D texture = CreateTexture("FireSmoke", 64);
            Vector2 center = new Vector2(31.5f, 31.5f);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / 31.5f;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 1.8f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return CreateSprite(texture, texture.name);
        }

        private static Sprite CreateFireBase()
        {
            Texture2D texture = CreateTexture("FireBase");

            Color32 ironOutline = new Color32(31, 35, 38, 255);
            Color32 ironDark = new Color32(55, 61, 64, 255);
            Color32 ironLight = new Color32(104, 112, 113, 255);
            Color32 fireBed = new Color32(82, 27, 19, 255);
            Color32 woodOutline = new Color32(51, 29, 20, 255);
            Color32 wood = new Color32(133, 70, 35, 255);
            Color32 woodLight = new Color32(190, 105, 47, 255);

            DrawEllipse(texture, 48, 12, 35, 6, new Color32(25, 28, 29, 105));
            DrawLine(texture, 25, 23, 20, 9, ironOutline, 8);
            DrawLine(texture, 71, 23, 76, 9, ironOutline, 8);
            DrawLine(texture, 25, 23, 20, 9, ironLight, 3);
            DrawLine(texture, 71, 23, 76, 9, ironLight, 3);

            DrawEllipse(texture, 48, 28, 43, 15, ironOutline);
            DrawEllipse(texture, 48, 32, 39, 12, ironDark);
            DrawEllipse(texture, 48, 36, 34, 8, fireBed);

            DrawLine(texture, 24, 39, 71, 25, woodOutline, 12);
            DrawLine(texture, 24, 39, 71, 25, wood, 7);
            DrawLine(texture, 26, 38, 69, 26, woodLight, 2);
            DrawLine(texture, 25, 25, 71, 40, woodOutline, 12);
            DrawLine(texture, 25, 25, 71, 40, wood, 7);
            DrawLine(texture, 27, 26, 69, 39, woodLight, 2);

            DrawEllipse(texture, 48, 23, 41, 8, ironOutline);
            DrawEllipse(texture, 48, 25, 36, 5, ironLight);
            DrawEllipse(texture, 48, 27, 33, 3, ironDark);
            texture.Apply();
            return CreateSprite(texture, texture.name, new Rect(3f, 5f, 90f, 47f), new Vector2(0.5f, 0f), 96f);
        }

        private static void DrawMagnet(Texture2D texture)
        {
            Color32 outline = new Color32(55, 54, 65, 255);
            DrawCircle(texture, 48, 49, 31, outline);
            DrawCircle(texture, 48, 49, 23, new Color32(228, 68, 76, 255));
            DrawCircle(texture, 48, 49, 13, new Color32(0, 0, 0, 0));
            for (int y = 49; y < 88; y++)
                for (int x = 0; x < 96; x++)
                    texture.SetPixel(x, y, new Color32(0, 0, 0, 0));
            DrawRectangle(texture, 16, 45, 34, 67, new Color32(224, 231, 237, 255));
            DrawRectangle(texture, 62, 45, 80, 67, new Color32(224, 231, 237, 255));
        }

        private static void DrawShieldIcon(Texture2D texture)
        {
            DrawCircle(texture, 48, 48, 34, new Color32(44, 103, 151, 255));
            DrawCircle(texture, 48, 48, 29, new Color32(111, 212, 243, 255));
            DrawTriangle(texture, 48, 18, 75, 34, 48, 82, new Color32(201, 247, 255, 255));
            DrawTriangle(texture, 48, 18, 21, 34, 48, 82, new Color32(134, 223, 248, 255));
        }

        private static void DrawGrowthIcon(Texture2D texture)
        {
            DrawCircle(texture, 48, 48, 26, new Color32(106, 180, 70, 255));
            DrawLine(texture, 48, 72, 48, 22, new Color32(238, 255, 209, 255), 8);
            DrawLine(texture, 48, 72, 30, 54, new Color32(238, 255, 209, 255), 8);
            DrawLine(texture, 48, 72, 66, 54, new Color32(238, 255, 209, 255), 8);
            DrawLine(texture, 25, 26, 71, 26, new Color32(54, 101, 43, 255), 5);
        }

        private static Texture2D CreateTexture(string name, int size = Size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[size * size];
            texture.SetPixels32(pixels);
            return texture;
        }

        private static Sprite CreateSprite(Texture2D texture, string name)
        {
            return CreateSprite(texture, name, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), Mathf.Max(texture.width, texture.height));
        }

        private static Sprite CreateSprite(Texture2D texture, string name, Rect rect, Vector2 pivot, float pixelsPerUnit)
        {
            Sprite sprite = Sprite.Create(texture, rect, pivot, pixelsPerUnit);
            sprite.name = name;
            return sprite;
        }

        private static void DrawCircle(Texture2D texture, int cx, int cy, int radius, Color32 color)
        {
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    if (x * x + y * y <= radius * radius)
                        SetPixel(texture, cx + x, cy + y, color);
        }

        private static void DrawEllipse(Texture2D texture, int cx, int cy, int radiusX, int radiusY, Color32 color)
        {
            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    float nx = x / (float)radiusX;
                    float ny = y / (float)radiusY;
                    if (nx * nx + ny * ny <= 1f)
                        SetPixel(texture, cx + x, cy + y, color);
                }
            }
        }

        private static void DrawRectangle(Texture2D texture, int left, int bottom, int right, int top, Color32 color)
        {
            for (int y = bottom; y <= top; y++)
                for (int x = left; x <= right; x++)
                    SetPixel(texture, x, y, color);
        }

        private static void DrawTriangle(Texture2D texture, int ax, int ay, int bx, int by, int cx, int cy, Color32 color)
        {
            int minX = Mathf.Min(ax, Mathf.Min(bx, cx));
            int maxX = Mathf.Max(ax, Mathf.Max(bx, cx));
            int minY = Mathf.Min(ay, Mathf.Min(by, cy));
            int maxY = Mathf.Max(ay, Mathf.Max(by, cy));
            float area = Edge(ax, ay, bx, by, cx, cy);
            if (Mathf.Abs(area) < 0.001f)
                return;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float w0 = Edge(bx, by, cx, cy, x, y);
                    float w1 = Edge(cx, cy, ax, ay, x, y);
                    float w2 = Edge(ax, ay, bx, by, x, y);
                    if ((w0 >= 0f && w1 >= 0f && w2 >= 0f) || (w0 <= 0f && w1 <= 0f && w2 <= 0f))
                        SetPixel(texture, x, y, color);
                }
            }
        }

        private static float Edge(float ax, float ay, float bx, float by, float px, float py)
        {
            return (px - ax) * (by - ay) - (py - ay) * (bx - ax);
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color32 color, int width)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1))));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                DrawCircle(texture, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)),
                    Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), Mathf.Max(1, width / 2), color);
            }
        }

        private static void SetPixel(Texture2D texture, int x, int y, Color32 color)
        {
            if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                texture.SetPixel(x, y, color);
        }
    }
}
