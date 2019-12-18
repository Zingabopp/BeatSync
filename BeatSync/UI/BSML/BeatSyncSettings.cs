using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Notify;
using BeatSync.Configs;

namespace BeatSync.UI.BSML
{
    internal class BeatSyncSettings : ConfigUiBase
    {
        internal PluginConfig PreviousConfig { get; }
        internal PluginConfig Config { get; }

        private string ViewFileName = "BeatSyncSettings.bsml";
        public override string ResourceName => "BeatSync.UI.BSML." + ViewFileName;

        public override string ContentFilePath => Path.Combine(ViewDirectory, ViewFileName);

        public BeatSyncSettings(PluginConfig config)
        {
            PreviousConfig = config;
            Config = config.Clone();
        }

        [UIValue("DownloadTimeoutChanged")]
        public bool DownloadTimeoutChanged { get { return Config.DownloadTimeout != PreviousConfig.DownloadTimeout; } }
        [UIValue("DownloadTimeout")]
        public int DownloadTimeout
        {
            get { return Config.DownloadTimeout; }
            set
            {
                if (Config.DownloadTimeout == value) return;
                Config.DownloadTimeout = value;
                NotifyPropertyChanged(nameof(DownloadTimeoutChanged));
            }
        }

        [UIValue("MaxConcurrentDownloadsChanged")]
        public bool MaxConcurrentDownloadsChanged { get { return Config.MaxConcurrentDownloads != PreviousConfig.MaxConcurrentDownloads; } }
        [UIValue("MaxConcurrentDownloads")]
        public int MaxConcurrentDownloads
        {
            get { return Config.MaxConcurrentDownloads; }
            set
            {
                if (Config.MaxConcurrentDownloads == value) return;
                Config.MaxConcurrentDownloads = value;
                NotifyPropertyChanged(nameof(MaxConcurrentDownloadsChanged));
            }
        }

        [UIValue("RecentPlaylistDaysChanged")]
        public bool RecentPlaylistDaysChanged { get { return Config.RecentPlaylistDays != PreviousConfig.RecentPlaylistDays; } }
        [UIValue("RecentPlaylistDays")]
        public int RecentPlaylistDays
        {
            get { return Config.RecentPlaylistDays; }
            set
            {
                if (Config.RecentPlaylistDays == value) return;
                Config.RecentPlaylistDays = value;
                NotifyPropertyChanged(nameof(RecentPlaylistDaysChanged));
            }
        }

        [UIValue("AllBeatSyncSongsPlaylistChanged")]
        public bool AllBeatSyncSongsPlaylistChanged { get { return Config.AllBeatSyncSongsPlaylist != PreviousConfig.AllBeatSyncSongsPlaylist; } }
        [UIValue("AllBeatSyncSongsPlaylist")]
        public bool AllBeatSyncSongsPlaylist
        {
            get { return Config.AllBeatSyncSongsPlaylist; }
            set
            {
                if (Config.AllBeatSyncSongsPlaylist == value) return;
                Config.AllBeatSyncSongsPlaylist = value;
                NotifyPropertyChanged(nameof(AllBeatSyncSongsPlaylistChanged));
            }
        }

    }
}
