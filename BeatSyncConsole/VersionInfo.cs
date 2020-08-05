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
                if(_versionDescription == null)
                {
                    string? versionDescription = Assembly.GetExecutingAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                    if (versionDescription != null && versionDescription.Length > 0)
                        versionDescription = $" {versionDescription}";
                    else
                        versionDescription = " Unofficial";
                    _versionDescription = versionDescription;
                }
                return _versionDescription; 
            }
        }

    }
}
