using UnityEngine;

namespace WoguaShooter.Config
{
    [CreateAssetMenu(fileName = "UpdateConfig", menuName = "Wogua Shooter/Update Config")]
    public class UpdateConfig : ScriptableObject
    {
        [Tooltip("手机需要填写本机局域网 IP，不能使用 localhost。")]
        public string manifestUrl = "http://192.168.110.141:8000/version.json";

        [Min(1)] public int timeoutSeconds = 1;
        public bool checkOnStart = true;
    }
}
