using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using WoguaShooter.Config;
using WoguaShooter.Lua;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace WoguaShooter.Update
{
    /// <summary>GameRoot 创建前运行的更新流程，负责用户选择、校验和 last-good 切换。</summary>
    public sealed class BootstrapUpdateController : MonoBehaviour
    {
        private enum UserDecision
        {
            None,
            Update,
            Skip
        }

        private UpdateConfig config;
        private UpdatePanel panel;
        private StartupUpdateStatusView status;
        private LuaHotUpdateRuntime luaRuntime;
        private HotUpdateStorage.ActiveState activeState;
        private AsyncOperationHandle<GameObject> panelHandle;
        private bool hasPanelHandle;
        private UserDecision decision;

        public IEnumerator Run(
            UpdateConfig updateConfig,
            Canvas canvas,
            LuaHotUpdateRuntime runtime,
            HotUpdateStorage.ActiveState currentState)
        {
            config = updateConfig;
            luaRuntime = runtime;
            activeState = currentState;
            status = StartupUpdateStatusView.Create(canvas, null);

            if (config == null || !config.checkOnStart || string.IsNullOrWhiteSpace(config.manifestUrl))
                yield break;

            yield return LoadPanel(canvas != null ? canvas.transform : null);
            status?.ShowConnecting();

            UpdateManifest manifest = null;
            string manifestError = string.Empty;
            using (UnityWebRequest request = UnityWebRequest.Get(config.manifestUrl))
            {
                request.timeout = config.timeoutSeconds;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    manifestError = request.error;
                }
                else
                {
                    try
                    {
                        manifest = JsonUtility.FromJson<UpdateManifest>(request.downloadHandler.text);
                    }
                    catch (Exception exception)
                    {
                        manifestError = exception.Message;
                    }
                }
            }

            if (manifest == null || string.IsNullOrWhiteSpace(manifest.ResolvedVersion))
            {
                Debug.Log("更新服务器不可用或清单无效，继续离线游戏：" + manifestError);
                status?.ShowConnectionFailed();
                if (status != null)
                    yield return status.HideAfter(1.2f);
                CleanupPanel();
                yield break;
            }

            status?.ShowChecking();
            yield return new WaitForSecondsRealtime(0.25f);
            string currentVersion = activeState != null && !string.IsNullOrWhiteSpace(activeState.contentVersion)
                ? activeState.contentVersion
                : Application.version;
            if (!IsNewer(manifest.ResolvedVersion, currentVersion))
            {
                status?.ShowUpToDate();
                if (status != null)
                    yield return status.HideAfter(0.55f);
                CleanupPanel();
                yield break;
            }

            if (panel == null)
            {
                Debug.LogWarning("检测到新版本，但启动更新面板加载失败，本次继续使用现有版本。");
                status?.ShowInvalidResponse();
                if (status != null)
                    yield return status.HideAfter(1.2f);
                CleanupPanel();
                yield break;
            }

            status?.ShowUpdateFound();
            if (status != null)
                yield return status.HideAfter(0.2f);
            decision = UserDecision.None;
            panel.Show(manifest, () => decision = UserDecision.Update, () => decision = UserDecision.Skip);

            while (true)
            {
                while (decision == UserDecision.None)
                    yield return null;
                if (decision == UserDecision.Skip)
                    break;

                decision = UserDecision.None;
                bool succeeded = false;
                string updateError = string.Empty;
                panel.SetDownloading();
                yield return DownloadAndActivate(
                    manifest,
                    (success, error) =>
                    {
                        succeeded = success;
                        updateError = error;
                    });

                if (!succeeded)
                {
                    Debug.LogWarning("热更新失败，保留当前可用版本：" + updateError);
                    panel.SetFailed(updateError);
                    continue;
                }

                panel.SetComplete();
                while (decision == UserDecision.None)
                    yield return null;
                break;
            }

            panel.Hide();
            CleanupPanel();
        }

        private IEnumerator LoadPanel(Transform parent)
        {
            string address = config != null ? config.updatePanelAddress : "ui/update";
            if (parent == null || string.IsNullOrWhiteSpace(address))
                yield break;

            panelHandle = UnityAddressables.InstantiateAsync(address, parent);
            hasPanelHandle = true;
            yield return panelHandle;
            if (panelHandle.Status != AsyncOperationStatus.Succeeded || panelHandle.Result == null)
                yield break;

            panel = panelHandle.Result.GetComponent<UpdatePanel>();
            panel?.Hide();
        }

        private IEnumerator DownloadAndActivate(UpdateManifest manifest, Action<bool, string> completed)
        {
            string stagingDirectory;
            string luaDirectory;
            try
            {
                stagingDirectory = HotUpdateStorage.CreateStagingDirectory();
                luaDirectory = Path.Combine(stagingDirectory, "lua");
                Directory.CreateDirectory(luaDirectory);
            }
            catch (Exception exception)
            {
                completed(false, "无法创建更新临时目录：" + exception.Message);
                yield break;
            }

            IResourceLocator newLocator = null;
            string promotedDirectory = string.Empty;
            List<string> luaFiles;
            try
            {
                luaFiles = CopyPreviousLuaFiles(luaDirectory);
            }
            catch (Exception exception)
            {
                FailUpdate(stagingDirectory, promotedDirectory, newLocator, exception.Message, completed);
                yield break;
            }

            if (manifest.luaFiles != null)
            {
                for (int i = 0; i < manifest.luaFiles.Length; i++)
                {
                    LuaPatchFile file = manifest.luaFiles[i];
                    if (!TryNormalizeRelativePath(file?.path, out string relativePath))
                    {
                        FailUpdate(stagingDirectory, promotedDirectory, newLocator, "Lua 补丁路径无效", completed);
                        yield break;
                    }

                    string targetPath = Path.Combine(
                        luaDirectory,
                        relativePath.Replace('/', Path.DirectorySeparatorChar));
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    }
                    catch (Exception exception)
                    {
                        FailUpdate(stagingDirectory, promotedDirectory, newLocator, exception.Message, completed);
                        yield break;
                    }

                    if (!TryResolveUrl(file.url, out string fileUrl, out string urlError))
                    {
                        FailUpdate(stagingDirectory, promotedDirectory, newLocator, urlError, completed);
                        yield break;
                    }

                    bool downloaded = false;
                    string downloadError = string.Empty;
                    yield return DownloadFile(
                        fileUrl,
                        targetPath,
                        file.sha256,
                        (success, error) =>
                        {
                            downloaded = success;
                            downloadError = error;
                        });
                    if (!downloaded)
                    {
                        FailUpdate(stagingDirectory, promotedDirectory, newLocator, downloadError, completed);
                        yield break;
                    }
                    if (!luaFiles.Contains(relativePath))
                        luaFiles.Add(relativePath);
                }
            }

            string catalogPath = activeState?.catalogFile;
            string catalogSha256 = activeState?.catalogSha256;
            if (!string.IsNullOrWhiteSpace(manifest.catalogUrl))
            {
                string extension = GetCatalogExtension(manifest.catalogUrl);
                catalogPath = Path.Combine(stagingDirectory, "catalog" + extension);
                if (!TryResolveUrl(manifest.catalogUrl, out string catalogUrl, out string urlError))
                {
                    FailUpdate(stagingDirectory, promotedDirectory, newLocator, urlError, completed);
                    yield break;
                }

                bool downloaded = false;
                string downloadError = string.Empty;
                yield return DownloadFile(
                    catalogUrl,
                    catalogPath,
                    manifest.catalogSha256,
                    (success, error) =>
                    {
                        downloaded = success;
                        downloadError = error;
                    });
                if (!downloaded)
                {
                    FailUpdate(stagingDirectory, promotedDirectory, newLocator, downloadError, completed);
                    yield break;
                }
                catalogSha256 = manifest.catalogSha256;

                AsyncOperationHandle<IResourceLocator> catalogHandle;
                try
                {
                    catalogHandle = UnityAddressables.LoadContentCatalogAsync(
                        new Uri(catalogPath).AbsoluteUri,
                        false);
                }
                catch (Exception exception)
                {
                    FailUpdate(stagingDirectory, promotedDirectory, newLocator, exception.Message, completed);
                    yield break;
                }

                yield return catalogHandle;
                if (catalogHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    if (catalogHandle.IsValid())
                        UnityAddressables.Release(catalogHandle);
                    FailUpdate(stagingDirectory, promotedDirectory, newLocator, "资源目录加载失败", completed);
                    yield break;
                }

                newLocator = catalogHandle.Result;
                UnityAddressables.Release(catalogHandle);
                string preloadLabel = string.IsNullOrWhiteSpace(manifest.preloadLabel)
                    ? config.preloadLabel
                    : manifest.preloadLabel;
                if (!string.IsNullOrWhiteSpace(preloadLabel))
                {
                    bool resourcesDownloaded = false;
                    string resourcesError = string.Empty;
                    yield return DownloadAddressables(
                        preloadLabel,
                        (success, error) =>
                        {
                            resourcesDownloaded = success;
                            resourcesError = error;
                        });
                    if (!resourcesDownloaded)
                    {
                        FailUpdate(stagingDirectory, promotedDirectory, newLocator, resourcesError, completed);
                        yield break;
                    }
                }
            }

            if (luaFiles.Count > 0 &&
                !luaRuntime.ValidatePatchDirectory(luaDirectory, luaFiles, out string validationError))
            {
                FailUpdate(
                    stagingDirectory,
                    promotedDirectory,
                    newLocator,
                    "Lua 补丁校验失败：" + validationError,
                    completed);
                yield break;
            }

            status?.ShowApplying();
            try
            {
                promotedDirectory = HotUpdateStorage.PromoteStaging(stagingDirectory, manifest.ResolvedVersion);
                stagingDirectory = string.Empty;
            }
            catch (Exception exception)
            {
                FailUpdate(stagingDirectory, promotedDirectory, newLocator, exception.Message, completed);
                yield break;
            }

            string promotedLuaDirectory = Path.Combine(promotedDirectory, "lua");
            if (luaFiles.Count > 0 &&
                !luaRuntime.TryApplyPatchDirectory(promotedLuaDirectory, luaFiles, out string applyError))
            {
                FailUpdate(
                    stagingDirectory,
                    promotedDirectory,
                    newLocator,
                    "Lua 补丁加载失败：" + applyError,
                    completed);
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(manifest.catalogUrl))
                catalogPath = Path.Combine(promotedDirectory, Path.GetFileName(catalogPath));

            activeState = new HotUpdateStorage.ActiveState
            {
                contentVersion = manifest.ResolvedVersion,
                catalogFile = catalogPath,
                catalogSha256 = catalogSha256,
                luaDirectory = promotedLuaDirectory,
                luaFiles = luaFiles.ToArray()
            };
            try
            {
                HotUpdateStorage.SaveActive(activeState);
            }
            catch (Exception exception)
            {
                FailUpdate(stagingDirectory, promotedDirectory, newLocator, exception.Message, completed);
                yield break;
            }

            HotUpdateState.ContentVersion = manifest.ResolvedVersion;
            Debug.Log("HOTUPDATE_ACTIVATED version=" + manifest.ResolvedVersion);
            completed(true, string.Empty);
        }

        private IEnumerator DownloadAddressables(string label, Action<bool, string> completed)
        {
            AsyncOperationHandle<long> sizeHandle = UnityAddressables.GetDownloadSizeAsync(label);
            yield return sizeHandle;
            if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                UnityAddressables.Release(sizeHandle);
                completed(false, "无法计算资源下载大小");
                yield break;
            }

            long downloadSize = sizeHandle.Result;
            UnityAddressables.Release(sizeHandle);
            if (downloadSize <= 0)
            {
                completed(true, string.Empty);
                yield break;
            }

            AsyncOperationHandle downloadHandle = UnityAddressables.DownloadDependenciesAsync(label, false);
            while (!downloadHandle.IsDone)
            {
                status?.ShowDownloading("游戏资源", downloadHandle.PercentComplete);
                yield return null;
            }
            bool succeeded = downloadHandle.Status == AsyncOperationStatus.Succeeded;
            UnityAddressables.Release(downloadHandle);
            completed(succeeded, succeeded ? string.Empty : "游戏资源下载失败");
        }

        private IEnumerator DownloadFile(
            string url,
            string targetPath,
            string sha256,
            Action<bool, string> completed)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = Math.Max(config.timeoutSeconds, 10);
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    status?.ShowDownloading(Path.GetFileName(targetPath), request.downloadProgress);
                    yield return null;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    completed(false, "下载失败：" + request.error);
                    yield break;
                }

                try
                {
                    File.WriteAllBytes(targetPath, request.downloadHandler.data);
                }
                catch (Exception exception)
                {
                    completed(false, "保存更新文件失败：" + exception.Message);
                    yield break;
                }
            }

            if (!HotUpdateStorage.VerifyFile(targetPath, sha256, out string error))
            {
                completed(false, error);
                yield break;
            }
            completed(true, string.Empty);
        }

        private List<string> CopyPreviousLuaFiles(string destinationDirectory)
        {
            List<string> result = new List<string>();
            if (activeState?.luaFiles == null || !Directory.Exists(activeState.luaDirectory))
                return result;

            for (int i = 0; i < activeState.luaFiles.Length; i++)
            {
                if (!TryNormalizeRelativePath(activeState.luaFiles[i], out string relativePath))
                    continue;
                string source = Path.Combine(
                    activeState.luaDirectory,
                    relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(source))
                    continue;
                string destination = Path.Combine(
                    destinationDirectory,
                    relativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(source, destination, true);
                result.Add(relativePath);
            }
            return result;
        }

        private bool TryResolveUrl(string url, out string resolved, out string error)
        {
            try
            {
                resolved = Uri.TryCreate(url, UriKind.Absolute, out Uri absolute)
                    ? absolute.ToString()
                    : new Uri(new Uri(config.manifestUrl), url).ToString();
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                resolved = string.Empty;
                error = "更新地址无效：" + exception.Message;
                return false;
            }
        }

        private static bool TryNormalizeRelativePath(string value, out string normalized)
        {
            normalized = (value ?? string.Empty).Replace('\\', '/').TrimStart('/');
            if (string.IsNullOrWhiteSpace(normalized) || normalized.Contains("../") || normalized == ".." ||
                Path.IsPathRooted(normalized))
            {
                normalized = string.Empty;
                return false;
            }
            return true;
        }

        private static string GetCatalogExtension(string url)
        {
            try
            {
                Uri uri = new Uri(url, UriKind.RelativeOrAbsolute);
                string path = uri.IsAbsoluteUri ? uri.AbsolutePath : url;
                string extension = Path.GetExtension(path);
                return string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
            }
            catch (Exception)
            {
                return ".bin";
            }
        }

        private static bool IsNewer(string remote, string current)
        {
            if (Version.TryParse(remote, out Version remoteVersion) &&
                Version.TryParse(current, out Version currentVersion))
                return remoteVersion > currentVersion;
            return !string.Equals(remote, current, StringComparison.OrdinalIgnoreCase);
        }

        private static void FailUpdate(
            string stagingDirectory,
            string promotedDirectory,
            IResourceLocator locator,
            string error,
            Action<bool, string> completed)
        {
            if (locator != null)
                UnityAddressables.RemoveResourceLocator(locator);
            HotUpdateStorage.DeleteDirectoryQuietly(stagingDirectory);
            HotUpdateStorage.DeleteDirectoryQuietly(promotedDirectory);
            completed(false, error);
        }

        private void CleanupPanel()
        {
            panel = null;
            if (hasPanelHandle && panelHandle.IsValid())
                UnityAddressables.ReleaseInstance(panelHandle);
            hasPanelHandle = false;
        }
    }
}
