using System;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

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
            int levelId,
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
                minInterval,
                levelId);
            return Mathf.Max(0.2f, value);
        }

        public float GetHazardSpawnInterval(Vector2 configuredRange, int levelId)
        {
            float minimum = configuredRange.x > 0f ? configuredRange.x : 5.5f;
            float maximum = configuredRange.y >= minimum ? configuredRange.y : 8f;
            float fallback = Random.Range(minimum, maximum);
            return Mathf.Max(1f, CallSpawnRule(
                "GetHazardSpawnInterval",
                fallback,
                Random.value,
                levelId,
                minimum,
                maximum));
        }

        public float GetSpikeChance(int levelId)
        {
            return Mathf.Clamp01(CallSpawnRule("GetSpikeChance", 0.58f, levelId));
        }

        public int GetSpikeGroupCount(int levelId)
        {
            float randomValue = Random.value;
            int fallback = randomValue < 0.5f ? 1 : randomValue < 0.82f ? 2 : 3;
            return Mathf.Clamp(
                Mathf.RoundToInt(CallSpawnRule(
                    "GetSpikeGroupCount",
                    fallback,
                    randomValue,
                    levelId)),
                1,
                3);
        }

        public float GetMissileInitialDelay(int levelId)
        {
            float fallback = Random.Range(4f, 7f);
            return Mathf.Max(0.5f, CallSpawnRule(
                "GetMissileInitialDelay",
                fallback,
                Random.value,
                levelId));
        }

        public float GetMissileSpawnInterval(int levelId)
        {
            float fallback = Random.Range(9f, 13f);
            return Mathf.Max(1f, CallSpawnRule(
                "GetMissileSpawnInterval",
                fallback,
                Random.value,
                levelId));
        }

        public float GetMissileVolleyDelay(int levelId)
        {
            float fallback = Random.Range(0.55f, 0.9f);
            return Mathf.Max(0.1f, CallSpawnRule(
                "GetMissileVolleyDelay",
                fallback,
                Random.value,
                levelId));
        }

        public int GetMissileVolleyCount(int levelId)
        {
            float randomValue = Random.value;
            int fallback = randomValue < 0.5f ? 1 : randomValue < 0.82f ? 2 : 3;
            return Mathf.Clamp(
                Mathf.RoundToInt(CallSpawnRule(
                    "GetMissileVolleyCount",
                    fallback,
                    randomValue,
                    levelId)),
                1,
                3);
        }

        private static float CallSpawnRule(string functionName, float fallback, params object[] arguments)
        {
            LuaHotUpdateRuntime runtime = LuaHotUpdateRuntime.Instance;
            return runtime != null
                ? runtime.CallFloat("SpawnRules", functionName, fallback, arguments)
                : fallback;
        }
    }
}
