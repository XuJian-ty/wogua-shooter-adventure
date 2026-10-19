namespace WoguaShooter.Diagnostics
{
    /// <summary>用于确认 C# 方法确实经过 xLua Hotfix 注入，而非仅加载了 Lua 文件。</summary>
    public static class HotfixAcceptanceProbe
    {
        public static string GetMarker()
        {
            return "BASE";
        }
    }
}
