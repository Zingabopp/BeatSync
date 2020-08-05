namespace BeatSyncConsole
{
    public static class VersionInfo
    {
        private const string GitCommit = null;
        private static string? _versionDescription;

        public static string Description
        {
            get 
            { 
                if(_versionDescription == null)
                {
                    string gitInfo = GitCommit;
                    if (gitInfo != null && gitInfo.Length > 0)
                        gitInfo = $" {gitInfo}";
                    else
                        gitInfo = " Unofficial";
                    _versionDescription = gitInfo;
                }
                return _versionDescription; 
            }
        }

    }
}
