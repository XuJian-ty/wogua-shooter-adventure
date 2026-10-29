using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEngine;
using XLua;

namespace WoguaShooter.Editor
{
    /// <summary>把 Addressables 和 Lua 补丁整理成更新服务器可直接提供的文件。</summary>
    public static class HotUpdatePublisher
    {
        private const string PublicBaseUrl = "https://update.xinyin.space";
        private const string PlatformName = "Android";
        private const string ManifestRelativePath = "channels/stable-v2/android.json";
        private const string ContentVersionFile = "Server/content-version.txt";
        private const string LuaSourceDirectory = "Assets/Lua";

        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private static readonly string[] LuaModuleFiles =
        {
            "config.lua.txt",
            "player_rules.lua.txt",
            "weapon_rules.lua.txt",
            "spawn_rules.lua.txt",
            "combat_rules.lua.txt",
            "progression_rules.lua.txt",
            "tutorial_rules.lua.txt",
            "flow_rules.lua.txt",
            "hotfix_main.lua.txt"
        };

        [Serializable]
        private sealed class PublishedManifest
        {
            public int schemaVersion = 2;
            public string contentVersion;
            public string minClientVersion;
            public string title = "发现新版本";
            public string message = "更新内容：\n1. 第2关“暮色荒原”换用全新循环地图\n2. 第2关敌人和障碍节奏略微加快\n3. 第1关最高分达到2000分即可解锁第2关";
            public string catalogUrl;
            public string catalogSha256;
            public string preloadLabel = "hot-update-preload";
            public PublishedLuaFile[] luaFiles;
        }

        [Serializable]
        private sealed class PublishedLuaFile
        {
            public string path;
            public string url;
            public string sha256;
        }

        [MenuItem("Tools/Wogua Shooter/Hot Update/Build Content Update")]
        public static void BuildContentUpdate()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                throw new InvalidOperationException("Addressables 设置不存在。");

            string contentVersion = ReadContentVersion();
            if (CompareVersions(contentVersion, PlayerSettings.bundleVersion) <= 0)
            {
                throw new InvalidOperationException(
                    $"内容更新版本 {contentVersion} 必须高于安装包版本 {PlayerSettings.bundleVersion}。" +
                    $"请先修改 {ContentVersionFile}。");
            }

            string baseStatePath = GetArchivedStatePath(PlayerSettings.bundleVersion);
            if (!File.Exists(baseStatePath))
                throw new FileNotFoundException("找不到当前 APK 的 Addressables 基线状态文件。请先构建一次 APK。", baseStatePath);

            Dictionary<AddressableAssetEntry, AddressableAssetGroup> originalGroups =
                new Dictionary<AddressableAssetEntry, AddressableAssetGroup>();
            AddressableAssetGroup temporaryGroup = null;
            try
            {
                List<AddressableAssetEntry> modifiedEntries =
                    ContentUpdateScript.GatherModifiedEntries(settings, baseStatePath);
                if (modifiedEntries == null)
                    throw new InvalidOperationException("无法读取 Addressables 基线状态文件。");

                if (modifiedEntries.Count > 0)
                {
                    for (int i = 0; i < modifiedEntries.Count; i++)
                        originalGroups[modifiedEntries[i]] = modifiedEntries[i].parentGroup;

                    HashSet<AddressableAssetGroup> groupsBefore =
                        new HashSet<AddressableAssetGroup>(settings.groups);
                    ContentUpdateScript.CreateContentUpdateGroup(
                        settings,
                        modifiedEntries,
                        "Content Update " + contentVersion);
                    for (int i = 0; i < settings.groups.Count; i++)
                    {
                        if (!groupsBefore.Contains(settings.groups[i]))
                        {
                            temporaryGroup = settings.groups[i];
                            break;
                        }
                    }
                }

                AddressablesPlayerBuildResult result =
                    ContentUpdateScript.BuildContentUpdate(settings, baseStatePath);
                if (result == null || !string.IsNullOrEmpty(result.Error))
                    throw new InvalidOperationException("Addressables 内容更新构建失败：" + result?.Error);

                PublishBuild(result, contentVersion);
                Debug.Log($"HOT_UPDATE_CONTENT_PUBLISHED version={contentVersion}");
            }
            finally
            {
                RestoreTemporaryEntries(settings, originalGroups, temporaryGroup);
            }
        }

        public static void ValidateBaseBuildVersion()
        {
            string contentVersion = ReadContentVersion();
            if (CompareVersions(contentVersion, PlayerSettings.bundleVersion) != 0)
            {
                throw new BuildFailedException(
                    $"当前内容版本为 {contentVersion}，安装包版本为 {PlayerSettings.bundleVersion}。" +
                    "为避免覆盖已归档的基础包状态，请发布内容更新；如需构建新 APK，请先同步安装包版本。");
            }
        }

        [MenuItem("Tools/Wogua Shooter/Hot Update/Publish Lua Only")]
        public static void PublishLuaOnly()
        {
            string contentVersion = ReadContentVersion();
            WritePublishedFiles(contentVersion, string.Empty);
            Debug.Log($"HOT_UPDATE_LUA_PUBLISHED version={contentVersion}");
        }

        public static void ArchiveBaseBuildAndPublish(AddressablesPlayerBuildResult result)
        {
            ValidateBaseBuildVersion();
            if (result == null || !string.IsNullOrEmpty(result.Error))
                throw new InvalidOperationException("不能发布失败的 Addressables 构建。");

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            string statePath = Path.GetFullPath(
                ContentUpdateScript.GetContentStateDataPath(false, settings));
            if (!File.Exists(statePath))
                throw new FileNotFoundException("Addressables 构建后没有生成状态文件。", statePath);

            string archivePath = GetArchivedStatePath(PlayerSettings.bundleVersion);
            Directory.CreateDirectory(Path.GetDirectoryName(archivePath));
            File.Copy(statePath, archivePath, true);

            PublishBuild(result, ReadContentVersion());
            Debug.Log($"ADDRESSABLES_BASE_ARCHIVED path={archivePath}");
        }

        private static void PublishBuild(AddressablesPlayerBuildResult result, string contentVersion)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            string remoteBuildPath = settings.RemoteCatalogBuildPath.GetValue(settings);
            string sourceDirectory = Path.GetFullPath(remoteBuildPath);
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException("Addressables 输出目录不存在：" + sourceDirectory);

            string publicPlatformDirectory = GetPublicPlatformDirectory();
            CopyDirectory(sourceDirectory, publicPlatformDirectory);
            string catalogPath = CreateVersionedCatalog(
                FindCatalog(publicPlatformDirectory),
                contentVersion);
            WritePublishedFiles(contentVersion, catalogPath);
        }

        private static void WritePublishedFiles(string contentVersion, string catalogPath)
        {
            List<PublishedLuaFile> luaFiles = PublishLuaFiles(contentVersion);
            PublishedManifest manifest = new PublishedManifest
            {
                contentVersion = contentVersion,
                minClientVersion = PlayerSettings.bundleVersion,
                catalogUrl = string.IsNullOrEmpty(catalogPath)
                    ? string.Empty
                    : $"{PublicBaseUrl}/addressables/{PlatformName}/{Path.GetFileName(catalogPath)}",
                catalogSha256 = string.IsNullOrEmpty(catalogPath)
                    ? string.Empty
                    : ComputeSha256(catalogPath),
                luaFiles = luaFiles.ToArray()
            };

            string manifestPath = Path.GetFullPath(Path.Combine("Server/Public", ManifestRelativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true) + Environment.NewLine, Utf8WithoutBom);
        }

        private static List<PublishedLuaFile> PublishLuaFiles(string contentVersion)
        {
            string sourceDirectory = Path.GetFullPath(LuaSourceDirectory);
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException("Lua 源目录不存在：" + sourceDirectory);

            ValidateLuaSources(sourceDirectory);

            string relativeDirectory = $"lua/{contentVersion}";
            string targetDirectory = Path.GetFullPath(Path.Combine("Server/Public", relativeDirectory));
            Directory.CreateDirectory(targetDirectory);

            List<PublishedLuaFile> published = new List<PublishedLuaFile>(LuaModuleFiles.Length);
            for (int i = 0; i < LuaModuleFiles.Length; i++)
            {
                string fileName = LuaModuleFiles[i];
                string sourcePath = Path.Combine(sourceDirectory, fileName);
                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException("缺少 Lua 热更新模块。", sourcePath);
                string targetPath = Path.Combine(targetDirectory, fileName);
                File.Copy(sourcePath, targetPath, true);
                published.Add(new PublishedLuaFile
                {
                    path = fileName,
                    url = $"{PublicBaseUrl}/{relativeDirectory}/{fileName}",
                    sha256 = ComputeSha256(targetPath)
                });
            }

            return published;
        }

        private static void ValidateLuaSources(string sourceDirectory)
        {
            LuaEnv validationEnvironment = new LuaEnv();
            try
            {
                for (int i = 0; i < LuaModuleFiles.Length; i++)
                {
                    string fileName = LuaModuleFiles[i];
                    string sourcePath = Path.Combine(sourceDirectory, fileName);
                    if (!File.Exists(sourcePath))
                        throw new FileNotFoundException("缺少 Lua 热更新模块。", sourcePath);

                    using (LuaFunction compiled = validationEnvironment.LoadString(
                        File.ReadAllText(sourcePath),
                        fileName))
                    {
                    }
                }
            }
            finally
            {
                validationEnvironment.Dispose();
            }
        }

        private static void RestoreTemporaryEntries(
            AddressableAssetSettings settings,
            Dictionary<AddressableAssetEntry, AddressableAssetGroup> originalGroups,
            AddressableAssetGroup temporaryGroup)
        {
            foreach (KeyValuePair<AddressableAssetEntry, AddressableAssetGroup> pair in originalGroups)
            {
                if (pair.Key != null && pair.Value != null)
                    settings.MoveEntry(pair.Key, pair.Value, false, false);
            }

            if (temporaryGroup != null)
                settings.RemoveGroup(temporaryGroup);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
        }

        private static string ReadContentVersion()
        {
            string path = Path.GetFullPath(ContentVersionFile);
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, PlayerSettings.bundleVersion + Environment.NewLine, Utf8WithoutBom);
                return PlayerSettings.bundleVersion;
            }

            string version = File.ReadAllText(path).Trim();
            if (string.IsNullOrWhiteSpace(version))
                throw new InvalidDataException(ContentVersionFile + " 不能为空。");
            return version;
        }

        private static int CompareVersions(string left, string right)
        {
            if (Version.TryParse(left, out Version leftVersion) &&
                Version.TryParse(right, out Version rightVersion))
                return leftVersion.CompareTo(rightVersion);
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetArchivedStatePath(string playerVersion)
        {
            return Path.GetFullPath(Path.Combine(
                "Server/ContentState",
                PlatformName,
                playerVersion,
                "addressables_content_state.bin"));
        }

        private static string GetPublicPlatformDirectory()
        {
            return Path.GetFullPath(Path.Combine("Server/Public/addressables", PlatformName));
        }

        private static string FindCatalog(string directory)
        {
            if (!Directory.Exists(directory))
                return string.Empty;

            string expectedCatalog = Path.Combine(
                directory,
                $"catalog_{PlayerSettings.bundleVersion}.bin");
            if (File.Exists(expectedCatalog))
                return expectedCatalog;

            string[] catalogs = Directory.GetFiles(directory, "catalog_*.bin", SearchOption.TopDirectoryOnly);
            if (catalogs.Length == 0)
                return string.Empty;
            Array.Sort(catalogs, StringComparer.OrdinalIgnoreCase);
            return catalogs[catalogs.Length - 1];
        }

        private static string CreateVersionedCatalog(string sourcePath, string contentVersion)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                return string.Empty;

            StringBuilder fileVersion = new StringBuilder(contentVersion.Length);
            for (int i = 0; i < contentVersion.Length; i++)
            {
                char character = contentVersion[i];
                fileVersion.Append(char.IsLetterOrDigit(character) || character == '.' ||
                    character == '-' || character == '_' ? character : '_');
            }

            string catalogHash = ComputeSha256(sourcePath);
            string shortHash = catalogHash.Substring(0, 12);
            string targetPath = Path.Combine(
                Path.GetDirectoryName(sourcePath),
                $"catalog_{fileVersion}_{shortHash}.bin");
            if (!string.Equals(
                    Path.GetFullPath(sourcePath),
                    Path.GetFullPath(targetPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(sourcePath, targetPath, true);
            }
            return targetPath;
        }

        private static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);
            string[] files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string relativePath = files[i].Substring(sourceDirectory.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string targetPath = Path.Combine(targetDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Copy(files[i], targetPath, true);
            }
        }

        private static string ComputeSha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(stream);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    builder.Append(hash[i].ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
