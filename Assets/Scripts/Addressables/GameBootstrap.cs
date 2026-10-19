using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using WoguaShooter.Config;
using WoguaShooter.Lua;
using WoguaShooter.Update;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace WoguaShooter.Addressables
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string gameRootAddress = "game/root";
        [SerializeField] private string updateConfigAddress = "config/update";

        private IEnumerator Start()
        {
            HotUpdateState.StartupCompleted = false;
            AsyncOperationHandle initialization = UnityAddressables.InitializeAsync(false);
            yield return initialization;
            bool initializationSucceeded = initialization.Status == AsyncOperationStatus.Succeeded;
            UnityAddressables.Release(initialization);
            if (!initializationSucceeded)
            {
                Debug.LogError("Addressables 初始化失败，无法启动游戏。");
                yield break;
            }

            Canvas bootstrapCanvas = CreateBootstrapCanvas(out GameObject eventSystem);
            HotUpdateStorage.ActiveState activeState = null;
            UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator activeLocator = null;
            if (HotUpdateStorage.TryLoadActive(out HotUpdateStorage.ActiveState storedState))
            {
                bool stateValid = true;
                if (!string.IsNullOrWhiteSpace(storedState.catalogFile))
                {
                    stateValid = HotUpdateStorage.VerifyFile(
                        storedState.catalogFile,
                        storedState.catalogSha256,
                        out string catalogError);
                    if (!stateValid)
                        Debug.LogWarning("缓存资源目录校验失败，回退包内版本：" + catalogError);
                    else
                    {
                        AsyncOperationHandle<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator> catalog =
                            UnityAddressables.LoadContentCatalogAsync(new Uri(storedState.catalogFile).AbsoluteUri, false);
                        yield return catalog;
                        stateValid = catalog.Status == AsyncOperationStatus.Succeeded;
                        if (stateValid)
                            activeLocator = catalog.Result;
                        else
                            Debug.LogWarning("缓存资源目录加载失败，回退包内版本。");
                        if (catalog.IsValid())
                            UnityAddressables.Release(catalog);
                    }
                }

                if (stateValid)
                    activeState = storedState;
                else if (activeLocator != null)
                {
                    UnityAddressables.RemoveResourceLocator(activeLocator);
                    activeLocator = null;
                }
            }

            LuaHotUpdateRuntime luaRuntime = LuaHotUpdateRuntime.EnsureCreated();
            yield return luaRuntime.InitializeBundled();
            if (activeState != null && activeState.luaFiles != null && activeState.luaFiles.Length > 0)
            {
                if (!luaRuntime.TryApplyPatchDirectory(
                        activeState.luaDirectory,
                        activeState.luaFiles,
                        out string patchError))
                {
                    Debug.LogWarning("缓存 Lua 补丁无效，回退包内版本：" + patchError);
                    if (activeLocator != null)
                    {
                        UnityAddressables.RemoveResourceLocator(activeLocator);
                        activeLocator = null;
                    }
                    activeState = null;
                }
                else
                {
                    Debug.Log("XLUA_HOTFIX_APPLIED version=" + activeState.contentVersion);
                }
            }

            HotUpdateState.ContentVersion = activeState != null
                ? activeState.contentVersion
                : Application.version;

            UpdateConfig updateConfig = null;
            AsyncOperationHandle<UpdateConfig> configHandle =
                UnityAddressables.LoadAssetAsync<UpdateConfig>(updateConfigAddress);
            yield return configHandle;
            if (configHandle.Status == AsyncOperationStatus.Succeeded)
                updateConfig = configHandle.Result;
            else
                Debug.LogWarning("更新配置加载失败，本次使用代码默认配置。");

            bool transientConfig = updateConfig == null;
            if (transientConfig)
                updateConfig = ScriptableObject.CreateInstance<UpdateConfig>();

            BootstrapUpdateController updater = gameObject.AddComponent<BootstrapUpdateController>();
            yield return updater.Run(updateConfig, bootstrapCanvas, luaRuntime, activeState);

            string rootAddress = !string.IsNullOrWhiteSpace(updateConfig.gameRootAddress)
                ? updateConfig.gameRootAddress
                : gameRootAddress;
            if (bootstrapCanvas != null)
            {
                bootstrapCanvas.gameObject.SetActive(false);
                Destroy(bootstrapCanvas.gameObject);
            }
            if (eventSystem != null)
            {
                eventSystem.SetActive(false);
                Destroy(eventSystem);
            }

            HotUpdateState.StartupCompleted = true;
            AsyncOperationHandle<GameObject> load = UnityAddressables.InstantiateAsync(rootAddress);
            yield return load;

            if (load.Status != AsyncOperationStatus.Succeeded)
                Debug.LogError($"无法加载游戏根预制件：{rootAddress}");

            if (configHandle.IsValid())
                UnityAddressables.Release(configHandle);
            if (transientConfig)
                Destroy(updateConfig);
        }

        private static Canvas CreateBootstrapCanvas(out GameObject eventSystemObject)
        {
            GameObject canvasObject = new GameObject(
                "BootstrapUpdateCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            eventSystemObject = null;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                eventSystemObject = new GameObject(
                    "BootstrapEventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
            }
            return canvas;
        }
    }
}
