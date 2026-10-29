using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using WoguaShooter.Core;
using WoguaShooter.Diagnostics;
using XLua;

namespace WoguaShooter.Editor
{
    public static class HotUpdatePatchVerifier
    {
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

        [MenuItem("Tools/Wogua Shooter/Hot Update/Verify Current Lua Patch")]
        public static void Run()
        {
            LuaEnv environment = new LuaEnv();
            GameObject playerObject = null;
            GameObject backgroundObject = null;
            GameObject spawnerObject = null;
            try
            {
                LoadPatch(environment);
                Require(
                    HotfixAcceptanceProbe.GetMarker() == "PATCH_1.0.1",
                    "xLua 方法替换标记不正确");

                backgroundObject = new GameObject("HotUpdatePatchVerifierBackground");
                BackgroundScroller background = backgroundObject.AddComponent<BackgroundScroller>();
                Require(
                    (string)GetField(background, "levelTwoBackgroundAddress").GetValue(background) ==
                    "map/level2/background/1.0.1",
                    "第2关背景没有切换到 1.0.1 独立资源地址");

                playerObject = new GameObject("HotUpdatePatchVerifierPlayer");
                playerObject.AddComponent<Rigidbody2D>();
                playerObject.AddComponent<CapsuleCollider2D>();
                SpriteRenderer renderer = playerObject.AddComponent<SpriteRenderer>();
                PlayerController player = playerObject.AddComponent<PlayerController>();

                SetField(player, "shieldTime", 0.05f);
                SetField(player, "damageInvincibleTime", 0f);
                SetField(player, "growthTime", 0f);
                InvokePrivate(player, "TickPowerUps", 0.1f);
                Require(GetFloat(player, "damageInvincibleTime") >= 2.99f, "护盾自然到期没有获得 3 秒无敌");

                SetField(player, "magnetTime", 1f);
                Require(
                    player.CanMagnetAttract(player.transform.position + Vector3.right * 2.5f),
                    "磁铁无法吸附前方 2.5 米目标");
                Require(
                    !player.CanMagnetAttract(player.transform.position + Vector3.right * 3.1f),
                    "磁铁错误吸附了前方 3.1 米目标");

                SetField(player, "damageInvincibleTime", 0f);
                SetField(player, "growthTime", 2.5f);
                SetField(player, "playerVisualRenderers", new[] { renderer });
                SetField(player, "playerVisualBaseColors", new[] { Color.white });
                InvokePrivate(player, "UpdateDamageInvincibilityVisual");
                Require(renderer.color.a < 0.75f, "变大效果最后 3 秒没有显示半透明闪烁");

                float hazardInterval = CallRuleFloat(
                    environment,
                    "GetHazardSpawnInterval",
                    0.5f,
                    1,
                    7f,
                    11f);
                Require(hazardInterval >= 5.5f && hazardInterval <= 8f, "地形障碍刷新间隔未更新");

                float levelOneEnemyInterval = CallRuleFloat(
                    environment,
                    "GetEnemySpawnInterval",
                    0f,
                    2.4f,
                    0.01f,
                    0.8f,
                    1);
                float levelTwoEnemyInterval = CallRuleFloat(
                    environment,
                    "GetEnemySpawnInterval",
                    0f,
                    2.4f,
                    0.01f,
                    0.8f,
                    2);
                Require(levelTwoEnemyInterval < levelOneEnemyInterval, "第2关敌人刷新节奏未加快");

                float levelTwoHazardInterval = CallRuleFloat(
                    environment,
                    "GetHazardSpawnInterval",
                    0.5f,
                    2,
                    7f,
                    11f);
                Require(levelTwoHazardInterval < hazardInterval, "第2关障碍刷新节奏未加快");

                Require(CallRuleInt(environment, "GetSpikeGroupCount", 0.1f) == 1,
                    "地刺最小段数错误");
                Require(CallRuleInt(environment, "GetSpikeGroupCount", 0.65f) == 2,
                    "地刺双段规则错误");
                Require(CallRuleInt(environment, "GetSpikeGroupCount", 0.9f) == 3,
                    "地刺三段规则错误");
                Require(CallRuleInt(environment, "GetMissileVolleyCount", 0.65f) == 2,
                    "导弹双发规则错误");
                Require(CallRuleInt(environment, "GetMissileVolleyCount", 0.9f) == 3,
                    "导弹三发规则错误");
                Require(CallRuleInt(environment, "GetMissileVolleyCount", 0.45f, 2) == 2,
                    "第2关导弹批次强度未提高");

                Debug.Log("HOT_UPDATE_PATCH_TEST_PASS version=1.0.1 level2=twilight-wasteland difficulty=raised");
            }
            finally
            {
                if (spawnerObject != null)
                    UnityEngine.Object.DestroyImmediate(spawnerObject);
                if (backgroundObject != null)
                    UnityEngine.Object.DestroyImmediate(backgroundObject);
                if (playerObject != null)
                    UnityEngine.Object.DestroyImmediate(playerObject);
                ClearPatch(environment);
                environment.Dispose();
            }
        }

        private static void LoadPatch(LuaEnv environment)
        {
            string directory = Path.GetFullPath("Assets/Lua");
            for (int i = 0; i < LuaModuleFiles.Length; i++)
            {
                string fileName = LuaModuleFiles[i];
                environment.DoString(File.ReadAllText(Path.Combine(directory, fileName)), fileName);
            }
        }

        private static void ClearPatch(LuaEnv environment)
        {
            environment.DoString(@"
                xlua.hotfix(CS.WoguaShooter.Core.BackgroundScroller, 'Awake', nil)
                xlua.hotfix(CS.WoguaShooter.Core.PlayerController, 'TickPowerUps', nil)
                xlua.hotfix(CS.WoguaShooter.Core.PlayerController, 'UpdateDamageInvincibilityVisual', nil)
                xlua.hotfix(CS.WoguaShooter.Core.PlayerController, 'CanMagnetAttract', nil)
                xlua.hotfix(CS.WoguaShooter.Diagnostics.HotfixAcceptanceProbe, 'GetMarker', nil)
                collectgarbage('collect')
            ", "clear_hot_update_patch");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            environment.Tick();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = GetField(target, fieldName);
            field.SetValue(target, value);
        }

        private static float GetFloat(object target, string fieldName)
        {
            return (float)GetField(target, fieldName).GetValue(target);
        }

        private static int CallRuleInt(LuaEnv environment, string functionName, params object[] arguments)
        {
            using (LuaTable table = environment.Global.Get<LuaTable>("SpawnRules"))
            using (LuaFunction function = table.Get<LuaFunction>(functionName))
            {
                object[] result = function.Call(arguments);
                return Convert.ToInt32(result[0]);
            }
        }

        private static float CallRuleFloat(
            LuaEnv environment,
            string functionName,
            params object[] arguments)
        {
            using (LuaTable table = environment.Global.Get<LuaTable>("SpawnRules"))
            using (LuaFunction function = table.Get<LuaFunction>(functionName))
            {
                object[] result = function.Call(arguments);
                return Convert.ToSingle(result[0]);
            }
        }

        private static FieldInfo GetField(object target, string fieldName)
        {
            return target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(target.GetType().FullName, fieldName);
        }

        private static object InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(target.GetType().FullName, methodName);
            return method.Invoke(target, arguments);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
