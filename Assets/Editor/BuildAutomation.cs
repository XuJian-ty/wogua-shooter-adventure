using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using WoguaShooter.Core;
using WoguaShooter.Lua;
using WoguaShooter.UI;
using WoguaShooter.Update;

namespace WoguaShooter.Editor
{
    public static class BuildAutomation
    {
        private static double smokeStartTime;
        private static double gameplayStartTime;
        private static bool smokeStarted;
        private static bool smokeGameplayStarted;
        private static bool screenshotTaken;

        [MenuItem("Tools/Wogua Shooter/Build Addressables %#b")]
        public static void BuildAddressables()
        {
            HotUpdatePublisher.ValidateBaseBuildVersion();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error))
                throw new InvalidOperationException("Addressables 构建失败：" + result.Error);

            HotUpdatePublisher.ArchiveBaseBuildAndPublish(result);
            Debug.Log("Addressables 构建完成：" + result.OutputPath);
        }

        [MenuItem("Tools/Wogua Shooter/Prepare xLua Hotfix")]
        public static void PrepareXluaHotfix()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                    throw new BuildFailedException("无法切换到 Android 构建平台。");

                Debug.Log("已切换到 Android。请等待 Unity 编译完成后再次执行 Prepare xLua Hotfix。");
                return;
            }

            if (WoguaHotfixBuildGuard.EnsureAndroidDefine())
            {
                Debug.Log("已启用 Android HOTFIX_ENABLE。请等待 Unity 编译完成后再次执行 Prepare xLua Hotfix。");
                return;
            }

#if !HOTFIX_ENABLE
            Debug.Log("HOTFIX_ENABLE 正在生效。请等待 Unity 编译完成后再次执行 Prepare xLua Hotfix。");
            return;
#else
            if (EditorApplication.isCompiling)
                throw new BuildFailedException("Unity 正在编译脚本，请等待编译完成后重试。");

            WoguaHotfixBuildGuard.ValidateToolFiles();
            CSObjectWrapEditor.Generator.GenAll();
            AssetDatabase.SaveAssets();
            Debug.Log("xLua 代码生成完成。Unity 编译结束后即可构建 Android APK。");
#endif
        }

        [MenuItem("Tools/Wogua Shooter/Validate xLua Hotfix")]
        public static void ValidateXluaHotfix()
        {
            WoguaHotfixBuildGuard.ValidateAndroidPreparation();
            Debug.Log("XLUA_HOTFIX_PREPARATION_PASS");
        }

        public static void RunSmokeTest()
        {
            BuildAddressables();
            StartSmokeTest();
        }

        public static void RunHotfixSmokeTest()
        {
            StartSmokeTest();
        }

        private static void StartSmokeTest()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            smokeStarted = false;
            smokeGameplayStarted = false;
            screenshotTaken = false;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += SmokeTick;
            EditorApplication.EnterPlaymode();
        }

        public static void BuildWindowsPlayer()
        {
            BuildAddressables();
            string outputFolder = Path.GetFullPath("Builds/Windows");
            Directory.CreateDirectory(outputFolder);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Game.unity" },
                locationPathName = Path.Combine(outputFolder, "WoguaShooter.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows 构建失败：" + report.summary.result);
            Debug.Log($"WINDOWS_BUILD_PASS size={report.summary.totalSize}");
        }

        [MenuItem("Tools/Wogua Shooter/Build Android APK")]
        public static void BuildAndroidPlayer()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("无法切换到 Android 构建平台");

            EditorUserBuildSettings.buildAppBundle = false;
            ConfigureAndroidBranding();
            WoguaHotfixBuildGuard.ValidateAndroidPreparation();
            BuildAddressables();

            string outputFolder = Path.GetFullPath("Builds/Android");
            Directory.CreateDirectory(outputFolder);
            string outputPath = Path.Combine(outputFolder, "WoguaShooter.apk");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Game.unity" },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android 构建失败：" + report.summary.result);
            Debug.Log($"ANDROID_BUILD_PASS path={outputPath} size={report.summary.totalSize}");
        }

        public static void VerifyAndroidBaselineApk()
        {
            string apkPath = Path.GetFullPath("Builds/Android/WoguaShooter.apk");
            if (!File.Exists(apkPath))
                throw new FileNotFoundException("找不到待验证的 Android APK。", apkPath);

            string temporaryDirectory = Path.GetFullPath("Temp/ApkVerification");
            string bundlePath = Path.Combine(temporaryDirectory, "base-lua.bundle");
            AssetBundle bundle = null;
            try
            {
                Directory.CreateDirectory(temporaryDirectory);
                using (ZipArchive archive = ZipFile.OpenRead(apkPath))
                {
                    ZipArchiveEntry luaBundle = null;
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.StartsWith("assets/aa/Android/baselua", StringComparison.OrdinalIgnoreCase) &&
                            entry.FullName.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase))
                        {
                            luaBundle = entry;
                            break;
                        }
                    }

                    if (luaBundle == null)
                        throw new InvalidDataException("APK 中没有找到 Base Lua AssetBundle。");
                    luaBundle.ExtractToFile(bundlePath, true);
                }

                bundle = AssetBundle.LoadFromFile(bundlePath);
                if (bundle == null)
                    throw new InvalidDataException("无法读取 APK 内的 Base Lua AssetBundle。");

                TextAsset[] luaAssets = bundle.LoadAllAssets<TextAsset>();
                string combined = string.Empty;
                for (int i = 0; i < luaAssets.Length; i++)
                    combined += "\n" + luaAssets[i].text;

                if (!combined.Contains("version = \"1.0.0\"", StringComparison.Ordinal) ||
                    !combined.Contains("PATCH_1.0.0", StringComparison.Ordinal) ||
                    combined.Contains("PATCH_1.0.1", StringComparison.Ordinal))
                {
                    throw new InvalidDataException("APK 内置 Lua 不是干净的 1.0.0 基线。");
                }

                Debug.Log($"APK_BASELINE_VERIFY_PASS version=1.0.0 luaAssets={luaAssets.Length}");
            }
            finally
            {
                bundle?.Unload(true);
                if (Directory.Exists(temporaryDirectory))
                    Directory.Delete(temporaryDirectory, true);
            }
        }

        private static void ConfigureAndroidBranding()
        {
            const string iconPath = "Assets/Art/ui/generated/wogua_shooter_app_icon.png";
            PlayerSettings.productName = "倭瓜射手大冒险";
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (icon == null)
                throw new InvalidOperationException("Android 图标未导入：" + iconPath);

            NamedBuildTarget android = NamedBuildTarget.Android;
            int[] legacySizes = PlayerSettings.GetIconSizes(android, IconKind.Application);
            if (legacySizes.Length > 0)
            {
                Texture2D[] legacyIcons = new Texture2D[legacySizes.Length];
                for (int i = 0; i < legacyIcons.Length; i++)
                    legacyIcons[i] = icon;
                PlayerSettings.SetIcons(android, legacyIcons, IconKind.Application);
            }

            PlatformIconKind[] iconKinds = PlayerSettings.GetSupportedIconKinds(android);
            for (int kindIndex = 0; kindIndex < iconKinds.Length; kindIndex++)
            {
                PlatformIcon[] platformIcons = PlayerSettings.GetPlatformIcons(android, iconKinds[kindIndex]);
                for (int iconIndex = 0; iconIndex < platformIcons.Length; iconIndex++)
                {
                    Texture2D[] layers = new Texture2D[platformIcons[iconIndex].maxLayerCount];
                    for (int layer = 0; layer < layers.Length; layer++)
                        layers[layer] = icon;
                    platformIcons[iconIndex].SetTextures(layers);
                }
                PlayerSettings.SetPlatformIcons(android, iconKinds[kindIndex], platformIcons);
            }

            AssetDatabase.SaveAssets();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                smokeStartTime = EditorApplication.timeSinceStartup;
                smokeStarted = true;
            }
            else if (state == PlayModeStateChange.EnteredEditMode && smokeStarted)
            {
                CleanupSmokeTest();
                EditorApplication.Exit(0);
            }
        }

        private static void SmokeTick()
        {
            if (!smokeStarted || !EditorApplication.isPlaying)
                return;

            double elapsed = EditorApplication.timeSinceStartup - smokeStartTime;
            GameManager manager = UnityEngine.Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            LuaRuntimeBridge lua = UnityEngine.Object.FindFirstObjectByType<LuaRuntimeBridge>(FindObjectsInactive.Include);
            BackgroundScroller background = UnityEngine.Object.FindFirstObjectByType<BackgroundScroller>(FindObjectsInactive.Include);
            SpawnManager spawner = UnityEngine.Object.FindFirstObjectByType<SpawnManager>(FindObjectsInactive.Include);

            if (!smokeGameplayStarted)
            {
                if (manager == null || player == null || lua == null || !lua.IsLoaded)
                {
                    if (elapsed > 15d)
                        Fail("等待 Addressables 游戏根对象或 xLua 超时");
                    return;
                }

                player.gameObject.SetActive(true);
                player.enabled = true;
                FlowUIController flow = UnityEngine.Object.FindFirstObjectByType<FlowUIController>(FindObjectsInactive.Include);
                if (flow != null)
                {
                    flow.StopAllCoroutines();
                    flow.enabled = false;
                }
                Rigidbody2D body = player.GetComponent<Rigidbody2D>();
                if (body != null)
                    body.simulated = true;
                if (spawner == null || background == null)
                    Fail("游戏根对象缺少生成器或背景滚动组件");
                spawner.enabled = true;
                background.enabled = true;
                manager.SetCurrentLevel(2);
                background.StartCoroutine(background.ApplyLevelTheme(2));
                manager.ResetForNewGame();
                player.ActivateShield();
                smokeGameplayStarted = true;
                gameplayStartTime = EditorApplication.timeSinceStartup;
                return;
            }

            double gameplayElapsed = EditorApplication.timeSinceStartup - gameplayStartTime;
            if (gameplayElapsed > 3d && !screenshotTaken)
            {
                string folder = Path.GetFullPath("TestResults");
                Directory.CreateDirectory(folder);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, "game-smoke.png"));
                screenshotTaken = true;
            }

            if (gameplayElapsed < 14d)
                return;

            Projectile[] projectiles = UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            WorldHazard[] hazards = UnityEngine.Object.FindObjectsByType<WorldHazard>(FindObjectsSortMode.None);

            if (manager == null)
                Fail("GameManager 未由 Addressables 加载");
            if (player == null)
                Fail("玩家对象不存在");
            if (lua == null || !lua.IsLoaded)
                Fail("xLua 业务脚本未加载");
            if (manager.ElapsedTime < 2f)
                Fail("游戏计时没有正常推进");
            if (projectiles.Length == 0)
                Fail("自动射击未生成子弹");
            if (enemies.Length == 0)
                Fail("敌人生成逻辑未运行");
            System.Reflection.FieldInfo spikeSpawnedField = typeof(SpawnManager).GetField(
                "spikeSpawnedThisRound",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            bool spikeSpawned = spikeSpawnedField != null && (bool)spikeSpawnedField.GetValue(spawner);
            if (!spikeSpawned)
                Fail("地刺生成逻辑未运行");

            UnityEngine.UI.Text[] texts = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            UnityEngine.UI.Text versionText = Array.Find(
                texts,
                item => item != null && item.gameObject.name == "VersionText");
            string expectedVersion = string.IsNullOrWhiteSpace(HotUpdateState.ContentVersion)
                ? Application.version
                : HotUpdateState.ContentVersion;
            if (versionText == null || versionText.text != "版本 " + expectedVersion)
                Fail("主界面版本号未刷新，实际值：" + (versionText != null ? versionText.text : "缺失"));

            SpriteRenderer themedRenderer = background.GetComponentInChildren<SpriteRenderer>();
            Sprite themedSprite = themedRenderer != null ? themedRenderer.sprite : null;
            if (themedSprite == null || themedSprite.texture.width != 2000 || themedSprite.texture.height != 640)
                Fail("第2关循环背景未按 2000x640 规格加载");

            Debug.Log($"SMOKE_TEST_PASS level=2, time={manager.ElapsedTime:0.0}, projectiles={projectiles.Length}, enemies={enemies.Length}, hazards={hazards.Length}, lua={lua.IsLoaded}, versionLabel={versionText.text}, background={themedSprite.name}:{themedSprite.texture.width}x{themedSprite.texture.height}");
            EditorApplication.ExitPlaymode();
        }

        private static void Fail(string message)
        {
            Debug.LogError("SMOKE_TEST_FAIL: " + message);
            CleanupSmokeTest();
            EditorApplication.Exit(1);
            throw new InvalidOperationException(message);
        }

        private static void CleanupSmokeTest()
        {
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorApplication.update -= SmokeTick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
    }
}
