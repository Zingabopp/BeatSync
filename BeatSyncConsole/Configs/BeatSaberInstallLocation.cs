using BeatSyncConsole.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BeatSyncConsole.Configs
{
    public class BeatSaberInstallLocation : ISongLocation
    {
        public static BeatSaberInstallLocation CreateEmptyLocation() => new BeatSaberInstallLocation() { Enabled = false, BasePath = string.Empty };

        private bool _enabled;

        public BeatSaberInstallLocation() { }
        public BeatSaberInstallLocation(string gameDirectory)
        {
            BasePath = gameDirectory ?? string.Empty;
        }

        [JsonProperty("Enabled", Order = 0)]
        public bool Enabled { get => _enabled && !string.IsNullOrEmpty(BasePath); set => _enabled = value; }

        [JsonProperty("GameDirectory", Order = 5)]
        public string BasePath { get; set; } = string.Empty;
        [JsonIgnore]
        public string SongsDirectory => !string.IsNullOrEmpty(BasePath) ? Path.Combine(FullBasePath, "Beat Saber_Data", "CustomLevels") : string.Empty;
        [JsonIgnore]
        public string PlaylistDirectory => !string.IsNullOrEmpty(BasePath) ? Path.Combine(FullBasePath, "Playlists") : string.Empty;
        [JsonIgnore]
        public string HistoryPath => !string.IsNullOrEmpty(BasePath) ? Path.Combine(FullBasePath, "UserData", "BeatSyncHistory.json") : string.Empty;

        private string? _fullBasePath;
        [JsonIgnore]
        public string FullBasePath
        {
            get
            {
                if (_fullBasePath != null)
                    return _fullBasePath;
                if (string.IsNullOrEmpty(BasePath))
                    return BasePath;
                _fullBasePath = Paths.ExpandPath(BasePath);
                return _fullBasePath;
            }
        }
        [JsonIgnore]
        public string FullSongsPath => SongsDirectory;
        [JsonIgnore]
        public string FullPlaylistsPath => PlaylistDirectory;
        [JsonIgnore]
        public string FullHistoryPath => HistoryPath;

        public bool IsValid(out string? reason)
        {
            if (string.IsNullOrEmpty(BasePath))
            {
                reason = "Path is empty.";
                return false;
            }
            if (!Directory.Exists(FullBasePath))
            {
                reason = $"'{FullBasePath}' does not exist.";
                return false;
            }

            reason = null;
            return true;
        }
        public override string ToString()
        {
            if (Enabled)
                return $"GameInstall: {FullBasePath}";
            else
                return $"(Disabled) GameInstall: {FullBasePath}";
        }

        public bool IsValid() => IsValid(out _);
    }
}
