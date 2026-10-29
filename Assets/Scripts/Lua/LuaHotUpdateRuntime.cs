using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using WoguaShooter.Diagnostics;
using XLua;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace WoguaShooter.Lua
{
    /// <summary>常驻的 xLua 环境。启动层应用补丁后才创建游戏业务对象。</summary>
    public sealed class LuaHotUpdateRuntime : MonoBehaviour
    {
        private readonly struct ModuleSpec
        {
            public ModuleSpec(string fileName, string address)
            {
                FileName = fileName;
                Address = address;
            }

            public string FileName { get; }
            public string Address { get; }
        }

        private static readonly ModuleSpec[] Modules =
        {
            new ModuleSpec("config.lua.txt", "lua/config"),
            new ModuleSpec("player_rules.lua.txt", "lua/player_rules"),
            new ModuleSpec("weapon_rules.lua.txt", "lua/weapon_rules"),
            new ModuleSpec("spawn_rules.lua.txt", "lua/spawn_rules"),
            new ModuleSpec("combat_rules.lua.txt", "lua/combat_rules"),
            new ModuleSpec("progression_rules.lua.txt", "lua/progression_rules"),
            new ModuleSpec("tutorial_rules.lua.txt", "lua/tutorial_rules"),
            new ModuleSpec("flow_rules.lua.txt", "lua/flow_rules"),
            new ModuleSpec("hotfix_main.lua.txt", "lua/hotfix_main")
        };

        private static LuaHotUpdateRuntime instance;
        private readonly Dictionary<string, string> bundledSources =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private LuaEnv luaEnv;
        private float tickTimer;

        public static LuaHotUpdateRuntime Instance => instance;
        public bool IsReady { get; private set; }
        public string ActivePatchDirectory { get; private set; }

        public static LuaHotUpdateRuntime EnsureCreated()
        {
            if (instance != null)
                return instance;

            GameObject root = new GameObject("LuaHotUpdateRuntime");
            instance = root.AddComponent<LuaHotUpdateRuntime>();
            DontDestroyOnLoad(root);
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public IEnumerator InitializeBundled()
        {
            if (IsReady)
                yield break;

            bundledSources.Clear();
            for (int i = 0; i < Modules.Length; i++)
            {
                ModuleSpec module = Modules[i];
                AsyncOperationHandle<TextAsset> handle = UnityAddressables.LoadAssetAsync<TextAsset>(module.Address);
                yield return handle;
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    if (handle.IsValid())
                        UnityAddressables.Release(handle);
                    throw new InvalidOperationException("包内 Lua 模块加载失败：" + module.Address);
                }

                bundledSources[module.FileName] = handle.Result.text;
                UnityAddressables.Release(handle);
            }

            if (!TryReplaceEnvironment(bundledSources, out string error))
                throw new InvalidOperationException("包内 Lua 模块初始化失败：" + error);

            IsReady = true;
            ActivePatchDirectory = string.Empty;
        }

        public bool ValidatePatchDirectory(string directory, IEnumerable<string> patchFiles, out string error)
        {
            if (!TryComposeSources(directory, patchFiles, out Dictionary<string, string> sources, out error))
                return false;

            LuaEnv validationEnv = null;
            try
            {
                validationEnv = new LuaEnv();
                foreach (ModuleSpec module in Modules)
                {
                    using (LuaFunction compiled = validationEnv.LoadString(sources[module.FileName], module.FileName))
                    {
                    }
                }

                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                validationEnv?.Dispose();
            }
        }

        public bool TryApplyPatchDirectory(string directory, IEnumerable<string> patchFiles, out string error)
        {
            if (!TryComposeSources(directory, patchFiles, out Dictionary<string, string> sources, out error))
                return false;
            if (!TryReplaceEnvironment(sources, out error))
                return false;

            ActivePatchDirectory = directory;
            IsReady = true;
            return true;
        }

        public float CallFloat(string tableName, string functionName, float fallback, params object[] arguments)
        {
            if (luaEnv == null)
                return fallback;

            LuaTable table = null;
            LuaFunction function = null;
            try
            {
                table = luaEnv.Global.Get<LuaTable>(tableName);
                function = table?.Get<LuaFunction>(functionName);
                object[] result = function?.Call(arguments);
                return result == null || result.Length == 0 ? fallback : Convert.ToSingle(result[0]);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Lua 规则调用失败：{tableName}.{functionName}，使用 C# 默认值。{exception.Message}");
                return fallback;
            }
            finally
            {
                function?.Dispose();
                table?.Dispose();
            }
        }

        public int GetInt(string tableName, string fieldName, int fallback)
        {
            if (luaEnv == null)
                return fallback;
            LuaTable table = null;
            try
            {
                table = luaEnv.Global.Get<LuaTable>(tableName);
                object value = table?.Get<object>(fieldName);
                return value == null ? fallback : Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return fallback;
            }
            finally
            {
                table?.Dispose();
            }
        }

        public string GetString(string tableName, string fieldName, string fallback)
        {
            if (luaEnv == null)
                return fallback;
            LuaTable table = null;
            try
            {
                table = luaEnv.Global.Get<LuaTable>(tableName);
                return table?.Get<string>(fieldName) ?? fallback;
            }
            catch (Exception)
            {
                return fallback;
            }
            finally
            {
                table?.Dispose();
            }
        }

        private bool TryComposeSources(
            string directory,
            IEnumerable<string> patchFiles,
            out Dictionary<string, string> sources,
            out string error)
        {
            sources = new Dictionary<string, string>(bundledSources, StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(directory))
            {
                error = "Lua 补丁目录不存在";
                return false;
            }

            try
            {
                if (patchFiles != null)
                {
                    foreach (string relativePath in patchFiles)
                    {
                        if (string.IsNullOrWhiteSpace(relativePath))
                            continue;
                        string normalized = relativePath.Replace('\\', '/');
                        string fileName = Path.GetFileName(normalized);
                        string fullPath = Path.Combine(directory, normalized.Replace('/', Path.DirectorySeparatorChar));
                        if (!File.Exists(fullPath))
                            throw new FileNotFoundException("Lua 补丁文件不存在", fullPath);
                        if (!sources.ContainsKey(fileName))
                            throw new InvalidDataException("未知 Lua 模块：" + fileName);
                        sources[fileName] = File.ReadAllText(fullPath);
                    }
                }

                foreach (ModuleSpec module in Modules)
                {
                    if (!sources.ContainsKey(module.FileName))
                        throw new InvalidDataException("缺少 Lua 模块：" + module.FileName);
                }

                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private bool TryReplaceEnvironment(IReadOnlyDictionary<string, string> sources, out string error)
        {
            LuaEnv nextEnvironment = null;
            try
            {
                nextEnvironment = new LuaEnv();
                for (int i = 0; i < Modules.Length - 1; i++)
                {
                    ModuleSpec module = Modules[i];
                    nextEnvironment.DoString(sources[module.FileName], module.FileName);
                }

                LuaEnv previous = luaEnv;
                if (previous != null)
                {
                    ClearHotfixCallbacks(previous);
                    previous.Dispose();
                    luaEnv = null;
                }

                ModuleSpec hotfixModule = Modules[Modules.Length - 1];
                nextEnvironment.DoString(sources[hotfixModule.FileName], hotfixModule.FileName);
                Debug.Log("HOTFIX_PROBE=" + HotfixAcceptanceProbe.GetMarker());

                luaEnv = nextEnvironment;
                nextEnvironment = null;
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                nextEnvironment?.Dispose();
            }
        }

        private void Update()
        {
            if (luaEnv == null)
                return;
            tickTimer += Time.unscaledDeltaTime;
            if (tickTimer < 1f)
                return;
            tickTimer = 0f;
            luaEnv.Tick();
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;
            if (luaEnv != null)
            {
                ClearHotfixCallbacks(luaEnv);
                luaEnv.Dispose();
            }
            luaEnv = null;
            instance = null;
        }

        private static void ClearHotfixCallbacks(LuaEnv environment)
        {
            LuaTable table = null;
            LuaFunction clear = null;
            try
            {
                table = environment.Global.Get<LuaTable>("HotfixMain");
                clear = table?.Get<LuaFunction>("Clear");
                clear?.Call();
                environment.Tick();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("清理 xLua 热修复回调失败：" + exception.Message);
            }
            finally
            {
                clear?.Dispose();
                table?.Dispose();
            }
        }
    }
}
