using BeatSyncConsole.Utilities;
using System;
using System.IO;
using Newtonsoft.Json;
using static BeatSyncConsole.Utilities.Paths;
using Newtonsoft.Json.Converters;

namespace BeatSyncConsole.Configs
{
    public class CustomSongLocation : ISongLocation
    {

        public static void SetDefaultPaths(CustomSongLocation songLocation)
        {
            songLocation.SongsDirectory = songLocation.BasePath;
            songLocation.PlaylistDirectory = null;
            songLocation.HistoryPath = "BeatSyncHistory.json";
        }
        public static CustomSongLocation CreateGameLocation(string basePath)
        {
            if (string.IsNullOrEmpty(basePath))
                throw new ArgumentNullException(nameof(basePath), $"{nameof(basePath)} cannot be null or empty.");
            basePath = Path.GetFullPath(basePath);
            CustomSongLocation newLocation = new CustomSongLocation(basePath);
            SetDefaultPaths(newLocation);
            return newLocation;
        }

        public static CustomSongLocation CreateEmptyLocation()
        {
            return new CustomSongLocation()
            {
                BasePath = string.Empty,
                PlaylistDirectory = "Playlists",
                HistoryPath = "BeatSyncHistory.json"
            };
        }

        internal CustomSongLocation() { }
        public CustomSongLocation(string basePath)
        {
            if (!string.IsNullOrEmpty(basePath))
                BasePath = Path.GetFullPath(basePath);
            SetDefaultPaths(this);
        }
        [JsonIgnore]
        private string? _songsDirectory;
        [JsonIgnore]
        private string? _historyPath;

        [JsonProperty("Enabled", Order = 0)]
        public bool Enabled { get; set; }

        [JsonProperty("BasePath", Order = 5)]
        public string BasePath { get; set; } = string.Empty;

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
        public string? PlaylistDirectory { get; set; }

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
                return $"Custom: {BasePath}";
            else
                return $"(Disabled) Custom: {BasePath}";
        }

        public bool IsValid(out string? reason)
        {
            if (string.IsNullOrEmpty(BasePath))
            {
                reason = "Path is empty.";
                return false;
            }
            reason = null;
            return true;
        }

        public bool IsValid() => IsValid(out _);
    }
}
