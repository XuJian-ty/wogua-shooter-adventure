using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WoguaShooter.Addressables;
using WoguaShooter.Config;
using WoguaShooter.Core;
using WoguaShooter.UI;

namespace WoguaShooter.Editor
{
    public static class GameplayFeatureSmokeTest
    {
        private static double startTime;
        private static double validationTime;
        private static bool enteredPlayMode;
        private static bool featuresCreated;
        private static bool screenshotTaken;
        private static bool secondScreenshotTaken;
        private static bool testRootCreated;
        private static double testRootCreatedTime;
        private static GameManager testManager;
        private static PlayerController testPlayer;
        private static bool exitEditorOnCompletion;

        [MenuItem("Tools/Wogua Shooter/Run Gameplay Feature Smoke Test")]
        public static void Run()
        {
            exitEditorOnCompletion = Application.isBatchMode;
            BuildAutomation.BuildAddressables();
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
            enteredPlayMode = false;
            featuresCreated = false;
            screenshotTaken = false;
            secondScreenshotTaken = false;
            testRootCreated = false;
            testManager = null;
            testPlayer = null;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                enteredPlayMode = true;
                startTime = EditorApplication.timeSinceStartup;
            }
            else if (state == PlayModeStateChange.EnteredEditMode && enteredPlayMode)
            {
                Cleanup();
                if (exitEditorOnCompletion)
                    EditorApplication.Exit(0);
                else
                    Debug.Log("GAMEPLAY_FEATURE_SMOKE_INTERACTIVE_COMPLETE");
            }
        }

        private static void Tick()
        {
            if (!enteredPlayMode || !EditorApplication.isPlaying)
                return;

            GameManager manager = testManager != null
                ? testManager
                : UnityEngine.Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            PlayerController player = testPlayer != null
                ? testPlayer
                : UnityEngine.Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            double elapsed = EditorApplication.timeSinceStartup - startTime;
            if (!featuresCreated)
            {
                if (manager == null || player == null)
                {
                    EnsureTestRoot();
                    if (elapsed > 12d)
                        Fail("等待 GameManager 或玩家对象超时");
                    return;
                }

                if (testRootCreated && EditorApplication.timeSinceStartup - testRootCreatedTime < 0.5d)
                    return;

                player.gameObject.SetActive(true);
                manager.ResetForNewGame();
                ValidatePlayerPowerUps(manager, player);
                ValidateSprites();
                CreateVisualTestObjects(manager, player);
                featuresCreated = true;
                validationTime = EditorApplication.timeSinceStartup;
                return;
            }

            double featureElapsed = EditorApplication.timeSinceStartup - validationTime;
            if (!screenshotTaken && featureElapsed > 1.35d)
            {
                FireParticleVfx fireVfx = UnityEngine.Object.FindFirstObjectByType<FireParticleVfx>();
                Require(fireVfx != null && fireVfx.ActiveParticleCount > 0,
                    "火焰粒子系统没有生成可见粒子");
                Debug.Log($"GAMEPLAY_FEATURE_FIRE_PARTICLES count={fireVfx.ActiveParticleCount}");
                ValidateRollingSkeletonHeadGrounding();
                CaptureTestScreenshot("gameplay-features.png");
                screenshotTaken = true;
            }

            if (!secondScreenshotTaken && featureElapsed > 1.75d)
            {
                CaptureTestScreenshot("gameplay-features-later.png");
                secondScreenshotTaken = true;
            }

            if (featureElapsed < 2d || !secondScreenshotTaken)
                return;

            if (manager == null || manager.State != GameState.Playing)
                Fail("新玩法对象生成后游戏意外结束");

            Debug.Log("GAMEPLAY_FEATURE_SMOKE_PASS shield=pass growth=pass magnet=pass coins=pass hazards=pass sprites=pass");
            EditorApplication.ExitPlaymode();
        }

        private static void EnsureTestRoot()
        {
            if (testRootCreated)
                return;

            GameBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null)
                UnityEngine.Object.Destroy(bootstrap);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/Prefabs/GameRoot.prefab");
            Require(prefab != null, "测试用 GameRoot 预制件加载失败");
            GameObject root = UnityEngine.Object.Instantiate(prefab);
            root.name = "GameRoot_SmokeTest";
            FlowUIController flow = root.GetComponentInChildren<FlowUIController>(true);
            if (flow != null)
            {
                flow.enabled = false;
                UnityEngine.Object.Destroy(flow);
            }
            testManager = root.GetComponentInChildren<GameManager>(true);
            testPlayer = root.GetComponentInChildren<PlayerController>(true);
            Require(testManager != null, "测试根对象中没有 GameManager");
            Require(testPlayer != null, "测试根对象中没有玩家组件");
            Debug.Log("GAMEPLAY_FEATURE_SMOKE_ROOT_CREATED");
            testRootCreated = true;
            testRootCreatedTime = EditorApplication.timeSinceStartup;
        }

        private static void ValidatePlayerPowerUps(GameManager manager, PlayerController player)
        {
            player.ResetSpellState();
            Require(player.ActivateShield(), "首次使用护盾失败");
            Require(!player.ActivateShield(), "护盾生效期间允许了重复使用");
            manager.PlayerHit();
            Require(manager.State == GameState.Playing, "护盾没有抵挡伤害");
            Require(!player.ShieldActive && player.DamageInvincible, "护盾破碎后的无敌状态不正确");

            player.ResetSpellState();
            Require(player.ActivateGrowth(), "首次使用变大道具失败");
            Require(!player.ActivateGrowth(), "变大期间允许了重复使用");
            Transform visual = player.transform.Find("Visual");
            Require(visual != null, "没有找到玩家视觉根节点");
            Vector3 grownScale = visual.localScale;
            Vector3 grownPosition = visual.localPosition;
            player.SendMessage("Jump", SendMessageOptions.RequireReceiver);
            Require(visual.localScale.x < grownScale.x && visual.localScale.y > grownScale.y,
                "变大状态跳跃时没有保留跳跃拉伸效果");
            Require(visual.localScale.x > grownScale.x * 0.85f && visual.localScale.y < grownScale.y * 1.2f,
                "变大状态跳跃时丢失了放大比例");
            Require(Vector3.Distance(visual.localPosition, grownPosition) <= 0.001f,
                "变大状态跳跃时改变了脚底锚点");
            manager.PlayerHit();
            Require(manager.State == GameState.Playing, "变大状态没有抵挡伤害");

            player.ResetSpellState();
            Require(player.ActivateMagnet(), "磁铁道具使用失败");
            Vector3 near = player.transform.position + new Vector3(0.95f, 8f, 0f);
            Vector3 far = player.transform.position + new Vector3(1.05f, 0f, 0f);
            Require(player.CanMagnetAttract(near), "磁铁没有忽略高度吸附一米内物品");
            Require(!player.CanMagnetAttract(far), "磁铁吸附了玩家前方一米外物品");
        }

        private static void ValidateSprites()
        {
            Require(GameplaySpriteLibrary.Get("coin") != null, "金币图片加载失败");
            Require(GameplaySpriteLibrary.Get("spikes") != null, "地刺图片加载失败");
            Require(GameplaySpriteLibrary.Get("missile") != null, "导弹图片加载失败");
            Require(SpellIconFactory.Get(SpellType.Magnet) != null, "磁铁图标加载失败");
            Require(SpellIconFactory.Get(SpellType.Shield) != null, "护盾图标加载失败");
            Require(SpellIconFactory.Get(SpellType.Growth) != null, "变大图标加载失败");
        }

        private static void CreateVisualTestObjects(GameManager manager, PlayerController player)
        {
            GameConfig config = manager.Config;
            Collider2D groundCollider = GameObject.Find("GroundCollider")?.GetComponent<Collider2D>();
            float ground = groundCollider != null
                ? groundCollider.bounds.max.y
                : config != null ? config.groundY : -1.28f;
            for (int i = 0; i < 7; i++)
                CoinCollectible.Create(new Vector3(-0.1f + i * 0.48f, ground + 0.45f + Mathf.Sin(i / 6f * Mathf.PI) * 0.9f, 0f));

            WorldHazard spikes = WorldHazard.Create(WorldHazardType.Spikes, new Vector3(3.5f, ground, 0f));
            WorldHazard fire = WorldHazard.Create(WorldHazardType.FirePit, new Vector3(7.2f, ground, 0f));
            WorldHazard.Create(WorldHazardType.Missile, new Vector3(5.3f, ground + 1.8f, 0f));
            Require(Mathf.Abs(spikes.GetComponent<SpriteRenderer>().bounds.min.y - ground) <= 0.015f,
                "地刺没有贴合地面顶面");
            Require(Mathf.Abs(fire.GetComponent<SpriteRenderer>().bounds.min.y - ground) <= 0.015f,
                "火坑没有贴合地面顶面");
            Require(UnityEngine.Object.FindObjectsByType<CoinCollectible>(FindObjectsSortMode.None).Length >= 4,
                "金币运行时对象生成失败");
            Require(UnityEngine.Object.FindObjectsByType<WorldHazard>(FindObjectsSortMode.None).Length >= 3,
                "地形陷阱或导弹运行时对象生成失败");

            GameObject skeletonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Generated/Prefabs/EnemySkeleton.prefab");
            Require(skeletonPrefab != null, "骷髅预制件加载失败");
            GameObject skeletonObject = UnityEngine.Object.Instantiate(
                skeletonPrefab,
                new Vector3(12f, config != null ? config.groundY : ground, 0f),
                Quaternion.identity);
            Enemy skeleton = skeletonObject.GetComponent<Enemy>();
            Require(skeleton != null, "骷髅预制件缺少 Enemy 组件");
            skeleton.Kill();

            player.ActivateShield();
            player.ActivateGrowth();
        }

        private static void CaptureTestScreenshot(string fileName)
        {
            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            Require(camera != null, "截图时未找到摄像机");
            camera.gameObject.SetActive(true);

            const int width = 1280;
            const int height = 720;
            RenderTexture renderTexture = new RenderTexture(width, height, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();

            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            screenshot.Apply();

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TestResults"));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, fileName), screenshot.EncodeToPNG());

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.Destroy(renderTexture);
            UnityEngine.Object.Destroy(screenshot);
        }

        private static void ValidateRollingSkeletonHeadGrounding()
        {
            GameObject head = GameObject.Find("HeadPlatform");
            Require(head != null, "没有找到死亡骷髅头碰撞对象");
            CircleCollider2D supportCollider = null;
            CircleCollider2D[] colliders = head.GetComponents<CircleCollider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (!colliders[i].isTrigger)
                {
                    supportCollider = colliders[i];
                    break;
                }
            }

            Require(supportCollider != null, "死亡骷髅头缺少实体碰撞体");
            Collider2D groundCollider = GameObject.Find("GroundCollider")?.GetComponent<Collider2D>();
            float ground = groundCollider != null
                ? groundCollider.bounds.max.y
                : testManager != null && testManager.Config != null ? testManager.Config.groundY : -1.28f;
            Require(Mathf.Abs(supportCollider.bounds.min.y - ground) <= 0.035f,
                $"死亡骷髅头没有贴合地面，偏差={supportCollider.bounds.min.y - ground:F3}");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                Fail(message);
        }

        private static void Fail(string message)
        {
            Debug.LogError("GAMEPLAY_FEATURE_SMOKE_FAIL: " + message);
            Cleanup();
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            if (exitEditorOnCompletion)
                EditorApplication.Exit(1);
            throw new InvalidOperationException(message);
        }

        private static void Cleanup()
        {
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
    }
}
