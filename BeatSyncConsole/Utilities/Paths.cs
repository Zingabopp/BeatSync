using BeatSyncConsole.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSyncConsole.Utilities
{
    public static class Paths
    {
        public const string Path_CustomLevels = @"Beat Saber_Data\CustomLevels";
        public const string Path_Playlists = @"Playlists";
        public const string Path_History = @"UserData\BeatSyncHistory.json";
        public static SongLocation ToSongLocation(this BeatSaberInstall install)
        {
            return SongLocation.CreateGameLocation(install.InstallPath, install.InstallType);
        }
    }
}
