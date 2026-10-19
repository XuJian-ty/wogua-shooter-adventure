using System;
using System.Collections.Generic;
using System.IO;
using CSObjectWrapEditor;
using WoguaShooter.Core;
using WoguaShooter.Diagnostics;
using WoguaShooter.UI;
using XLua;

namespace WoguaShooter.Editor
{
    /// <summary>
    /// 可热修复的业务层白名单。启动、更新下载和 Lua 运行时属于可信底层，不放入此列表。
    /// </summary>
    public static class WoguaHotfixConfig
    {
        public const string GenerationMarkerPath = "Assets/XLua/Gen/wogua_hotfix_generation.txt";

        private static readonly List<Type> BusinessTypes = new List<Type>
        {
            typeof(AudioManager),
            typeof(BackgroundScroller),
            typeof(CoinCollectible),
            typeof(CollectibleWeapon),
            typeof(CombatVfx),
            typeof(Enemy),
            typeof(FireParticleVfx),
            typeof(GameManager),
            typeof(GameplaySpriteLibrary),
            typeof(HotfixAcceptanceProbe),
            typeof(PlayerController),
            typeof(Projectile),
            typeof(RuntimeEffectSprites),
            typeof(SpawnManager),
            typeof(SpellInventory),
            typeof(WeaponController),
            typeof(WorldHazard),
            typeof(FlowUIController),
            typeof(LeaderboardService),
            typeof(LeaderboardUIController),
            typeof(LoadingOverlayController),
            typeof(SpellIconFactory),
            typeof(SpellSlotView),
            typeof(TutorialGradientTextEffect),
            typeof(TutorialGuideController),
            typeof(UIButtonMotion),
            typeof(UIController)
        };

        [Hotfix(HotfixFlag.IgnoreCompilerGenerated)]
        public static IEnumerable<Type> HotfixTypes => BusinessTypes;

        [LuaCallCSharp]
        public static IEnumerable<Type> LuaCallCSharpTypes => BusinessTypes;

        [GenCodeMenu]
        public static void WriteGenerationMarker()
        {
            if (Generator.HotfixCfg == null || !Generator.HotfixCfg.ContainsKey(typeof(HotfixAcceptanceProbe)))
                throw new InvalidOperationException("xLua 生成配置未包含 HotfixAcceptanceProbe。");

            File.WriteAllLines(GenerationMarkerPath, new[]
            {
                "WOGUA_HOTFIX_CONFIG_V1",
                typeof(HotfixAcceptanceProbe).FullName
            });
        }
    }
}
