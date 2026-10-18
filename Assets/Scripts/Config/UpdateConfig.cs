using UnityEngine;

namespace WoguaShooter.Config
{
    [CreateAssetMenu(fileName = "UpdateConfig", menuName = "Wogua Shooter/Update Config")]
    public class UpdateConfig : ScriptableObject
    {
        [Tooltip("更新清单的公网 HTTPS 地址。")]
        public string manifestUrl = "https://update.xinyin.space/version.json";

        [Min(1)] public int timeoutSeconds = 5;
        public bool checkOnStart = true;
    }
}
