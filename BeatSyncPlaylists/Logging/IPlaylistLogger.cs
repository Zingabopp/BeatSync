using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncPlaylists.Logging
{
    public interface IPlaylistLogger
    {
        void Log(string message, LogLevel logLevel);
        void Log(Exception ex, LogLevel logLevel);

        public void Debug(string message);
        public void Debug(Exception exception);

        public void Warn(string message);
        public void Warn(Exception exception);

        public void Info(string message);
        public void Info(Exception exception);

        public void Error(string message);
        public void Error(Exception exception);
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }
}
