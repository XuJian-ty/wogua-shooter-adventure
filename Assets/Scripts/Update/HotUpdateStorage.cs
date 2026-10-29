using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace WoguaShooter.Update
{
    public static class HotUpdateStorage
    {
        [Serializable]
        public sealed class ActiveState
        {
            public int cacheGeneration;
            public string contentVersion;
            public string catalogFile;
            public string catalogSha256;
            public string luaDirectory;
            public string[] luaFiles;
        }

        private const string RootFolderName = "HotUpdateV2";
        private const string StateFileName = "active.json";

        public static string RootDirectory => Path.Combine(Application.persistentDataPath, RootFolderName);
        public static string ActiveStatePath => Path.Combine(RootDirectory, StateFileName);

        public static bool TryLoadActive(out ActiveState state)
        {
            state = null;
            try
            {
                if (!File.Exists(ActiveStatePath))
                    return false;

                state = JsonUtility.FromJson<ActiveState>(File.ReadAllText(ActiveStatePath));
                return state != null && !string.IsNullOrWhiteSpace(state.contentVersion);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("读取热更新状态失败，使用包内版本：" + exception.Message);
                state = null;
                return false;
            }
        }

        public static string CreateStagingDirectory()
        {
            Directory.CreateDirectory(RootDirectory);
            string path = Path.Combine(RootDirectory, "staging-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        public static string PromoteStaging(string stagingDirectory, string version)
        {
            string versionsDirectory = Path.Combine(RootDirectory, "versions");
            Directory.CreateDirectory(versionsDirectory);
            string finalDirectory = Path.Combine(versionsDirectory, SanitizeSegment(version));

            if (Directory.Exists(finalDirectory))
                Directory.Delete(finalDirectory, true);
            Directory.Move(stagingDirectory, finalDirectory);
            return finalDirectory;
        }

        public static void SaveActive(ActiveState state)
        {
            Directory.CreateDirectory(RootDirectory);
            string temporaryPath = ActiveStatePath + ".next";
            string backupPath = ActiveStatePath + ".last-good";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(state, true));

            if (File.Exists(ActiveStatePath))
            {
                try
                {
                    File.Replace(temporaryPath, ActiveStatePath, backupPath);
                    return;
                }
                catch (PlatformNotSupportedException)
                {
                }
                catch (IOException)
                {
                }

                File.Copy(ActiveStatePath, backupPath, true);
                File.Delete(ActiveStatePath);
            }

            File.Move(temporaryPath, ActiveStatePath);
        }

        public static void ClearAll()
        {
            DeleteDirectoryQuietly(RootDirectory);
        }

        public static bool VerifyFile(string path, string expectedSha256, out string error)
        {
            if (!File.Exists(path))
            {
                error = "文件不存在：" + path;
                return false;
            }

            if (string.IsNullOrWhiteSpace(expectedSha256))
            {
                error = "更新清单缺少 SHA-256：" + Path.GetFileName(path);
                return false;
            }

            string actual = ComputeSha256(path);
            if (!string.Equals(actual, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                error = $"SHA-256 不匹配：{Path.GetFileName(path)}";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static string ComputeSha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha256 = SHA256.Create())
                return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }

        public static void DeleteDirectoryQuietly(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("清理热更新临时目录失败：" + exception.Message);
            }
        }

        private static string SanitizeSegment(string value)
        {
            string safe = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
                safe = safe.Replace(invalid, '_');
            return safe;
        }
    }
}
