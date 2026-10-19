using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WoguaShooter.Editor
{
    /// <summary>
    /// 在 IL2CPP 转换前验证 xLua 已生成并完成程序集注入。
    /// </summary>
    internal sealed class WoguaHotfixBuildGuard : IPreprocessBuildWithReport, IPostBuildPlayerScriptDLLs
    {
        private const string HotfixDefine = "HOTFIX_ENABLE";
        private const string InjectedFieldName = "__Hotfix0_GetMarker";
        private const string GeneratedBridgePath = "Assets/XLua/Gen/DelegatesGensBridge.cs";
        private const string GeneratedLinkPath = "Assets/XLua/Gen/link.xml";
        private const string GenerationMarker = "WOGUA_HOTFIX_CONFIG_V1";
        private const string HotfixConfigPath = "Assets/Editor/WoguaHotfixConfig.cs";

        private static readonly string[] RequiredToolFiles =
        {
            "Tools/XLuaHotfixInject.exe",
            "Tools/Mono.Cecil.dll",
            "Tools/Mono.Cecil.Mdb.dll",
            "Tools/Mono.Cecil.Pdb.dll"
        };

        public int callbackOrder => 100;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android)
                ValidateAndroidPreparation();
        }

        public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
                return;

            string assemblyPath = report.GetFiles()
                .Select(file => file.path)
                .FirstOrDefault(path => path.EndsWith("Assembly-CSharp.dll", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(assemblyPath) || !File.Exists(assemblyPath))
                throw new BuildFailedException("无法找到待转换的 Assembly-CSharp.dll，不能确认 xLua 热修复注入结果。");

            if (!FileContains(assemblyPath, InjectedFieldName))
            {
                throw new BuildFailedException(
                    "xLua 热修复注入失败：Assembly-CSharp.dll 中没有找到 " + InjectedFieldName +
                    "。请检查 Tools/XLuaHotfixInject.exe 和 Unity Console 中的注入日志。");
            }

            Debug.Log("XLUA_HOTFIX_INJECTION_PASS: " + InjectedFieldName);
        }

        public static bool EnsureAndroidDefine()
        {
            NamedBuildTarget android = NamedBuildTarget.Android;
            string symbols = PlayerSettings.GetScriptingDefineSymbols(android);
            List<string> values = symbols.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (values.Contains(HotfixDefine))
                return false;

            values.Add(HotfixDefine);
            PlayerSettings.SetScriptingDefineSymbols(android, string.Join(";", values));
            AssetDatabase.SaveAssets();
            return true;
        }

        public static void ValidateToolFiles()
        {
            foreach (string path in RequiredToolFiles)
            {
                if (!File.Exists(Path.GetFullPath(path)))
                    throw new BuildFailedException("缺少 xLua 热修复工具文件：" + path);
            }

            string projectPath = Path.GetFullPath(".");
            if (projectPath.Any(character => character > 127))
                throw new BuildFailedException("xLua 注入器要求项目路径只包含 ASCII 字符：" + projectPath);
        }

        public static void ValidateAndroidPreparation()
        {
            ValidateToolFiles();

            string symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
            bool defineEnabled = symbols.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(value => string.Equals(value.Trim(), HotfixDefine, StringComparison.Ordinal));
            if (!defineEnabled)
                throw new BuildFailedException("Android 未启用 HOTFIX_ENABLE。请先执行 Prepare xLua Hotfix。");

#if !HOTFIX_ENABLE
            throw new BuildFailedException(
                "当前编辑器脚本尚未使用 HOTFIX_ENABLE 编译。请等待 Unity 编译完成后重新执行构建。");
#endif

            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
                throw new BuildFailedException("Android 必须使用 IL2CPP 才能执行本项目的热修复发布校验。");
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
                throw new BuildFailedException("Android 构建必须包含 ARM64 架构。");

            if (EditorApplication.isCompiling)
                throw new BuildFailedException("Unity 正在编译脚本，请等待编译完成后再构建。");
            if (!File.Exists(GeneratedBridgePath) ||
                !File.Exists(WoguaHotfixConfig.GenerationMarkerPath) ||
                !FileContains(WoguaHotfixConfig.GenerationMarkerPath, GenerationMarker) ||
                !FileContains(WoguaHotfixConfig.GenerationMarkerPath,
                    "WoguaShooter.Diagnostics.HotfixAcceptanceProbe"))
            {
                throw new BuildFailedException("xLua 热修复桥接代码尚未按项目白名单生成。请先执行 Prepare xLua Hotfix。");
            }
            if (!File.Exists(GeneratedLinkPath) || !FileContains(GeneratedLinkPath, "WoguaShooter.Core.WeaponController"))
                throw new BuildFailedException("xLua link.xml 未包含项目业务类型。请重新执行 Prepare xLua Hotfix。");

            DateTime generatedAt = File.GetLastWriteTimeUtc(GeneratedBridgePath);
            foreach (string inputPath in GetGenerationInputs())
            {
                if (File.GetLastWriteTimeUtc(inputPath) > generatedAt)
                {
                    throw new BuildFailedException(
                        "xLua 生成代码早于业务脚本，可能已经过期：" + inputPath +
                        "。请重新执行 Prepare xLua Hotfix。");
                }
            }
        }

        private static IEnumerable<string> GetGenerationInputs()
        {
            yield return HotfixConfigPath;
            foreach (string directory in new[]
                     {
                         "Assets/Scripts/Core",
                         "Assets/Scripts/Diagnostics",
                         "Assets/Scripts/UI"
                     })
            {
                foreach (string path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
                    yield return path;
            }
        }

        private static bool FileContains(string path, string expectedText)
        {
            byte[] content = File.ReadAllBytes(path);
            byte[] expected = Encoding.UTF8.GetBytes(expectedText);
            if (expected.Length == 0 || content.Length < expected.Length)
                return false;

            for (int start = 0; start <= content.Length - expected.Length; start++)
            {
                int offset = 0;
                while (offset < expected.Length && content[start + offset] == expected[offset])
                    offset++;
                if (offset == expected.Length)
                    return true;
            }

            return false;
        }
    }
}
