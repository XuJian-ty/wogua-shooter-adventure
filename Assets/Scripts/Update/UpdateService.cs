using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using WoguaShooter.Config;
using WoguaShooter.Lua;
using WoguaShooter.UI;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace WoguaShooter.Update
{
    public class UpdateService : MonoBehaviour
    {
        private const string ContentVersionKey = "content_version";
        private const string PatchFileName = "game_rules.lua.txt";

        [SerializeField] private UpdateConfig config;
        [SerializeField] private UpdatePanel panel;
        [SerializeField] private LuaRuntimeBridge luaRuntime;

        private UpdateManifest availableUpdate;
        private bool updating;
        private StartupUpdateStatusView startupStatus;

        public bool IsCheckFinished { get; private set; }
        public bool HasPendingUpdate => availableUpdate != null && panel != null && panel.gameObject.activeInHierarchy;

        /// <summary>更新检查结束后触发。参数表示是否检测到了可用更新。</summary>
        public event Action<bool> CheckFinished;

        private void Awake()
        {
            // 老版本的 GameRoot 没有流程控制器时，运行时自动补上，避免必须重新生成预制体。
            if (FindFirstObjectByType<FlowUIController>(FindObjectsInactive.Include) == null)
                gameObject.AddComponent<FlowUIController>();
        }

        private IEnumerator Start()
        {
            if (panel != null)
                panel.Hide();
            if (config == null || !config.checkOnStart)
            {
                IsCheckFinished = true;
                CheckFinished?.Invoke(false);
                yield break;
            }

            Canvas canvas = panel != null
                ? panel.GetComponentInParent<Canvas>(true)
                : FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            Font font = null;
            if (panel != null)
            {
                Text[] panelTexts = panel.GetComponentsInChildren<Text>(true);
                if (panelTexts.Length > 0)
                    font = panelTexts[0].font;
            }
            startupStatus = StartupUpdateStatusView.Create(canvas, font);
            yield return CheckForUpdate();
        }

        public IEnumerator CheckForUpdate()
        {
            IsCheckFinished = false;
            if (config == null || string.IsNullOrWhiteSpace(config.manifestUrl))
            {
                startupStatus?.ShowConnectionFailed();
                if (startupStatus != null)
                    yield return startupStatus.HideAfter(1.2f);
                IsCheckFinished = true;
                CheckFinished?.Invoke(false);
                yield break;
            }

            startupStatus?.ShowConnecting();
            using (UnityWebRequest request = UnityWebRequest.Get(config.manifestUrl))
            {
                request.timeout = config.timeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log($"更新服务器不可用，继续离线游戏：{request.error}");
                    startupStatus?.ShowConnectionFailed();
                    if (startupStatus != null)
                        yield return startupStatus.HideAfter(1.2f);
                    IsCheckFinished = true;
                    CheckFinished?.Invoke(false);
                    yield break;
                }

                Debug.Log("更新清单请求成功：" + request.downloadHandler.text);
                startupStatus?.ShowChecking();
                yield return new WaitForSecondsRealtime(0.35f);

                UpdateManifest manifest = null;
                string manifestError = null;
                try
                {
                    manifest = JsonUtility.FromJson<UpdateManifest>(request.downloadHandler.text);
                }
                catch (Exception exception)
                {
                    manifestError = exception.Message;
                }

                if (!string.IsNullOrEmpty(manifestError) || manifest == null || string.IsNullOrWhiteSpace(manifest.version))
                {
                    Debug.LogWarning("更新清单格式错误，已跳过检查：" + manifestError);
                    startupStatus?.ShowInvalidResponse();
                    if (startupStatus != null)
                        yield return startupStatus.HideAfter(1.2f);
                    IsCheckFinished = true;
                    CheckFinished?.Invoke(false);
                    yield break;
                }

                string currentVersion = PlayerPrefs.GetString(ContentVersionKey, Application.version);
                Debug.Log($"更新版本比较：remote={manifest?.version}, current={currentVersion}");
                if (!IsNewer(manifest.version, currentVersion))
                {
                    startupStatus?.ShowUpToDate();
                    if (startupStatus != null)
                        yield return startupStatus.HideAfter(0.65f);
                    IsCheckFinished = true;
                    CheckFinished?.Invoke(false);
                    yield break;
                }

                availableUpdate = manifest;
                if (panel != null)
                {
                    Debug.Log("显示更新弹窗：" + manifest.version);
                    startupStatus?.ShowUpdateFound();
                    if (startupStatus != null)
                        yield return startupStatus.HideAfter(0.25f);
                    panel.Show(manifest, StartUpdate, SkipUpdate);
                    IsCheckFinished = true;
                    CheckFinished?.Invoke(true);
                }
                else
                {
                    Debug.LogWarning("检测到新版本，但场景中没有更新面板。");
                    startupStatus?.ShowInvalidResponse();
                    if (startupStatus != null)
                        yield return startupStatus.HideAfter(1.2f);
                    IsCheckFinished = true;
                    CheckFinished?.Invoke(true);
                }
            }
        }

        private void StartUpdate()
        {
            if (!updating && availableUpdate != null)
                StartCoroutine(DownloadUpdate());
        }

        private void SkipUpdate()
        {
            panel?.Hide();
            IsCheckFinished = true;
            CheckFinished?.Invoke(true);
        }

        private IEnumerator DownloadUpdate()
        {
            updating = true;
            panel?.SetDownloading();

            if (!string.IsNullOrWhiteSpace(availableUpdate.catalogUrl))
            {
                string catalogUrl = ResolveUrl(availableUpdate.catalogUrl);
                AsyncOperationHandle<IResourceLocator> catalog = UnityAddressables.LoadContentCatalogAsync(catalogUrl, false);
                yield return catalog;
                bool catalogLoaded = catalog.Status == AsyncOperationStatus.Succeeded;
                UnityAddressables.Release(catalog);
                if (!catalogLoaded)
                {
                    panel?.SetFailed("资源目录下载失败");
                    updating = false;
                    yield break;
                }
            }

            if (!string.IsNullOrWhiteSpace(availableUpdate.luaUrl))
            {
                using (UnityWebRequest request = UnityWebRequest.Get(ResolveUrl(availableUpdate.luaUrl)))
                {
                    request.timeout = Math.Max(config.timeoutSeconds, 10);
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        panel?.SetFailed("更新文件下载失败");
                        updating = false;
                        yield break;
                    }

                    string source = request.downloadHandler.text;
                    if (luaRuntime == null)
                    {
                        panel?.SetFailed("热更新模块不可用");
                        updating = false;
                        yield break;
                    }

                    if (!luaRuntime.ValidateRules(source, out string validationError))
                    {
                        panel?.SetFailed("热更新文件校验未通过");
                        Debug.LogWarning("Lua 热更新校验失败：" + validationError);
                        updating = false;
                        yield break;
                    }

                    string directory = Path.Combine(Application.persistentDataPath, "HotUpdate");
                    string patchPath = Path.Combine(directory, PatchFileName);
                    string temporaryPath = patchPath + ".download";
                    try
                    {
                        Directory.CreateDirectory(directory);
                        File.WriteAllText(temporaryPath, source);
                        if (File.Exists(patchPath))
                            File.Delete(patchPath);
                        File.Move(temporaryPath, patchPath);
                    }
                    catch (Exception exception)
                    {
                        panel?.SetFailed("无法保存更新文件");
                        Debug.LogWarning("Lua 热更新保存失败：" + exception.Message);
                        updating = false;
                        yield break;
                    }

                    if (!luaRuntime.ReloadFromFile(patchPath, out string reloadError))
                    {
                        panel?.SetFailed("无法加载更新文件");
                        Debug.LogWarning("Lua 热更新加载失败：" + reloadError);
                        updating = false;
                        yield break;
                    }
                }
            }

            PlayerPrefs.SetString(ContentVersionKey, availableUpdate.version);
            PlayerPrefs.Save();
            panel?.SetComplete();
            updating = false;
        }

        private string ResolveUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri absolute))
                return absolute.ToString();
            return new Uri(new Uri(config.manifestUrl), url).ToString();
        }

        private static bool IsNewer(string remote, string current)
        {
            if (Version.TryParse(remote, out Version remoteVersion) && Version.TryParse(current, out Version currentVersion))
                return remoteVersion > currentVersion;
            return !string.Equals(remote, current, StringComparison.OrdinalIgnoreCase);
        }
    }
}
