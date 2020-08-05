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
            songLocation.HistoryPath = "BeatSyncHistory.json";
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
        [JsonConstructor]
        internal CustomSongLocation() { }

        public CustomSongLocation(string basePath)
        {
            if (string.IsNullOrEmpty(basePath))
                throw new ArgumentNullException(nameof(basePath));
            BasePath = basePath;
            SetDefaultPaths(this);
        }
        [JsonIgnore]
        private string? _basePath;
        [JsonIgnore]
        private string? _songsDirectory;
        [JsonIgnore]
        private string? _playlistDirectory;
        [JsonIgnore]
        private string? _historyPath;
        [JsonIgnore]
        private bool? _unzipBeatmaps;


        [JsonProperty("Enabled", Order = 0)]
        public bool Enabled { get; set; }

        [JsonProperty("BasePath", Order = 5)]
        public string BasePath
        {
            get => _basePath ??= string.Empty;
            set
            {
                _fullBasePath = null;
                _basePath = value;
            }
        }

        [JsonProperty("SongsDirectory", Order = 15)]
        public string SongsDirectory
        {
            get => _songsDirectory ??= "CustomLevels";
            set
            {
                _fullSongsPath = null;
                _songsDirectory = value;
            }
        }

        [JsonProperty("PlaylistDirectory", Order = 20)]
        public string PlaylistDirectory
        {
            get => _playlistDirectory ??= "Playlists";
            set
            {
                _fullPlaylistsPath = null;
                _playlistDirectory = value;
            }
        }

        [JsonProperty("HistoryPath", Order = 25)]
        public string HistoryPath
        {
            get => _historyPath ??= "BeatSyncHistory.json";
            set
            {
                _fullHistoryPath = null;
                _historyPath = value;
            }
        }


        [JsonProperty("UnzipBeatmaps", Order = 30)]
        public bool UnzipBeatmaps
        {
            get { return _unzipBeatmaps ??= true; }
            set { _unzipBeatmaps = value; }
        }

        [JsonIgnore]
        private object fullBaseLock = new object();
        [JsonIgnore]
        private object fullSongsLock = new object();
        [JsonIgnore]
        private object fullPlaylistsLock = new object();
        [JsonIgnore]
        private object fullHistoryLock = new object();
        [JsonIgnore]
        private string? _fullBasePath;
        [JsonIgnore]
        private string? _fullSongsPath;
        [JsonIgnore]
        private string? _fullPlaylistsPath;
        [JsonIgnore]
        private string? _fullHistoryPath;
        [JsonIgnore]
        public string FullBasePath
        {
            get
            {
                lock (fullBaseLock)
                {
                    if (_fullBasePath != null)
                        return _fullBasePath;
                    if (string.IsNullOrEmpty(BasePath))
                        _fullBasePath = Paths.AssemblyDirectory;
                    else
                        _fullBasePath = Paths.GetFullPath(BasePath, PathRoot.AssemblyDirectory);
                    return _fullBasePath;
                }
            }
        }
        [JsonIgnore]
        public string FullSongsPath
        {
            get
            {
                lock (fullSongsLock)
                {
                    if (_fullSongsPath != null)
                        return _fullSongsPath;
                    _fullSongsPath = Paths.GetFullPath(SongsDirectory, FullBasePath);
                    return _fullSongsPath; 
                }
            }
        }
        [JsonIgnore]
        public string FullPlaylistsPath
        {
            get
            {
                lock (fullPlaylistsLock)
                {
                    if (_fullPlaylistsPath != null)
                        return _fullPlaylistsPath;
                    _fullPlaylistsPath = Paths.GetFullPath(PlaylistDirectory, FullBasePath);
                    return _fullPlaylistsPath; 
                }
            }
        }
        [JsonIgnore]
        public string FullHistoryPath
        {
            get
            {
                lock (fullHistoryLock)
                {
                    if (_fullHistoryPath != null)
                        return _fullHistoryPath;
                    _fullHistoryPath = Paths.GetFullPath(HistoryPath, FullBasePath);
                    return _fullHistoryPath; 
                }
            }
        }



        public override string ToString()
        {
            if (Enabled)
                return $"Custom: {GetRelativeDirectory(FullBasePath, PathRoot.AssemblyDirectory)}";
            else
                return $"(Disabled) Custom: {GetRelativeDirectory(FullBasePath, PathRoot.AssemblyDirectory)}";
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
