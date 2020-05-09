using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncPlaylists.Logging
{
    public abstract class BeatSyncPlaylistLogger : IPlaylistLogger
    {
        public abstract void Log(string message, LogLevel logLevel);

        public abstract void Log(Exception ex, LogLevel logLevel);

        public void Debug(string message) => Log(message, LogLevel.Debug);
        public void Debug(Exception exception) => Log(exception, LogLevel.Debug);

        public void Warn(string message) => Log(message, LogLevel.Warning);
        public void Warn(Exception exception) => Log(exception, LogLevel.Warning);

        public void Info(string message) => Log(message, LogLevel.Info);
        public void Info(Exception exception) => Log(exception, LogLevel.Info);

        public void Error(string message) => Log(message, LogLevel.Error);
        public void Error(Exception exception) => Log(exception, LogLevel.Error);
    }
}
