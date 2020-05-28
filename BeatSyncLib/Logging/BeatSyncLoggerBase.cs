using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace BeatSyncLib.Logging
{
    public abstract class BeatSyncLoggerBase : IBeatSyncLogger
    {
        public LogLevel LoggingLevel { get; set; }

        public abstract void Log(string message, LogLevel logLevel);
        public abstract void Log(Exception ex, LogLevel logLevel);

        public void Critical(string message) => Log(message, LogLevel.Critical);
        public void Critical(Exception ex) => Log(ex, LogLevel.Critical);

        public void Debug(string message) => Log(message, LogLevel.Debug);

        public void Debug(Exception ex) => Log(ex, LogLevel.Debug);

        public void Error(string message) => Log(message, LogLevel.Error);

        public void Error(Exception ex) => Log(ex, LogLevel.Error);

        public void Info(string message) => Log(message, LogLevel.Info);

        public void Info(Exception ex) => Log(ex, LogLevel.Info);

        public void Warn(string message) => Log(message, LogLevel.Warn);

        public void Warn(Exception ex) => Log(ex, LogLevel.Warn);
    }
}
