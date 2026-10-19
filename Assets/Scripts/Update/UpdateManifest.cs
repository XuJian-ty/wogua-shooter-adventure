using System;

namespace WoguaShooter.Update
{
    [Serializable]
    public class UpdateManifest
    {
        public int schemaVersion = 2;
        public string contentVersion;
        public string minClientVersion;
        public string version;
        public string title;
        public string message;
        public string luaUrl;
        public string catalogUrl;
        public string catalogSha256;
        public string preloadLabel;
        public LuaPatchFile[] luaFiles;

        public string ResolvedVersion => string.IsNullOrWhiteSpace(contentVersion) ? version : contentVersion;
    }

    [Serializable]
    public class LuaPatchFile
    {
        public string path;
        public string url;
        public string sha256;
    }
}
