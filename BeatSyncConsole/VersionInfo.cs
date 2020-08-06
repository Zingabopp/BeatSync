using System.Reflection;

namespace BeatSyncConsole
{
    public static class VersionInfo
    {
        private static string? _versionDescription;

        public static string Description
        {
            get
            {
                if (_versionDescription == null)
                {
                    string? versionDescription = Assembly.GetExecutingAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                    versionDescription = "Official-master-cfa3d9c";
                    if (versionDescription != null && versionDescription.Length > 0)
                    {
                        if (versionDescription.Contains('+'))
                            versionDescription = versionDescription.Substring(0, versionDescription.IndexOf('+'));
                        versionDescription = $" {versionDescription}";
                    }
                    else
                        versionDescription = " Unknown Build";
                    _versionDescription = versionDescription;
                }
                return _versionDescription;
            }
        }

    }
}
