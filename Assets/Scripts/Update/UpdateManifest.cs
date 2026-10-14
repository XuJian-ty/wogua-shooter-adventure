using System;

namespace WoguaShooter.Update
{
    [Serializable]
    public class UpdateManifest
    {
        public string version;
        public string title;
        public string message;
        public string luaUrl;
        public string catalogUrl;
    }
}
