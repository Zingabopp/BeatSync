using BeatSyncLib.Logging;
using System;

namespace BeatSyncConsole.Loggers
{
    public class BeatSyncLogger : BeatSyncLoggerBase
    {
        public readonly string SourceName;
        public BeatSyncLogger(string sourceName)
        {
            SourceName = sourceName;
        }
        public override void Log(string message, LogLevel logLevel)
        {
            if (LoggingLevel > logLevel)
                return;
            LogManager.QueueMessage($"[{SourceName} - {logLevel}]: {message}", logLevel);
        }

        public override void Log(Exception ex, LogLevel logLevel)
        {
            if (LoggingLevel > logLevel)
                return;
            LogManager.QueueMessage($"[{SourceName} - {logLevel}]: {ex}", logLevel);
        }
    }
}
