using UnityEditor;
using UnityEngine;

namespace WoguaShooter.Editor
{
    public static class GameplayAssetSetup
    {
        private static readonly string[] SpritePaths =
        {
            "Assets/Resources/Gameplay/coin.png",
            "Assets/Resources/Gameplay/spikes.png",
            "Assets/Resources/Gameplay/missile.png",
            "Assets/Resources/Gameplay/icon_magnet.png",
            "Assets/Resources/Gameplay/icon_shield.png",
            "Assets/Resources/Gameplay/icon_growth.png"
        };

        [MenuItem("Tools/Wogua Shooter/Configure Gameplay Sprites")]
        public static void ConfigureGeneratedSprites()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            for (int i = 0; i < SpritePaths.Length; i++)
            {
                TextureImporter importer = AssetImporter.GetAtPath(SpritePaths[i]) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning("未找到玩法图片：" + SpritePaths[i]);
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 256f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("GAMEPLAY_SPRITES_CONFIGURED count=" + SpritePaths.Length);
        }
    }
}
