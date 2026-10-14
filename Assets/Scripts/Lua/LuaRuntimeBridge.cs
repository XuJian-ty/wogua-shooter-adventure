using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using XLua;

namespace WoguaShooter.Lua
{
    public class LuaRuntimeBridge : MonoBehaviour
    {
        [SerializeField] private string rulesAddress = "lua/game_rules";

        private LuaEnv luaEnv;
        private LuaTable rules;
        private LuaFunction getSpawnInterval;
        private AsyncOperationHandle<TextAsset> rulesHandle;
        private bool hasRulesHandle;

        public bool IsLoaded => getSpawnInterval != null;

        private void Awake()
        {
            luaEnv = new LuaEnv();
        }

        private IEnumerator Start()
        {
            rulesHandle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<TextAsset>(rulesAddress);
            hasRulesHandle = true;
            yield return rulesHandle;

            if (rulesHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Lua 业务脚本加载失败：{rulesAddress}");
                yield break;
            }

            string patchPath = Path.Combine(Application.persistentDataPath, "HotUpdate", "game_rules.lua.txt");
            if (File.Exists(patchPath))
            {
                string patchSource = File.ReadAllText(patchPath);
                if (TryReload(patchSource, "game_rules_patch", out string patchError))
                    yield break;

                Debug.LogWarning("本地 Lua 补丁无效，已回退到包内版本：" + patchError);
                File.Delete(patchPath);
            }

            if (!TryReload(rulesHandle.Result.text, "game_rules", out string bundledError))
                Debug.LogError("包内 Lua 业务脚本无效：" + bundledError);
        }

        public bool ValidateRules(string source, out string error)
        {
            LuaEnv validationEnv = null;
            LuaTable validationRules = null;
            LuaFunction validationFunction = null;
            try
            {
                validationEnv = new LuaEnv();
                validationEnv.DoString(source, "game_rules_validation");
                validationRules = validationEnv.Global.Get<LuaTable>("GameRules");
                validationFunction = validationRules?.Get<LuaFunction>("GetEnemySpawnInterval");
                if (validationFunction == null)
                    throw new InvalidDataException("缺少 GameRules.GetEnemySpawnInterval 函数");

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
                validationFunction?.Dispose();
                validationRules?.Dispose();
                validationEnv?.Dispose();
            }
        }

        public bool ReloadFromFile(string path, out string error)
        {
            if (!File.Exists(path))
            {
                error = "补丁文件不存在";
                return false;
            }

            return TryReload(File.ReadAllText(path), "game_rules_patch", out error);
        }

        private bool TryReload(string source, string chunkName, out string error)
        {
            if (!ValidateRules(source, out error))
                return false;

            try
            {
                ExecuteRules(source, chunkName);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private void ExecuteRules(string source, string chunkName)
        {
            getSpawnInterval?.Dispose();
            rules?.Dispose();
            getSpawnInterval = null;
            rules = null;
            luaEnv.DoString("GameRules = nil");
            luaEnv.DoString(source, chunkName);
            rules = luaEnv.Global.Get<LuaTable>("GameRules");
            getSpawnInterval = rules?.Get<LuaFunction>("GetEnemySpawnInterval");
        }

        public float GetEnemySpawnInterval(float elapsedTime, float startInterval, float acceleration, float minInterval, float fallback)
        {
            if (getSpawnInterval == null)
                return fallback;

            object[] result = getSpawnInterval.Call(elapsedTime, startInterval, acceleration, minInterval);
            if (result != null && result.Length > 0 && result[0] is double value)
                return Mathf.Max(0.2f, (float)value);
            if (result != null && result.Length > 0 && result[0] is float floatValue)
                return Mathf.Max(0.2f, floatValue);
            return fallback;
        }

        private void OnDestroy()
        {
            getSpawnInterval?.Dispose();
            rules?.Dispose();
            luaEnv?.Dispose();
            if (hasRulesHandle && rulesHandle.IsValid())
                UnityEngine.AddressableAssets.Addressables.Release(rulesHandle);
        }
    }
}
