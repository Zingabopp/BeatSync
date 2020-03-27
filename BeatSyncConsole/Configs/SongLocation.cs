using BeatSyncConsole.Utilities;
using System;
using System.IO;
using Newtonsoft.Json;
using static BeatSyncConsole.Utilities.Paths;

namespace BeatSyncConsole.Configs
{
    public class SongLocation
    {

        public static void SetDefaultPaths(SongLocation songLocation)
        {
            string songsPath;
            string playlistsDirectory;
            string historyPath;
            if (songLocation.LocationType != InstallType.Custom)
            {
                songsPath = Path_CustomLevels;
                playlistsDirectory = "Playlists";
                historyPath = Path.Combine("UserData", "BeatSyncHistory.json");
            }
            else
            {
                songsPath = songLocation.BasePath;
                playlistsDirectory = null;
                historyPath = "BeatSyncHistory.json";
            }

            songLocation.SongsDirectory = songsPath;
            songLocation.PlaylistDirectory = playlistsDirectory;
            songLocation.HistoryPath = historyPath;
        }
        public static SongLocation CreateGameLocation(string basePath, InstallType locationType)
        {
            if (locationType == InstallType.Custom)
                throw new ArgumentException("LocationType should be either Steam or Oculus.", nameof(locationType));
            if (string.IsNullOrEmpty(basePath))
                throw new ArgumentNullException(nameof(basePath), $"{nameof(basePath)} cannot be null or empty.");
            basePath = Path.GetFullPath(basePath);
            SongLocation newLocation = new SongLocation(basePath, locationType);
            SetDefaultPaths(newLocation);
            return newLocation;
        }
        internal SongLocation() { }
        public SongLocation(string basePath, InstallType locationType)
        {
            if (string.IsNullOrEmpty(basePath))
                throw new ArgumentNullException(nameof(basePath), $"{nameof(basePath)} cannot be null or empty.");
            BasePath = Path.GetFullPath(basePath);
            LocationType = locationType;
            SetDefaultPaths(this);
        }
        [JsonIgnore]
        private string _songsDirectory;
        [JsonIgnore]
        private string _historyPath;

        [JsonProperty("Enabled", Order = 0)]
        public bool Enabled { get; set; }

        [JsonProperty("BasePath", Order = 5)]
        public string BasePath { get; set; }

        [JsonProperty("LocationType", Order = 10)]
        public InstallType LocationType { get; set; }

        [JsonProperty("SongsDirectory", Order = 15)]
        public string SongsDirectory
        {
            get => _songsDirectory ?? BasePath;
            set
            {
                _songsDirectory = value;
            }
        }

        [JsonProperty("PlaylistDirectory", Order = 20)]
        public string PlaylistDirectory { get; set; }

        [JsonProperty("HistoryPath", Order = 25)]
        public string HistoryPath
        {
            get => _historyPath ??= Path.Combine(BasePath, "BeatSyncHistory.json");
            set
            {
                _historyPath = value;
            }
        }

        public override string ToString()
        {
            if (Enabled)
                return $"{LocationType}: {BasePath}";
            else
                return $"(Disabled) {LocationType}: {BasePath}";
        }
    }
}
