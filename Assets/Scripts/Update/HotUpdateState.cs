namespace WoguaShooter.Update
{
    /// <summary>启动更新层与旧 GameRoot 组件之间的轻量状态桥。</summary>
    public static class HotUpdateState
    {
        public static bool StartupCompleted { get; internal set; }
        public static string ContentVersion { get; internal set; }
    }
}
