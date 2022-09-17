using SongFeedReaders.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncConsole.Loggers
{
    public class BeatSyncLoggerSettings : ILoggerSettings
    {
        public string? ModuleName { get; set; }
        public LogLevel LogLevel { get; set; }
        public bool ShowModule { get; set; }
        public bool ShortSource { get; set; }
        public bool EnableTimeStamp { get; set; }

        public BeatSyncLoggerSettings() { }
        public BeatSyncLoggerSettings(ILoggerSettings settings)
        {
            ModuleName = settings.ModuleName;
            LogLevel = settings.LogLevel;
            ShowModule = settings.ShowModule;
            ShortSource = settings.ShortSource;
            EnableTimeStamp = settings.EnableTimeStamp;

        }
    }
}
