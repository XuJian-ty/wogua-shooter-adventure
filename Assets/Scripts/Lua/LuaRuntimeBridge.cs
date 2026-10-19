using System;
using System.IO;
using UnityEngine;

namespace WoguaShooter.Lua
{
    /// <summary>供场景业务组件调用常驻 Lua 规则的兼容桥。</summary>
    public class LuaRuntimeBridge : MonoBehaviour
    {
        [SerializeField] private string rulesAddress = "lua/spawn_rules";

        public bool IsLoaded => LuaHotUpdateRuntime.Instance != null && LuaHotUpdateRuntime.Instance.IsReady;

        public bool ValidateRules(string source, out string error)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                error = "Lua 文件为空";
                return false;
            }

            try
            {
                using (XLua.LuaEnv environment = new XLua.LuaEnv())
                using (XLua.LuaFunction function = environment.LoadString(source, "legacy_rules_validation"))
                {
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

        public bool ReloadFromFile(string path, out string error)
        {
            if (!File.Exists(path))
            {
                error = "补丁文件不存在";
                return false;
            }

            error = "旧版单文件补丁已停用，请使用 stable-v2 多模块更新清单";
            return false;
        }

        public float GetEnemySpawnInterval(
            float elapsedTime,
            float startInterval,
            float acceleration,
            float minInterval,
            float fallback)
        {
            LuaHotUpdateRuntime runtime = LuaHotUpdateRuntime.Instance;
            if (runtime == null)
                return fallback;
            float value = runtime.CallFloat(
                "SpawnRules",
                "GetEnemySpawnInterval",
                fallback,
                elapsedTime,
                startInterval,
                acceleration,
                minInterval);
            return Mathf.Max(0.2f, value);
        }
    }
}
