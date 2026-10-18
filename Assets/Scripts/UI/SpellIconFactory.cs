using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using WoguaShooter.Config;
using WoguaShooter.Core;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WoguaShooter.UI
{
    /// <summary>
    /// Creates small crisp slot icons at runtime so every spell has a visual
    /// identity without adding another prefab dependency.
    /// </summary>
    public static class SpellIconFactory
    {
        private const int Size = 64;
        private static readonly Dictionary<SpellType, Sprite> Cache = new Dictionary<SpellType, Sprite>();
        private static readonly Dictionary<SpellType, AsyncOperationHandle<Sprite>> AddressableHandles =
            new Dictionary<SpellType, AsyncOperationHandle<Sprite>>();

        public static Sprite Get(SpellType type)
        {
            if (type == SpellType.None)
                return null;
            if (Cache.TryGetValue(type, out Sprite sprite))
                return sprite;

            sprite = LoadExistingIcon(type);
            if (sprite != null)
            {
                Cache.Add(type, sprite);
                return sprite;
            }

            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.name = $"SpellIcon_{type}";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, 0, 0, 0);
            texture.SetPixels32(pixels);

            switch (type)
            {
                case SpellType.MachineGun:
                    DrawMachineGun(texture);
                    break;
                case SpellType.CornCannon:
                    DrawCorn(texture);
                    break;
                case SpellType.WaveFlight:
                    DrawWave(texture);
                    break;
                case SpellType.TripleShot:
                    DrawTripleShot(texture);
                    break;
                case SpellType.Repeater:
                    DrawRepeater(texture);
                    break;
                case SpellType.DualMuzzle:
                    DrawDualMuzzle(texture);
                    break;
                case SpellType.Magnet:
                case SpellType.Shield:
                case SpellType.Growth:
                    DrawFallback(texture);
                    break;
                default:
                    DrawFallback(texture);
                    break;
            }

            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
            sprite.name = $"SpellIcon_{type}";
            Cache.Add(type, sprite);
            return sprite;
        }

        private static Sprite LoadExistingIcon(SpellType type)
        {
            string path = GetAssetPath(type);
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(path))
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int i = 0; i < assets.Length; i++)
                {
                    Sprite existing = assets[i] as Sprite;
                    if (existing != null)
                        return existing;
                }
            }
#endif

            string resourceName = GetResourceName(type);
            if (!string.IsNullOrEmpty(resourceName))
                return GameplaySpriteLibrary.Get(resourceName);

            string address = GetAddress(type);
            if (string.IsNullOrEmpty(address))
                return null;

            try
            {
                AsyncOperationHandle<Sprite> handle = UnityAddressables.LoadAssetAsync<Sprite>(address);
                Sprite loaded = handle.WaitForCompletion();
                if (handle.Status == AsyncOperationStatus.Succeeded && loaded != null)
                {
                    AddressableHandles[type] = handle;
                    return loaded;
                }

                if (handle.IsValid())
                    UnityAddressables.Release(handle);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"技能图标加载失败，将使用备用图标：{address}\n{exception.Message}");
            }

            return null;
        }

        private static string GetAssetPath(SpellType type)
        {
            switch (type)
            {
                case SpellType.MachineGun: return "Assets/Art/获得物/机枪.png";
                case SpellType.CornCannon: return "Assets/Art/获得物/玉米.png";
                case SpellType.WaveFlight: return "Assets/Art/ui/spells/icon_wave_flight.png";
                case SpellType.TripleShot: return "Assets/Art/ui/spells/icon_triple_shot.png";
                case SpellType.Repeater: return "Assets/Art/ui/spells/icon_repeater.png";
                case SpellType.DualMuzzle: return "Assets/Art/ui/spells/icon_dual_muzzle.png";
                case SpellType.Magnet: return "Assets/Resources/Gameplay/icon_magnet.png";
                case SpellType.Shield: return "Assets/Resources/Gameplay/icon_shield.png";
                case SpellType.Growth: return "Assets/Resources/Gameplay/icon_growth.png";
                default: return null;
            }
        }

        private static string GetResourceName(SpellType type)
        {
            switch (type)
            {
                case SpellType.Magnet: return "icon_magnet";
                case SpellType.Shield: return "icon_shield";
                case SpellType.Growth: return "icon_growth";
                default: return null;
            }
        }

        private static string GetAddress(SpellType type)
        {
            switch (type)
            {
                case SpellType.MachineGun: return "ui/spells/machine_gun";
                case SpellType.CornCannon: return "ui/spells/corn_cannon";
                case SpellType.WaveFlight: return "ui/spells/wave_flight";
                case SpellType.TripleShot: return "ui/spells/triple_shot";
                case SpellType.Repeater: return "ui/spells/repeater";
                case SpellType.DualMuzzle: return "ui/spells/dual_muzzle";
                default: return null;
            }
        }

        private static void DrawMachineGun(Texture2D texture)
        {
            Color32 outline = new Color32(26, 38, 34, 255);
            Color32 green = new Color32(112, 178, 102, 255);
            DrawLine(texture, 11, 32, 52, 32, outline, 12);
            DrawLine(texture, 11, 32, 52, 32, green, 7);
            DrawLine(texture, 20, 40, 31, 52, outline, 7);
            DrawLine(texture, 20, 40, 31, 50, new Color32(75, 121, 75, 255), 4);
            DrawCircle(texture, 48, 32, 5, new Color32(226, 241, 189, 255));
            DrawCircle(texture, 48, 32, 2, new Color32(54, 92, 58, 255));
        }

        private static void DrawCorn(Texture2D texture)
        {
            Color32 outline = new Color32(83, 58, 22, 255);
            Color32 yellow = new Color32(250, 193, 54, 255);
            Color32 kernel = new Color32(255, 225, 104, 255);
            DrawEllipse(texture, 34, 31, 17, 22, outline);
            DrawEllipse(texture, 34, 31, 13, 18, yellow);
            DrawLine(texture, 25, 24, 42, 40, kernel, 3);
            DrawLine(texture, 27, 34, 42, 21, kernel, 3);
            DrawLine(texture, 17, 44, 32, 29, new Color32(68, 143, 75, 255), 7);
            DrawLine(texture, 18, 48, 35, 35, new Color32(38, 98, 57, 255), 3);
        }

        private static void DrawWave(Texture2D texture)
        {
            Color32 outline = new Color32(30, 68, 94, 255);
            Color32 blue = new Color32(91, 202, 231, 255);
            Vector2 previous = new Vector2(7f, 32f);
            for (int i = 1; i <= 24; i++)
            {
                float x = 7f + i * 2.1f;
                float y = 32f + Mathf.Sin(i * Mathf.PI * 2f / 12f) * 15f;
                DrawLine(texture, previous.x, previous.y, x, y, outline, 9);
                DrawLine(texture, previous.x, previous.y, x, y, blue, 5);
                previous = new Vector2(x, y);
            }
            DrawLine(texture, 47, 25, 56, 32, new Color32(194, 244, 250, 255), 3);
            DrawLine(texture, 56, 32, 47, 39, new Color32(194, 244, 250, 255), 3);
        }

        private static void DrawTripleShot(Texture2D texture)
        {
            Color32 outline = new Color32(57, 34, 78, 255);
            Color32 purple = new Color32(195, 112, 231, 255);
            DrawCircle(texture, 15, 32, 7, purple);
            DrawCircle(texture, 49, 14, 7, purple);
            DrawCircle(texture, 49, 50, 7, purple);
            DrawLine(texture, 20, 31, 47, 16, outline, 8);
            DrawLine(texture, 20, 33, 47, 16, purple, 4);
            DrawLine(texture, 20, 33, 47, 48, outline, 8);
            DrawLine(texture, 20, 33, 47, 48, purple, 4);
            DrawLine(texture, 19, 32, 45, 32, outline, 8);
            DrawLine(texture, 19, 32, 45, 32, purple, 4);
        }

        private static void DrawRepeater(Texture2D texture)
        {
            Color32 outline = new Color32(96, 52, 29, 255);
            Color32 orange = new Color32(245, 145, 65, 255);
            DrawLine(texture, 16, 32, 48, 32, outline, 6);
            DrawCircle(texture, 18, 32, 10, orange);
            DrawCircle(texture, 47, 32, 10, new Color32(255, 202, 88, 255));
            DrawCircle(texture, 18, 32, 3, outline);
            DrawCircle(texture, 47, 32, 3, outline);
            DrawLine(texture, 27, 18, 38, 18, new Color32(255, 238, 174, 255), 3);
            DrawLine(texture, 38, 18, 34, 14, new Color32(255, 238, 174, 255), 3);
            DrawLine(texture, 38, 18, 34, 22, new Color32(255, 238, 174, 255), 3);
        }

        private static void DrawDualMuzzle(Texture2D texture)
        {
            Color32 outline = new Color32(27, 37, 45, 255);
            Color32 teal = new Color32(78, 190, 176, 255);
            DrawLine(texture, 11, 25, 51, 13, outline, 11);
            DrawLine(texture, 11, 25, 51, 13, teal, 6);
            DrawLine(texture, 11, 39, 51, 51, outline, 11);
            DrawLine(texture, 11, 39, 51, 51, new Color32(52, 129, 166, 255), 6);
            DrawCircle(texture, 11, 25, 5, new Color32(224, 247, 238, 255));
            DrawCircle(texture, 11, 39, 5, new Color32(224, 247, 238, 255));
        }

        private static void DrawFallback(Texture2D texture)
        {
            Color32 outline = new Color32(42, 28, 18, 255);
            Color32 accent = new Color32(235, 183, 85, 255);
            DrawCircle(texture, 32, 32, 19, outline);
            DrawCircle(texture, 32, 32, 13, accent);
            DrawLine(texture, 21, 32, 43, 32, outline, 4);
            DrawLine(texture, 32, 21, 32, 43, outline, 4);
        }

        private static void DrawCircle(Texture2D texture, int cx, int cy, int radius, Color32 color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                        SetPixel(texture, cx + x, cy + y, color);
                }
            }
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

        private static void DrawLine(Texture2D texture, float x0, float y0, float x1, float y1, Color32 color, int width)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)) * 2f));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                DrawCircle(texture, x, y, Mathf.Max(1, width / 2), color);
            }
        }

        private static void SetPixel(Texture2D texture, int x, int y, Color32 color)
        {
            if (x >= 0 && x < Size && y >= 0 && y < Size)
                texture.SetPixel(x, y, color);
        }
    }
}
