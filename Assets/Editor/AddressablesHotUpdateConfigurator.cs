using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace WoguaShooter.Editor
{
    public static class AddressablesHotUpdateConfigurator
    {
        public const string ProfileName = "stable-v2";
        public const string PreloadLabel = "hot-update-preload";

        private const string BootstrapGroupName = "Bootstrap Local";
        private const string GameplayGroupName = "Base Gameplay";
        private const string UiGroupName = "Base UI";
        private const string LuaGroupName = "Base Lua";
        private const string RemoteGroupName = "Remote Content";

        private const string RemoteBuildPath = "Server/Staging/[BuildTarget]";
        private const string RemoteLoadPath = "https://update.xinyin.space/addressables/[BuildTarget]";

        private static readonly string[] BootstrapAddresses =
        {
            "ui/update",
            "config/update"
        };

        private static readonly string[] GameplayAddresses =
        {
            "game/root",
            "character/player",
            "map/ground",
            "config/game"
        };

        private static readonly string[] UiAddresses =
        {
            "ui/main_menu",
            "ui/hud",
            "ui/settings",
            "ui/volume",
            "ui/pause",
            "ui/level_select",
            "ui/game_over",
            "ui/spells/machine_gun",
            "ui/spells/corn_cannon",
            "ui/spells/triple_shot",
            "ui/spells/dual_muzzle",
            "ui/spells/repeater",
            "ui/spells/wave_flight"
        };

        private static readonly string[] LuaAddresses =
        {
            "lua/game_rules"
        };

        private static readonly (string AssetPath, string Address)[] OptionalLuaModules =
        {
            ("Assets/Lua/config.lua.txt", "lua/config"),
            ("Assets/Lua/player_rules.lua.txt", "lua/player_rules"),
            ("Assets/Lua/weapon_rules.lua.txt", "lua/weapon_rules"),
            ("Assets/Lua/spawn_rules.lua.txt", "lua/spawn_rules"),
            ("Assets/Lua/combat_rules.lua.txt", "lua/combat_rules"),
            ("Assets/Lua/progression_rules.lua.txt", "lua/progression_rules"),
            ("Assets/Lua/tutorial_rules.lua.txt", "lua/tutorial_rules"),
            ("Assets/Lua/flow_rules.lua.txt", "lua/flow_rules"),
            ("Assets/Lua/hotfix_main.lua.txt", "lua/hotfix_main")
        };

        [MenuItem("Tools/Wogua Shooter/Hot Update/Configure Addressables")]
        public static void Configure()
        {
            AddressableAssetSettings settings = RequireSettings();
            ConfigureProfile(settings);

            AddressableAssetGroup bootstrap = GetOrCreateGroup(settings, BootstrapGroupName);
            AddressableAssetGroup gameplay = GetOrCreateGroup(settings, GameplayGroupName);
            AddressableAssetGroup ui = GetOrCreateGroup(settings, UiGroupName);
            AddressableAssetGroup lua = GetOrCreateGroup(settings, LuaGroupName);
            AddressableAssetGroup remote = GetOrCreateGroup(settings, RemoteGroupName);

            ConfigureLocalGroup(settings, bootstrap, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            ConfigureLocalGroup(settings, gameplay, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            ConfigureLocalGroup(settings, ui, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            ConfigureLocalGroup(settings, lua, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            ConfigureRemoteGroup(settings, remote);

            settings.AddLabel(PreloadLabel, false);
            MoveEntries(settings, bootstrap, BootstrapAddresses, false);
            MoveEntries(settings, gameplay, GameplayAddresses, true);
            MoveEntries(settings, ui, UiAddresses, true);
            MoveEntries(settings, lua, LuaAddresses, true);
            AddOptionalLuaModules(settings, lua);

            // New ordinary entries should start in the offline-capable gameplay group.
            settings.DefaultGroup = gameplay;
            settings.BuildRemoteCatalog = true;
            settings.DisableCatalogUpdateOnStartup = true;
            settings.UniqueBundleIds = true;
            settings.CatalogRequestsTimeout = 10;
            settings.BundleRetryCount = 3;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateInternal(settings, true);
            Debug.Log("Addressables 热更新配置完成。首包资源仍保存在本地组，Remote Content 仅用于后续远端内容。");
        }

        [MenuItem("Tools/Wogua Shooter/Hot Update/Validate Addressables")]
        public static void Validate()
        {
            ValidateInternal(RequireSettings(), true);
        }

        private static AddressableAssetSettings RequireSettings()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                throw new InvalidOperationException("项目中没有 AddressableAssetSettings，请先创建 Addressables 设置。");
            return settings;
        }

        private static void ConfigureProfile(AddressableAssetSettings settings)
        {
            string profileId = settings.profileSettings.GetProfileId(ProfileName);
            if (string.IsNullOrEmpty(profileId))
                profileId = settings.profileSettings.AddProfile(ProfileName, settings.activeProfileId);

            settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kLocalBuildPath,
                AddressableAssetSettings.kLocalBuildPathValue);
            settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kLocalLoadPath,
                AddressableAssetSettings.kLocalLoadPathValue);
            settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteBuildPath, RemoteBuildPath);
            settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteLoadPath, RemoteLoadPath);
            settings.activeProfileId = profileId;
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group != null)
                return group;

            return settings.CreateGroup(
                groupName,
                false,
                false,
                false,
                null,
                typeof(ContentUpdateGroupSchema),
                typeof(BundledAssetGroupSchema));
        }

        private static void ConfigureLocalGroup(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            BundledAssetGroupSchema.BundlePackingMode bundleMode)
        {
            BundledAssetGroupSchema bundled = GetOrAddSchema<BundledAssetGroupSchema>(group);
            bundled.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            bundled.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            bundled.BundleMode = bundleMode;
            bundled.UseAssetBundleCrc = true;
            bundled.UseAssetBundleCrcForCachedBundles = true;
            bundled.UseAssetBundleCache = true;
            bundled.RetryCount = 0;

            // These assets ship inside the APK. Content Update must move changed entries
            // into a remote group instead of replacing an unavailable local bundle.
            GetOrAddSchema<ContentUpdateGroupSchema>(group).StaticContent = true;
        }

        private static void ConfigureRemoteGroup(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            BundledAssetGroupSchema bundled = GetOrAddSchema<BundledAssetGroupSchema>(group);
            bundled.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            bundled.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            bundled.UseAssetBundleCache = true;
            bundled.UseAssetBundleCrc = true;
            bundled.UseAssetBundleCrcForCachedBundles = true;
            bundled.RetryCount = 3;
            bundled.Timeout = 15;

            GetOrAddSchema<ContentUpdateGroupSchema>(group).StaticContent = false;
        }

        private static T GetOrAddSchema<T>(AddressableAssetGroup group)
            where T : AddressableAssetGroupSchema
        {
            T schema = group.GetSchema<T>();
            return schema != null ? schema : group.AddSchema<T>();
        }

        private static void MoveEntries(
            AddressableAssetSettings settings,
            AddressableAssetGroup targetGroup,
            IEnumerable<string> addresses,
            bool preload)
        {
            foreach (string address in addresses)
            {
                AddressableAssetEntry existing = FindEntryByAddress(settings, address);
                if (existing == null)
                {
                    Debug.LogWarning($"Addressables 配置跳过不存在的地址：{address}");
                    continue;
                }

                AddressableAssetEntry entry = settings.CreateOrMoveEntry(existing.guid, targetGroup, false, false);
                entry.address = address;
                entry.SetLabel(PreloadLabel, preload, false, false);
            }
        }

        private static AddressableAssetEntry FindEntryByAddress(AddressableAssetSettings settings, string address)
        {
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;

                AddressableAssetEntry entry = group.entries.FirstOrDefault(item => item.address == address);
                if (entry != null)
                    return entry;
            }

            return null;
        }

        private static void AddOptionalLuaModules(AddressableAssetSettings settings, AddressableAssetGroup luaGroup)
        {
            foreach ((string assetPath, string address) in OptionalLuaModules)
            {
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                    continue;

                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, luaGroup, false, false);
                entry.address = address;
                entry.SetLabel(PreloadLabel, true, false, false);
            }
        }

        private static void ValidateInternal(AddressableAssetSettings settings, bool throwOnFailure)
        {
            List<string> errors = new List<string>();
            string profileId = settings.profileSettings.GetProfileId(ProfileName);

            Check(!string.IsNullOrEmpty(profileId), $"缺少 {ProfileName} Profile", errors);
            Check(settings.activeProfileId == profileId, $"当前 Profile 不是 {ProfileName}", errors);
            if (!string.IsNullOrEmpty(profileId))
            {
                Check(settings.profileSettings.GetValueByName(profileId, AddressableAssetSettings.kRemoteBuildPath) == RemoteBuildPath,
                    "Remote.BuildPath 配置错误", errors);
                Check(settings.profileSettings.GetValueByName(profileId, AddressableAssetSettings.kRemoteLoadPath) == RemoteLoadPath,
                    "Remote.LoadPath 配置错误", errors);
            }

            Check(settings.BuildRemoteCatalog, "未开启远端 Catalog", errors);
            Check(settings.DisableCatalogUpdateOnStartup, "必须关闭 Addressables 启动时自动更新 Catalog", errors);
            Check(settings.UniqueBundleIds, "未开启 Unique Bundle IDs", errors);

            ValidateGroup(settings, BootstrapGroupName, false, BootstrapAddresses, false, errors);
            ValidateGroup(settings, GameplayGroupName, false, GameplayAddresses, true, errors);
            ValidateGroup(settings, UiGroupName, false, UiAddresses, true, errors);
            ValidateGroup(settings, LuaGroupName, false, LuaAddresses, true, errors);
            ValidateGroup(settings, RemoteGroupName, true, Array.Empty<string>(), false, errors);
            ValidateOptionalLuaModules(settings, errors);

            if (errors.Count == 0)
            {
                Debug.Log("ADDRESSABLES_HOT_UPDATE_CONFIG_PASS：分组、Profile、远端 Catalog 和首包离线资源检查通过。");
                return;
            }

            string message = "Addressables 热更新配置检查失败：\n- " + string.Join("\n- ", errors);
            Debug.LogError(message);
            if (throwOnFailure)
                throw new InvalidOperationException(message);
        }

        private static void ValidateGroup(
            AddressableAssetSettings settings,
            string groupName,
            bool remote,
            IEnumerable<string> expectedAddresses,
            bool shouldPreload,
            ICollection<string> errors)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null)
            {
                errors.Add($"缺少分组 {groupName}");
                return;
            }

            BundledAssetGroupSchema bundled = group.GetSchema<BundledAssetGroupSchema>();
            ContentUpdateGroupSchema content = group.GetSchema<ContentUpdateGroupSchema>();
            if (bundled == null || content == null)
            {
                errors.Add($"分组 {groupName} 缺少必要 Schema");
                return;
            }

            string expectedBuildPath = remote ? RemoteBuildPath : AddressableAssetSettings.kLocalBuildPathValue;
            string expectedLoadPath = remote ? RemoteLoadPath : AddressableAssetSettings.kLocalLoadPathValue;
            Check(bundled.BuildPath.GetValue(settings) == settings.profileSettings.EvaluateString(settings.activeProfileId, expectedBuildPath),
                $"分组 {groupName} 的 BuildPath 配置错误", errors);
            Check(bundled.LoadPath.GetValue(settings) == settings.profileSettings.EvaluateString(settings.activeProfileId, expectedLoadPath),
                $"分组 {groupName} 的 LoadPath 配置错误", errors);
            Check(bundled.UseAssetBundleCrc, $"分组 {groupName} 未开启 CRC", errors);
            Check(bundled.UseAssetBundleCache, $"分组 {groupName} 未开启缓存", errors);

            if (remote)
            {
                Check(bundled.RetryCount == 3, $"分组 {groupName} 的重试次数不是 3", errors);
                Check(!content.StaticContent, $"分组 {groupName} 不应标记为静态内容", errors);
            }
            else
            {
                Check(content.StaticContent, $"分组 {groupName} 必须标记 Prevent Updates", errors);
            }

            foreach (string address in expectedAddresses)
            {
                AddressableAssetEntry entry = FindEntryByAddress(settings, address);
                if (entry == null)
                {
                    errors.Add($"缺少地址 {address}");
                    continue;
                }

                Check(entry.parentGroup == group, $"地址 {address} 不在 {groupName}", errors);
                Check(entry.labels.Contains(PreloadLabel) == shouldPreload,
                    $"地址 {address} 的 {PreloadLabel} 标签状态错误", errors);
            }
        }

        private static void Check(bool condition, string message, ICollection<string> errors)
        {
            if (!condition)
                errors.Add(message);
        }

        private static void ValidateOptionalLuaModules(AddressableAssetSettings settings, ICollection<string> errors)
        {
            AddressableAssetGroup luaGroup = settings.FindGroup(LuaGroupName);
            if (luaGroup == null)
                return;

            foreach ((string assetPath, string address) in OptionalLuaModules)
            {
                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
                    continue;

                AddressableAssetEntry entry = FindEntryByAddress(settings, address);
                Check(entry != null, $"Lua 模块未加入 Addressables：{assetPath}", errors);
                if (entry == null)
                    continue;

                Check(entry.parentGroup == luaGroup, $"地址 {address} 不在 {LuaGroupName}", errors);
                Check(entry.labels.Contains(PreloadLabel), $"地址 {address} 缺少 {PreloadLabel} 标签", errors);
            }
        }
    }
}
