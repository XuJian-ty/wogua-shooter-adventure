using UnityEngine;

namespace WoguaShooter.Config
{
    [CreateAssetMenu(fileName = "UpdateConfig", menuName = "Wogua Shooter/Update Config")]
    public class UpdateConfig : ScriptableObject
    {
        [Tooltip("更新清单的公网 HTTPS 地址。")]
        public string manifestUrl = "https://update.xinyin.space/channels/stable-v2/android.json";

        [Min(1)] public int timeoutSeconds = 60;
        public bool checkOnStart = true;

        [Header("本地缓存")]
        [Min(1)]
        [Tooltip("修改此值会清理旧的热更新缓存，不会影响游戏存档。")]
        public int cacheGeneration = 1;

        [Header("Addressables")]
        [Tooltip("更新后需要预下载的 Addressables 标签。")]
        public string preloadLabel = "hot-update-preload";

        [Header("启动资源地址")]
        public string updatePanelAddress = "ui/update";
        public string gameRootAddress = "game/root";
    }
}
